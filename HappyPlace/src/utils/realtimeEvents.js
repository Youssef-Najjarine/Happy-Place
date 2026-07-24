export const CHAT_GROUP_CHANGED_EVENT = 'chatGroupChanged';
export const FRIENDS_CHANGED_EVENT = 'friendsChanged';
export const HELP_CHANGED_EVENT = 'helpChanged';

export const MESSAGES_KIND = 'messages';
export const MEMBERSHIP_KIND = 'membership';

export const CHAT_GROUPS_MEMBERSHIP_TAGS = ['ChatGroupList', 'ChatGroupMembers'];
export const CHAT_GROUPS_MESSAGES_TAGS = ['ChatGroupList'];
export const CHAT_GROUPS_RESYNC_TAGS = ['ChatGroupList', 'ChatGroupMembers', 'AvailableHelpers'];
export const FRIENDS_TAGS = ['FriendList', 'IncomingRequests', 'OutgoingRequests', 'UserSearch', 'BlockedList'];

export function buildInvalidationsForEvent(eventName, payload) {
    const result = { chatGroupsTags: [], friendsTags: [], helpTick: false, chatGroupNotification: null };
    if (eventName === CHAT_GROUP_CHANGED_EVENT) {
        const kind = payload && payload.kind === MESSAGES_KIND ? MESSAGES_KIND : MEMBERSHIP_KIND;
        result.chatGroupsTags = kind === MESSAGES_KIND ? [...CHAT_GROUPS_MESSAGES_TAGS] : [...CHAT_GROUPS_MEMBERSHIP_TAGS];
        const chatGroupId = payload && typeof payload.chatGroupId === 'string' && payload.chatGroupId.length > 0 ? payload.chatGroupId : null;
        if (chatGroupId) {
            result.chatGroupNotification = { chatGroupId, kind };
        }
        return result;
    }
    if (eventName === FRIENDS_CHANGED_EVENT) {
        result.friendsTags = [...FRIENDS_TAGS];
        return result;
    }
    if (eventName === HELP_CHANGED_EVENT) {
        result.helpTick = true;
        return result;
    }
    return result;
}

export function nextReconnectDelayMs(previousRetryCount) {
    const delays = [0, 2000, 5000, 10000];
    if (previousRetryCount == null || previousRetryCount < 0) {
        return delays[0];
    }
    if (previousRetryCount < delays.length) {
        return delays[previousRetryCount];
    }
    return 30000;
}

export function shouldResyncOnForeground(previousAppState, nextAppState) {
    return nextAppState === 'active' && previousAppState !== 'active';
}

export function createChatGroupListenerRegistry() {
    const listenersByChatGroupId = new Map();
    return {
        subscribe: function(chatGroupId, listener) {
            if (!chatGroupId || typeof listener !== 'function') {
                return function() {};
            }
            if (!listenersByChatGroupId.has(chatGroupId)) {
                listenersByChatGroupId.set(chatGroupId, new Set());
            }
            const listeners = listenersByChatGroupId.get(chatGroupId);
            listeners.add(listener);
            return function() {
                listeners.delete(listener);
                if (listeners.size === 0) {
                    listenersByChatGroupId.delete(chatGroupId);
                }
            };
        },
        notify: function(chatGroupId, kind) {
            const listeners = listenersByChatGroupId.get(chatGroupId);
            if (!listeners) {
                return;
            }
            listeners.forEach(function(listener) {
                try { listener(kind); } catch {}
            });
        },
        notifyAll: function(kind) {
            listenersByChatGroupId.forEach(function(listeners) {
                listeners.forEach(function(listener) {
                    try { listener(kind); } catch {}
                });
            });
        },
    };
}

export function createSubscriptionCounter(onFirstAcquire, onLastRelease) {
    let subscriberCount = 0;
    return {
        acquire: function() {
            subscriberCount += 1;
            if (subscriberCount === 1) {
                try { onFirstAcquire(); } catch {}
            }
            let released = false;
            return function() {
                if (released) {
                    return;
                }
                released = true;
                subscriberCount -= 1;
                if (subscriberCount === 0) {
                    try { onLastRelease(); } catch {}
                }
            };
        },
        count: function() {
            return subscriberCount;
        },
    };
}