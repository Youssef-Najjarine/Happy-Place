import { AppRegistry } from 'react-native';
import pushNotificationService from './src/services/pushNotificationService';
import localNotifications from './src/services/localNotifications';
import App from './App.jsx';
import { name as appName } from './app.json';
pushNotificationService.setBackgroundMessageHandler(async (remoteMessage) => {
    const data = remoteMessage && remoteMessage.data;
    if (data && data.type === 'dismiss' && data.collapseId) {
        await localNotifications.cancelByCollapseId(data.collapseId);
    }
});
AppRegistry.registerComponent(appName, () => App);