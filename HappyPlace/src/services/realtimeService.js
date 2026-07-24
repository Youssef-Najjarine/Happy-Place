import 'react-native-url-polyfill/auto';
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AppState } from 'react-native';
import baseService from './baseService';
import tokenStorage from './tokenStorage';
import { chatGroupsApi } from 'store/chatGroupsApi';
import { friendsApi } from 'store/friendsApi';
import { setRealtimeConnected, bumpHelpChanged } from 'store/realtimeSlice';
import {
    CHAT_GROUP_CHANGED_EVENT,
    FRIENDS_CHANGED_EVENT,
    HELP_CHANGED_EVENT,
    MESSAGES_KIND,
    CHAT_GROUPS_RESYNC_TAGS,
    FRIENDS_TAGS,
    buildInvalidationsForEvent,
    nextReconnectDelayMs,
    shouldResyncOnForeground,
    createChatGroupListenerRegistry,
    createSubscriptionCounter,
} from '../utils/realtimeEvents';

const RETRY_AFTER_START_FAILURE_MS = 10000;

let dispatchFunction = null;
let connection = null;
let currentAuthToken = null;
let isStarting = false;
let retryTimer = null;
let lastAppState = 'active';
let tokenUnsubscribe = null;
let appStateSubscription = null;
const registry = createChatGroupListenerRegistry();
const openRequestsSubscriptionCounter = createSubscriptionCounter(applyListeningState, applyListeningState);

function applyListeningState() {
    if (!connection || connection.state !== HubConnectionState.Connected) {
        return;
    }
    const wantListening = openRequestsSubscriptionCounter.count() > 0;
    Promise.resolve(connection.invoke('SetListening', wantListening)).catch(function() {});
}

function dispatchSafe(action) {
    if (dispatchFunction) {
        dispatchFunction(action);
    }
}

function clearRetryTimer() {
    if (retryTimer) {
        clearTimeout(retryTimer);
        retryTimer = null;
    }
}

function scheduleStartRetry() {
    clearRetryTimer();
    if (!currentAuthToken) {
        return;
    }
    retryTimer = setTimeout(function() {
        retryTimer = null;
        ensureStartedAndAuthenticated();
    }, RETRY_AFTER_START_FAILURE_MS);
}

function handleRealtimeEvent(eventName, payload) {
    const invalidations = buildInvalidationsForEvent(eventName, payload);
    if (invalidations.chatGroupsTags.length > 0) {
        dispatchSafe(chatGroupsApi.util.invalidateTags(invalidations.chatGroupsTags));
    }
    if (invalidations.friendsTags.length > 0) {
        dispatchSafe(friendsApi.util.invalidateTags(invalidations.friendsTags));
    }
    if (invalidations.helpTick) {
        dispatchSafe(bumpHelpChanged());
    }
    if (invalidations.chatGroupNotification) {
        registry.notify(invalidations.chatGroupNotification.chatGroupId, invalidations.chatGroupNotification.kind);
    }
}

function resyncAll() {
    dispatchSafe(chatGroupsApi.util.invalidateTags([...CHAT_GROUPS_RESYNC_TAGS]));
    dispatchSafe(friendsApi.util.invalidateTags([...FRIENDS_TAGS]));
    dispatchSafe(bumpHelpChanged());
    registry.notifyAll(MESSAGES_KIND);
}

