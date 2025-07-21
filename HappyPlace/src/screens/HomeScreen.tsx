// src/screens/HomeScreen.tsx

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Image } from 'react-native';

const HappyColor = '#F26C89';  // Main pink
const White = '#fff';
const Dark = '#222';

export default function HomeScreen() {
  return (
    <View style={styles.root}>
      {/* Top Section: Logo and App Name */}
      <View style={styles.topSection}>
        <View style={styles.logoWrap}>
          {/* Replace with your SVG/Image asset as appropriate */}
          {/* If you have a logo image, import and place it in Image below */}
          {/* <Image source={require('../../assets/logo.png')} style={styles.logoImg}/> */}
          <Text style={styles.logoH}>H</Text>
          <Text style={styles.logoText}>Happy{'\n'}Place</Text>
        </View>
      </View>

      {/* Bottom Section: Card */}
      <View style={styles.card}>
        <Text style={styles.heading}>What's your issue?</Text>
        <Text style={styles.subhead}>Someone is here to help.</Text>

        <TouchableOpacity style={styles.helpMeBtn}>
          <Text style={styles.btnIcon}>😟</Text>
          <Text style={styles.helpMeBtnText}>HELP ME</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.iCanHelpBtn}>
          <Text style={styles.btnIcon}>🙂</Text>
          <Text style={styles.iCanHelpBtnText}>I CAN HELP</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.signUpBtn}>
          <Text style={styles.signUpBtnText}>Sign Up</Text>
        </TouchableOpacity>

        <View style={styles.divider}>
          <View style={styles.line} />
          <Text style={styles.or}>or</Text>
          <View style={styles.line} />
        </View>

        <Text style={styles.loginText}>
          Already have an account?{' '}
          <Text style={styles.loginLink}>Login</Text>
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: HappyColor,
  },
  topSection: {
    flex: 1.2,
    justifyContent: 'center',
    alignItems: 'center',
  },
  logoWrap: {
    backgroundColor: White,
    borderRadius: 160,
    width: 220,
    height: 220,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: Dark,
    shadowOpacity: 0.1,
    shadowRadius: 20,
    elevation: 8,
    marginTop: 32,
  },
  logoH: {
    fontSize: 70,
    color: HappyColor,
    fontWeight: 'bold',
    fontFamily: 'Avenir-Heavy', // Use your font if needed
    marginBottom: 4,
  },
  logoText: {
    fontSize: 32,
    textAlign: 'center',
    color: HappyColor,
    fontFamily: 'Avenir-Heavy',
    fontWeight: 'bold',
    marginTop: 0,
    lineHeight: 33,
  },
  card: {
    flex: 2,
    backgroundColor: White,
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    paddingTop: 30,
    paddingHorizontal: 18,
    shadowColor: Dark,
    shadowOpacity: 0.08,
    shadowRadius: 15,
    elevation: 12,
  },
  heading: {
    fontSize: 26,
    color: HappyColor,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 6,
  },
  subhead: {
    fontSize: 16,
    color: Dark,
    textAlign: 'center',
    marginBottom: 25,
  },
  helpMeBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: HappyColor,
    borderRadius: 40,
    paddingVertical: 16,
    paddingHorizontal: 40,
    width: '95%',
    backgroundColor: White,
    marginBottom: 14,
  },
  helpMeBtnText: {
    color: Dark,
    fontSize: 18,
    fontWeight: 'bold',
    marginLeft: 9,
    letterSpacing: 1,
  },
  iCanHelpBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 40,
    paddingVertical: 16,
    paddingHorizontal: 40,
    width: '95%',
    backgroundColor: HappyColor,
    marginBottom: 16,
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: 18,
    fontWeight: 'bold',
    marginLeft: 9,
    letterSpacing: 1,
  },
  btnIcon: {
    fontSize: 24,
  },
  signUpBtn: {
    backgroundColor: Dark,
    borderRadius: 20,
    paddingHorizontal: 32,
    paddingVertical: 8,
    alignItems: 'center',
    marginTop: 10,
    marginBottom: 10,
  },
  signUpBtnText: {
    color: White,
    fontSize: 16,
    fontWeight: 'bold',
    letterSpacing: 1,
  },
  divider: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '80%',
    marginVertical: 8,
  },
  line: {
    flex: 1,
    height: 1,
    backgroundColor: '#ccc',
    marginHorizontal: 10,
  },
  or: {
    fontSize: 16,
    color: Dark,
    marginVertical: 0,
    fontWeight: '500',
  },
  loginText: {
    color: Dark,
    fontSize: 15,
    textAlign: 'center',
    marginTop: 5,
    marginBottom: 8,
    letterSpacing: 0.2,
  },
  loginLink: {
    color: HappyColor,
    fontWeight: 'bold',
  },
});
