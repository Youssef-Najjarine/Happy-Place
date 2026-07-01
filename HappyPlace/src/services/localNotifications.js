import notifee from '@notifee/react-native';

async function cancelByCollapseId(collapseId) {
    if (!collapseId) return;
    try {
        await notifee.cancelDisplayedNotification(collapseId);
    } catch (error) {
    }
}

const localNotifications = {
    cancelByCollapseId
};

export default localNotifications;