function buildConnection() {
    const hubConnection = new HubConnectionBuilder()
        .withUrl(baseService.getBaseUrl() + '/hubs/realtime')
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: function(retryContext) {
                return nextReconnectDelayMs(retryContext.previousRetryCount);
            },
        })
        .build();
    hubConnection.on(CHAT_GROUP_CHANGED_EVENT, function(payload) { handleRealtimeEvent(CHAT_GROUP_CHANGED_EVENT, payload); });
    hubConnection.on(FRIENDS_CHANGED_EVENT, function(payload) { handleRealtimeEvent(FRIENDS_CHANGED_EVENT, payload); });
    hubConnection.on(HELP_CHANGED_EVENT, function(payload) { handleRealtimeEvent(HELP_CHANGED_EVENT, payload); });
    hubConnection.onreconnecting(function() {
        dispatchSafe(setRealtimeConnected(false));
    });
    hubConnection.onreconnected(function() {
        authenticateAfterReconnect();
    });
    hubConnection.onclose(function() {
        dispatchSafe(setRealtimeConnected(false));
        scheduleStartRetry();
    });
    return hubConnection;
}

async function authenticate() {
    const status = await connection.invoke('Authenticate', currentAuthToken);
    if (status !== 'authenticated') {
        throw new Error('realtime authentication rejected');
    }
}

async function authenticateAfterReconnect() {
    try {
        await authenticate();
        dispatchSafe(setRealtimeConnected(true));
        applyListeningState();
        resyncAll();
    } catch {
        stopConnection();
    }
}

async function ensureStartedAndAuthenticated() {
    if (isStarting || !currentAuthToken) {
        return;
    }
    isStarting = true;
    clearRetryTimer();
    try {
        if (!connection) {
            connection = buildConnection();
        }
        if (connection.state === HubConnectionState.Disconnected) {
            await connection.start();
        }
        await authenticate();
        dispatchSafe(setRealtimeConnected(true));
        applyListeningState();
        resyncAll();
    } catch {
        dispatchSafe(setRealtimeConnected(false));
        scheduleStartRetry();
    } finally {
        isStarting = false;
    }
}

function stopConnection() {
    clearRetryTimer();
    dispatchSafe(setRealtimeConnected(false));
    if (connection) {
        const stoppingConnection = connection;
        connection = null;
        Promise.resolve(stoppingConnection.stop()).catch(function() {});
    }
}

function handleTokenChanged(authToken) {
    const previousAuthToken = currentAuthToken;
    currentAuthToken = authToken || null;
    if (!currentAuthToken) {
        stopConnection();
        return;
    }
    if (currentAuthToken === previousAuthToken && connection && connection.state === HubConnectionState.Connected) {
        return;
    }
    if (connection && connection.state === HubConnectionState.Connected) {
        authenticateAfterReconnect();
        return;
    }
    ensureStartedAndAuthenticated();
}

function handleAppStateChanged(nextAppState) {
    const previousAppState = lastAppState;
    lastAppState = nextAppState;
    if (!shouldResyncOnForeground(previousAppState, nextAppState)) {
        return;
    }
    if (connection && connection.state === HubConnectionState.Connected) {
        resyncAll();
        return;
    }
    if (currentAuthToken) {
        ensureStartedAndAuthenticated();
    }
}

const realtimeService = {
    initialize: function(dispatch) {
        dispatchFunction = dispatch;
        if (tokenUnsubscribe) {
            return;
        }
        lastAppState = AppState.currentState || 'active';
        tokenUnsubscribe = tokenStorage.subscribe(handleTokenChanged);
        appStateSubscription = AppState.addEventListener('change', handleAppStateChanged);
        Promise.resolve(tokenStorage.getToken())
            .then(function(authToken) {
                if (authToken && !currentAuthToken) {
                    handleTokenChanged(authToken);
                }
            })
            .catch(function() {});
    },

    teardown: function() {
        if (tokenUnsubscribe) {
            tokenUnsubscribe();
            tokenUnsubscribe = null;
        }
        if (appStateSubscription) {
            appStateSubscription.remove();
            appStateSubscription = null;
        }
        stopConnection();
        currentAuthToken = null;
        dispatchFunction = null;
    },

    subscribeToChatGroup: function(chatGroupId, listener) {
        return registry.subscribe(chatGroupId, listener);
    },

    acquireOpenRequestsSubscription: function() {
        return openRequestsSubscriptionCounter.acquire();
    },
};

export default realtimeService;