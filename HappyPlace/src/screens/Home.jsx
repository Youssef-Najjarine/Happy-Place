import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { useNavigation } from '@react-navigation/native';
import CustomText from 'src/components/FontFamilyText';
import HappyEmoji from 'assets/images/home/happy-emoji.svg';
import SadEmoji from 'assets/images/home/sad-emoji.svg';
import Logo from 'assets/images/logo.png';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '35%',
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
    height: '67.2%',
    resizeMode: 'contain'
  },
  card: {
    height:'65%',
    backgroundColor: White,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 24
  },
  header: {
    height: '15.1%',
    justifyContent: 'space-between'
  },
  helpButtons: {
    height: '34.8%',
    width: '83%',
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: '73%',
    height: '19.1%',
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
    height: '45.2%',
    backgroundColor: White
  },
  emojis: {
    width: '10.3%',
    height: '42.1%',
    resizeMode: 'contain'
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
    height: '45.2%',
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: 24,
    fontWeight: 700,
    lineHeight: 36,
    letterSpacing: -0.48
  },
  signUp: {
    width: '100%',
    height: '34%',
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
    height: '23%'

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
    height: '26.1%',
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
  }
});
const tabletStyles = StyleSheet.create({
root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '35.4%',
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoImg: {
    width: '34%',
    height: '63%',
    resizeMode: 'contain'
  },
  card: {
    height:'64.6%',
    backgroundColor: White,
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 32
  },
  header: {
    height: '13.3%',
    justifyContent: 'space-between'
  },
  helpButtons: {
    width: '94%',
    height: '33%',
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: '79%',
    height: '16.5%',
    justifyContent: 'space-between'
  },
  heading: {
    color: HappyColor,
    fontSize: 50,
    fontWeight: 800,
    lineHeight: 48,
    letterSpacing: -0.4
  },
  subhead: {
    color: Black,
    textAlign: 'center',
    fontSize: 28,
    fontWeight: 500,
    lineHeight: 33,
    letterSpacing: -0.22
  },
  helpMeBtn: {
    width: '100%',
    height: '45.2%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 13.41,
    borderWidth: 2.012,
    borderColor: Black,
    borderRadius: 132.792,
    backgroundColor: White
  },
  emojis: {
    width: '6.2%',
    height: '42.2%',
    resizeMode: 'contain'
  },
  helpMeBtnText: {
    color: Black,
    fontSize: 32,
    fontWeight: 700,
    lineHeight: 48,
    letterSpacing: -0.64
  },
  iCanHelpBtn: {
    width: '100%',
    height: '45.2%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 13.41,
    borderWidth: 0,
    borderRadius: 132.792,
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: 32,
    fontWeight: 700,
    lineHeight: 48,
    letterSpacing: -0.64
  },
  signUp: {
    width: '100%',
    height: '34%',
    alignItems: 'center'
  },
  signUpBtn: {
    backgroundColor: Black,
    borderRadius: 132.792,
    width: '24.4%',
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    color: White,
    fontSize: 22,
    fontWeight: 800,
    lineHeight: 33,
    letterSpacing: -0.22
  },
  divider: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10.73,
    height: 24

  },
  line: {
    width: '47%',
    height: 2,
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: '26.3%',
    gap: 10
  },
  loginText: {
    color: Black,
    fontSize: 20,
    fontWeight: 600,
    lineHeight: 30,
    letterSpacing: -0.2
  },
  loginLink: {
    color: HappyColor,
    fontSize: 20,
    fontWeight: 600,
    lineHeight: 30,
    letterSpacing: -0.2
  }
});
export default function Home() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight
  };
  const cardStyle = {
    ...styles.card,
    paddingBottom: bottomSafeHeight
  }
  return (
    <View style={rootStyle}>
      <View style={styles.topSection}>
        <View style={styles.logoBox}>
          <Image
            source={Logo}
            style={styles.logoImg}
            accessible={true}
            accessibilityLabel="App logo"
          />
        </View>
      </View>

      <View style={cardStyle}>

        <View style={styles.header}>
          <CustomText style={styles.heading}>What's your issue?</CustomText>
          <CustomText style={styles.subhead}>Someone is here to help.</CustomText>
        </View>

        <View style={styles.helpButtons}>
          <TouchableOpacity style={styles.helpMeBtn} onPress={() => navigation.navigate('ChatGroups')}>
            <SadEmoji {...styles.emojis}/>
            <CustomText style={styles.helpMeBtnText}>HELP ME</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.iCanHelpBtn} onPress={() => navigation.navigate('ChatGroups')}>
            <HappyEmoji {...styles.emojis}/>
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
            <CustomText style={styles.loginLink} onPress={() => navigation.navigate('LoginOptions')}>Login</CustomText>
          </View>
        </View>
      </View>
    </View>
  );
}