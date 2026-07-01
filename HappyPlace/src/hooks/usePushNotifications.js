import { useEffect } from 'react';
import { AppState, InteractionManager } from 'react-native';
import tokenStorage from 'src/services/tokenStorage';
import pushNotificationService from 'src/services/pushNotificationService';
import navigationRef from 'src/services/navigationService';
import pendingInvite from 'src/services/pendingInvite';
import handledGroups from 'src/services/handledGroups';
import { useJoinMutation } from 'src/store/helpApi';
import { showToast } from 'src/components/Toast';
import localNotifications from 'src/services/localNotifications';

export default function usePushNotifications() {
    const [join] = useJoinMutation();

    useEffect(() => {
        let active = true;

        const registerCurrentDevice = async () => {
            try {
                const token = await tokenStorage.getToken();
                if (active && token) {
                    await pushNotificationService.registerDevice(token);
                }
            } catch (error) {
            }
        };

        const waitForNavigationReady = () => new Promise((resolve) => {
            if (navigationRef.isReady()) {
                resolve();
                return;
            }
            const startedAt = Date.now();
            const timer = setInterval(() => {
                if (navigationRef.isReady() || Date.now() - startedAt > 15000) {
                    clearInterval(timer);
                    resolve();
                }
            }, 100);
        });

        const runWhenSettled = (callback) => {
            InteractionManager.runAfterInteractions(() => {
                requestAnimationFrame(() => {
                    callback();
                });
            });
        };

        const resetToChatGroup = (chatGroupId) => {
            runWhenSettled(() => {
                if (active && navigationRef.isReady()) {
                    navigationRef.reset({ index: 1, routes: [{ name: 'ChatGroups' }, { name: 'ChatGroup', params: { chatGroupId } }] });
                }
            });
        };

        const navigateToRoute = (routeName) => {
            runWhenSettled(() => {
                if (active && navigationRef.isReady()) {
                    navigationRef.navigate(routeName);
                }
            });
        };

        const joinGroup = async (chatGroupId) => {
            const token = await tokenStorage.getToken();
            if (!token) return null;
            const result = await join({ authToken: token, chatGroupId }).unwrap();
            if (!result || !result.chatGroupId) return null;
            return result.chatGroupId;
        };

        const openInvite = async (chatGroupId) => {
            handledGroups.markHandled(chatGroupId);
            let targetId = null;
            try {
                targetId = await joinGroup(chatGroupId);
            } catch (error) {
                handledGroups.unmark(chatGroupId);
                throw error;
            }
            if (targetId === null) {
                handledGroups.unmark(chatGroupId);
                showToast('That conversation is no longer available', 'info');
                return false;
            }
            await waitForNavigationReady();
            if (!active) return true;
            resetToChatGroup(targetId);
            return true;
        };

        const handleNotificationTap = async (remoteMessage) => {
            const data = remoteMessage && remoteMessage.data;
            if (!data || !data.type) return;
            if (data.type === 'invite') {
                if (!data.chatGroupId) return;
                try {
                    await openInvite(data.chatGroupId);
                } catch (error) {
                    showToast('Could not join right now', 'info');
                }
                return;
            }
            if (data.type === 'helpWaiting') {
                await waitForNavigationReady();
                if (!active) return;
                navigateToRoute('OfferHelp');
                return;
            }
            if (data.type === 'helpOffers') {
                await waitForNavigationReady();
                if (!active) return;
                navigateToRoute('ChatGroups');
            }
        };

        const handleForegroundMessage = async (remoteMessage) => {
            const data = remoteMessage && remoteMessage.data;
            if (!data) return;
            if (data.type === 'dismiss' && data.collapseId) {
                await localNotifications.cancelByCollapseId(data.collapseId);
                return;
            }
            if (data.type === 'helpWaiting' || data.type === 'helpOffers') {
                const currentRoute = navigationRef.isReady() ? navigationRef.getCurrentRoute() : null;
                const currentName = currentRoute && currentRoute.name ? currentRoute.name : null;
                if (currentName === 'ChatGroups' || currentName === 'OfferHelp') {
                    return;
                }
                const notification = remoteMessage.notification;
                const body = notification && notification.body ? notification.body : null;
                if (body) {
                    const target = data.type === 'helpWaiting' ? 'OfferHelp' : 'ChatGroups';
                    showToast(body, 'info', { label: 'View', onPress: () => navigateToRoute(target) });
                }
            }
        };

        const handleColdStart = async () => {
            let initialData = null;
            try {
                const initialNotification = await pushNotificationService.getInitialNotification();
                initialData = initialNotification && initialNotification.data ? initialNotification.data : null;
            } catch (error) {
            }
            if (active && initialData && initialData.type === 'invite' && initialData.chatGroupId) {
                pendingInvite.set(initialData.chatGroupId);
            }
            if (active && initialData && (initialData.type === 'helpWaiting' || initialData.type === 'helpOffers')) {
                await waitForNavigationReady();
                if (active) {
                    navigateToRoute(initialData.type === 'helpWaiting' ? 'OfferHelp' : 'ChatGroups');
                }
                return;
            }
            const chatGroupId = pendingInvite.peek();
            if (!chatGroupId) return;
            try {
                const handled = await openInvite(chatGroupId);
                if (handled) {
                    pendingInvite.markHandled();
                } else {
                    pendingInvite.clear();
                }
            } catch (error) {
                pendingInvite.clear();
                showToast('Could not join right now', 'info');
            }
        };

        const init = async () => {
            await handleColdStart();
            try {
                await pushNotificationService.requestPermission();
            } catch (error) {
            }
            registerCurrentDevice();
        };

        init();

        const unsubscribeTokenRefresh = pushNotificationService.onTokenRefresh(() => {
            registerCurrentDevice();
        });

        const unsubscribeForegroundMessage = pushNotificationService.onForegroundMessage(handleForegroundMessage);

        const unsubscribeNotificationOpened = pushNotificationService.onNotificationOpenedApp(handleNotificationTap);

        const appStateSubscription = AppState.addEventListener('change', (nextAppState) => {
            if (nextAppState === 'active') {
                registerCurrentDevice();
            }
        });

        return () => {
            active = false;
            unsubscribeTokenRefresh();
            unsubscribeForegroundMessage();
            unsubscribeNotificationOpened();
            appStateSubscription.remove();
        };
    }, [join]);
}