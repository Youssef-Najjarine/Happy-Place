import React from 'react';
import { StatusBar, useColorScheme, StyleSheet, View } from 'react-native';
import Home from 'screens/Home';
import ChatGroups from 'screens/chatGroups';

const App = () => {
  const isDarkMode = useColorScheme() === 'dark';

  return (
    <View style={styles.container}>
      <StatusBar barStyle={isDarkMode ? 'light-content' : 'dark-content'} />
      <Home />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1
  },
});

export default App;
