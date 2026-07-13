import React from 'react';
import { StatusBar, useColorScheme } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { Provider } from 'react-redux';
import store from 'store';
import navigationRef from 'src/services/navigationService';
import usePushNotifications from 'src/hooks/usePushNotifications';
import ErrorBoundary from 'components/ErrorBoundary';
import LoadingModal from 'components/LoadingModal';
import ToastHost from 'src/components/Toast';
import Home from 'screens/Home';
import ChatGroups from 'screens/ChatGroups';
import ChatGroup from 'screens/ChatGroup';
import OfferHelp from 'screens/OfferHelp';
import CreateAccount from 'screens/CreateAccount';
import FinishAccount from 'screens/FinishAccount';
import VerifyCode from 'screens/VerifyCode';
import AccountVerified from 'screens/AccountVerified';
import LoginOptions from 'screens/LoginOptions';
import Login from 'screens/Login';
import ForgotPassword from 'screens/ForgotPassword';
import SetupPassword from 'screens/SetupPassword';
import PasswordReset from 'screens/PasswordReset';
import Profile from 'screens/Profile';
import EditProfile from 'screens/EditProfile';
import AddNewEmailOrPhone from 'screens/AddNewEmailOrPhone';
import EditEmailOrPhone from 'screens/EditEmailOrPhone';
import Friends from 'screens/Friends';
import AddFriends from 'screens/AddFriends';
import BlockedUsers from 'screens/BlockedUsers';
import Members from 'screens/Members';
import TermsAndPrivacyInformation from 'screens/TermsAndPrivacyInformation';

const Stack = createNativeStackNavigator();

function PushNotificationsManager() {
  usePushNotifications();
  return null;
}

const App = () => {
  const isDarkMode = useColorScheme() === 'dark';

  return (
    <Provider store={store}>
      <SafeAreaProvider>
        <ErrorBoundary>
          <NavigationContainer ref={navigationRef}>
            <StatusBar barStyle={isDarkMode ? 'light-content' : 'dark-content'} />
            <Stack.Navigator initialRouteName="Home" screenOptions={{ headerShown: false }}>
              <Stack.Screen
                name="Home"
                component={Home}
                options={{
                  animation: 'slide_from_right',
                }}
              />
              <Stack.Screen name="CreateAccount" component={CreateAccount} />
              <Stack.Screen name="FinishAccount" component={FinishAccount} />
              <Stack.Screen name="VerifyCode" component={VerifyCode} />
              <Stack.Screen name="AccountVerified" component={AccountVerified} />
              <Stack.Screen name="ChatGroups" component={ChatGroups} />
              <Stack.Screen name="ChatGroup" component={ChatGroup} />
              <Stack.Screen name="OfferHelp" component={OfferHelp} />
              <Stack.Screen name="LoginOptions" component={LoginOptions} />
              <Stack.Screen name="Login" component={Login} />
              <Stack.Screen name="ForgotPassword" component={ForgotPassword} />
              <Stack.Screen name="SetupPassword" component={SetupPassword} />
              <Stack.Screen name="PasswordReset" component={PasswordReset} />
              <Stack.Screen name="Profile" component={Profile} />
              <Stack.Screen name="EditProfile" component={EditProfile} />
              <Stack.Screen name="AddNewEmailOrPhone" component={AddNewEmailOrPhone} />
              <Stack.Screen name="EditEmailOrPhone" component={EditEmailOrPhone} />
              <Stack.Screen name="Friends" component={Friends} />
              <Stack.Screen name="AddFriends" component={AddFriends} />
              <Stack.Screen name="BlockedUsers" component={BlockedUsers} />
              <Stack.Screen name="Members" component={Members} />
              <Stack.Screen name="TermsAndPrivacyInformation" component={TermsAndPrivacyInformation} />
            </Stack.Navigator>
            <LoadingModal />
          </NavigationContainer>
          <PushNotificationsManager />
          <ToastHost />
        </ErrorBoundary>
      </SafeAreaProvider>
    </Provider>
  );
};

export default App;