import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Image, LogBox } from 'react-native';
import HappyEmoji from 'assets/images/happy-emoji.svg';
import SadEmoji from 'assets/images/sad-emoji.svg';
import Logo from 'assets/images/Logo.png';

const HappyColor = '#ED5370';
const White = '#FFFFFF';
const Dark = '#222';

export default function HomeScreen() {
  return (
    <View style={styles.root}>
      <View style={styles.topSection}>
        <View style={styles.logoBox}>
          <Image
            source={Logo}
            style={styles.logoImg}
            resizeMode="contain"
            accessible={true}
            accessibilityLabel="App logo"
          />
        </View>
      </View>

      {/* Bottom Section: Card */}
      <View style={styles.card}>
        <Text style={styles.heading}>What's your issue?</Text>
        <Text style={styles.subhead}>Someone is here to help.</Text>

        <TouchableOpacity style={styles.helpMeBtn}>
          <SadEmoji width={32} height={32} />
          <Text style={styles.helpMeBtnText}>HELP ME</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.iCanHelpBtn}>
          <HappyEmoji width={32} height={32} />
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
    height: '40%',
    width: '100%'
  },
  logoBox: {
      height: '100%',
      width: '100%',
      alignItems: 'center',
      justifyContent: 'center',
  },
  logoImg: {
    width: '50%',
    height: 188
  },
  card: {
    height:'60%',
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
    justifyContent: "center",
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: HappyColor,
    borderRadius: 40,
    paddingVertical: 16,
    paddingHorizontal: 40,
    width: '83%',
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
    justifyContent: "center",
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 40,
    paddingVertical: 16,
    paddingHorizontal: 40,
    width: '83%',
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
