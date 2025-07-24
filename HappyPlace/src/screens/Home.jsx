import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Image, LogBox } from 'react-native';
import CustomText from 'src/components/FontFamilyText';
import HappyEmoji from 'assets/images/happy-emoji.svg';
import SadEmoji from 'assets/images/sad-emoji.svg';
import Logo from 'assets/images/Logo.png';

export default function Home() {
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

      <View style={styles.card}>

        <View style={styles.header}>
          <CustomText style={styles.heading}>What's your issue?</CustomText>
          <CustomText style={styles.subhead}>Someone is here to help.</CustomText>
        </View>

        <View style={styles.helpButtons}>
          <TouchableOpacity style={styles.helpMeBtn}>
            <SadEmoji style={styles.emojis}/>
            <CustomText style={styles.helpMeBtnText}>HELP ME</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.iCanHelpBtn}>
            <HappyEmoji style={styles.emojis}/>
            <CustomText style={styles.iCanHelpBtnText}>I CAN HELP</CustomText>
          </TouchableOpacity>
        </View>

        <View style={styles.signUpLogIn}>
          <View style={styles.signUp}>
            <TouchableOpacity style={styles.signUpBtn}>
              <CustomText style={styles.signUpBtnText}>Sign Up</CustomText>
            </TouchableOpacity>
          </View>

          <View style={styles.divider}>
            <View style={styles.line} />
            <CustomText style={styles.or}>or</CustomText>
            <View style={styles.line} />
          </View>
          <View style={styles.alreadyHaveAccount}>
            <CustomText style={styles.loginText}>
              Already have an account?
            </CustomText>
            <CustomText style={styles.loginLink}>Login</CustomText>
          </View>
        </View>
      </View>
    </View>
  );
}

const HappyColor = '#ED5370';
const White = '#FFFFFF';
const Black = '#232323';
const styles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
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
    height: '59%'
  },
  card: {
    height:'60%',
    backgroundColor: White,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 24,
    paddingBottom: 34
  },
  header: {
    height: '15%',
    justifyContent: 'space-between'
  },
  helpButtons: {
    height: '35%',
    width: '83%',
    justifyContent: 'space-between',
  },
  signUpLogIn: {
    width: '73%',
    height: '19%',
    justifyContent: 'space-between'
  },
  heading: {
    color: HappyColor,
    fontSize: 32,
    fontWeight: 800,
    lineHeight: 38.4,
    letterSpacing: -0.32
  },
  subhead: {
    color: Black,
    textAlign: 'center',
    fontSize: 18,
    fontWeight: 600,
    lineHeight: 27,
    letterSpacing: -0.18
  },
  helpMeBtn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
    borderWidth: 1.5,
    borderColor: Black,
    borderRadius: 99,
    height: 76,
    backgroundColor: White
  },
  emojis: {
    width: 32,
    height: 32
  },
  helpMeBtnText: {
    color: Black,
    fontSize: 24,
    fontWeight: 700,
    lineHeight: 36,
    letterSpacing: -0.48
  },
  iCanHelpBtn: {
   width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
    borderWidth: 0,
    borderRadius: 99,
    height: 76,
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: 24,
    fontWeight: 700,
    lineHeight: 36,
  },
  signUp: {
    width: '100%',
    height: 31,
    alignItems: 'center'
  },
  signUpBtn: {
    backgroundColor: Black,
    borderRadius: 99,
    width: '41.3%',
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    color: White,
    fontSize: 18,
    fontWeight: 800,
    lineHeight: 27,
    letterSpacing: -0.18
  },
  divider: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    height: 21

  },
  line: {
    width: '45%',
    height: 1,
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: 14,
    fontWeight: 600,
    lineHeight: 21,
    letterSpacing: -0.14,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: 24,
    gap: 5
  },
  loginText: {
    color: Black,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16
  },
  loginLink: {
    color: HappyColor,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16
  },
});
