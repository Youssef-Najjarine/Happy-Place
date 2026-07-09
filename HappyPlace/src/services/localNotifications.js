import { Platform } from 'react-native';
import notifee from '@notifee/react-native';

async function cancelByCollapseId(collapseId) {
    if (!collapseId) return;
    if (Platform.OS === 'android') {
        await cancelAndroidNotificationsByTag(collapseId);
        return;
    }
    try {
        await notifee.cancelDisplayedNotification(collapseId);
    } catch (error) {
    }
}

async function cancelAndroidNotificationsByTag(tag) {
    try {
        await notifee.cancelDisplayedNotification('0', tag);
    } catch (error) {
    }
    try {
        const displayedNotifications = await notifee.getDisplayedNotifications();
        for (const displayedNotification of displayedNotifications) {
            const androidTag = displayedNotification.notification && displayedNotification.notification.android ? displayedNotification.notification.android.tag : null;
            if (androidTag === tag || displayedNotification.id === tag) {
                await notifee.cancelDisplayedNotification(displayedNotification.id, androidTag || undefined);
            }
        }
    } catch (error) {
    }
}

const localNotifications = {
    cancelByCollapseId
};

export default localNotifications;