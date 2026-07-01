import { Platform } from 'react-native';
import {
    getMessaging,
    requestPermission as fbRequestPermission,
    getToken as fbGetToken,
    registerDeviceForRemoteMessages as fbRegisterDeviceForRemoteMessages,
    isDeviceRegisteredForRemoteMessages as fbIsDeviceRegisteredForRemoteMessages,
    onTokenRefresh as fbOnTokenRefresh,
    onMessage as fbOnMessage,
    onNotificationOpenedApp as fbOnNotificationOpenedApp,
    getInitialNotification as fbGetInitialNotification,
    setBackgroundMessageHandler as fbSetBackgroundMessageHandler,
    AuthorizationStatus
} from '@react-native-firebase/messaging';
import baseService from 'src/services/baseService';

const GetTokenTimeoutMs = 8000;

async function requestPermission() {
    const messaging = getMessaging();
    const authorizationStatus = await fbRequestPermission(messaging);
    return authorizationStatus === AuthorizationStatus.AUTHORIZED || authorizationStatus === AuthorizationStatus.PROVISIONAL;
}

async function ensureRegisteredForRemoteMessages() {
    if (Platform.OS !== 'ios') return;
    const messaging = getMessaging();
    if (fbIsDeviceRegisteredForRemoteMessages(messaging)) return;
    await fbRegisterDeviceForRemoteMessages(messaging);
}

function getDeviceToken() {
    return new Promise((resolve) => {
        let settled = false;
        const finish = (value) => {
            if (settled) return;
            settled = true;
            resolve(value);
        };
        const timer = setTimeout(() => finish(null), GetTokenTimeoutMs);
        const messaging = getMessaging();
        fbGetToken(messaging).then((token) => {
            clearTimeout(timer);
            finish(token || null);
        }).catch(() => {
            clearTimeout(timer);
            finish(null);
        });
    });
}

async function resolveDeviceToken() {
    try {
        await ensureRegisteredForRemoteMessages();
    } catch (error) {
        return null;
    }
    return getDeviceToken();
}

async function registerDevice(authToken) {
    if (!authToken) return;
    try {
        const deviceToken = await resolveDeviceToken();
        if (!deviceToken) return;
        await baseService.postJson('device/registerDevice', { AuthToken: authToken, Token: deviceToken, Platform: Platform.OS });
    } catch (error) {
    }
}

async function unregisterDevice(authToken) {
    if (!authToken) return;
    try {
        const deviceToken = await resolveDeviceToken();
        if (!deviceToken) return;
        await baseService.postJson('device/unregisterDevice', { AuthToken: authToken, Token: deviceToken });
    } catch (error) {
    }
}

function onTokenRefresh(callback) {
    const messaging = getMessaging();
    return fbOnTokenRefresh(messaging, callback);
}

function onForegroundMessage(callback) {
    const messaging = getMessaging();
    return fbOnMessage(messaging, callback);
}

function onNotificationOpenedApp(callback) {
    const messaging = getMessaging();
    return fbOnNotificationOpenedApp(messaging, callback);
}

async function getInitialNotification() {
    const messaging = getMessaging();
    return fbGetInitialNotification(messaging);
}

function setBackgroundMessageHandler(handler) {
    const messaging = getMessaging();
    fbSetBackgroundMessageHandler(messaging, handler);
}

const pushNotificationService = {
    requestPermission,
    registerDevice,
    unregisterDevice,
    onTokenRefresh,
    onForegroundMessage,
    onNotificationOpenedApp,
    getInitialNotification,
    setBackgroundMessageHandler
};

export default pushNotificationService;