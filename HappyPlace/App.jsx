import React from 'react';
import { StatusBar, useColorScheme, StyleSheet } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import Home from 'screens/Home';
import LoginOptions from 'screens/LoginOptions';
import Login from 'screens/Login';
import ChatGroups from 'screens/ChatGroups';

const Stack = createNativeStackNavigator();

const App = () => {
  const isDarkMode = useColorScheme() === 'dark';

  return (
    <SafeAreaProvider>
      <NavigationContainer>
        <StatusBar barStyle={isDarkMode ? 'light-content' : 'dark-content'} />
        <Stack.Navigator initialRouteName="Home" screenOptions={{ headerShown: false }}>
          <Stack.Screen name="Home" component={Home} 
            options={{
              animation: 'slide_from_right'
            }}
          />
          <Stack.Screen name="LoginOptions" component={LoginOptions}/>
          <Stack.Screen name="Login" component={Login}/>
          <Stack.Screen name="ChatGroups" component={ChatGroups} />
        </Stack.Navigator>
      </NavigationContainer>
    </SafeAreaProvider>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1
  },
});

export default App;