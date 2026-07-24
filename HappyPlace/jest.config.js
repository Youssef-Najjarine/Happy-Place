const reactNativePreset = require('@react-native/jest-preset');

const scriptTransformer = reactNativePreset.transform['^.+\\.(js|ts|tsx)$'] || 'babel-jest';
const transform = { ...reactNativePreset.transform };
delete transform['^.+\\.(js|ts|tsx)$'];
transform['^.+\\.(js|jsx|ts|tsx)$'] = scriptTransformer;

module.exports = {
  ...reactNativePreset,
  transform,
  transformIgnorePatterns: [
    'node_modules/(?!((jest-)?react-native|@react-native(-community)?|@reduxjs/toolkit|immer|redux|redux-thunk|reselect|react-redux|@microsoft/signalr)/)'
  ]
};