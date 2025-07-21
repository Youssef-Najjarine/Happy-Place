import { AppRegistry } from 'react-native';
import App from './App.jsx'; // Explicitly specify .jsx
import { name as appName } from './app.json';

AppRegistry.registerComponent(appName, () => App);