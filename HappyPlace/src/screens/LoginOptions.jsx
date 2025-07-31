import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, HappyColorFade, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import CustomText from 'src/components/FontFamilyText';
import BackArrow from 'assets/images/loginOptions/back-arrow-white-icon.svg';
import Logo from 'assets/images/global/logo.png';
import FacebookIcon from 'assets/images/loginOptions/facebook-icon.png';
import AppleIcon from 'assets/images/loginOptions/apple-icon.png';
import GoogleIcon from 'assets/images/loginOptions/google-icon.png';
import HappyEmailIcon from 'assets/images/loginOptions/happy-email-icon.svg';
import EmailIcon from 'assets/images/loginOptions/email-icon.svg';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '21.4%',
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  backArrow: {
    width: '11.2%',
    height: '25%',
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    top: 6,
    left: 20,
    backgroundColor: '#D84863',
    borderRadius: 99
  },
  backArrowIcon: {
    width: '67%',
    height: '67%',
    resizeMode: 'contain'
  },
  logoImg: {
    width: '29%',
    height: '62.1%',
    resizeMode: 'contain'
  },
  card: {
    height:'78.6%',
    backgroundColor: White,
    boxShadow: '0 8px 30px 0 rgba(9, 65, 115, 0.10)',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 20
  },
  header: {
    height: '11.1%',
    justifyContent: 'space-between'
  },
  loginOptions1: {
    height: '43%',
    width: '89%',
    justifyContent: 'space-between',
  },
  divider: {
    width: '89%',
    height: '4%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  loginOptions2: {
    width: '89%',
    height: '9.4%'
  },
  termsPolicy: {
    width: '89%',
    flexWrap: 'wrap',
    gap: 7,
    height: '8%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    textAlign: 'center',
    color: Black,
    fontSize: 24,
    fontWeight: 700,
    lineHeight: 36
  },
  subhead: {
    color: 'rgba(35, 35, 35, 0.50)',
    fontSize: 16,
    fontWeight: 500,
    lineHeight: 24
  },
  loginOption1Btn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    borderWidth: 1,
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade,
    borderRadius: 67.067,
    height: '22%',
  },
  icons: {
    width: '7.2%',
    height: '43%',
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    color: Black,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16
  },
  line: {
    width: '46%',
    height: 1,
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: 14,
    fontWeight: 600,
    lineHeight: 21,
    letterSpacing: -0.14
  },
  signUpBtn: {
   width: '100%',
   height: '100%',
   gap: 10,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    borderRadius: 99,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    color: White,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16
  },
  termsPolicyBlackTxt: {
    color: Black,
    fontSize: 16,
    fontWeight: 500,
    lineHeight: 24,
    letterSpacing: -0.16
  },
  termsPolicyHappyTxt: {
    color: HappyColor,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16,
    textDecorationLine: 'underline'
  }
});
const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '23.4%',
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  backArrow: {
    width: '7.6%',
    height: '21.3%',
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    top: 22,
    left: 24,
    backgroundColor: '#D84863',
    borderRadius: 132.792
  },
  backArrowIcon: {
    width: '67%',
    height: '67%',
    resizeMode: 'contain'
  },
  logoImg: {
    width: '19.5%',
    height: '55%',
    resizeMode: 'contain'
  },
  card: {
    height:'76.6%',
    backgroundColor: White,
    boxShadow: '0 8px 30px 0 rgba(9, 65, 115, 0.10)',
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 24
  },
  header: {
    height: '8.5%',
    justifyContent: 'space-between'
  },
  loginOptions1: {
    height: '67.3%',
    width: '94%',
    justifyContent: 'space-between',
  },
  divider: {
    width: '94%',
    height: '3.3%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  loginOptions2: {
    width: '94%',
    height: '9%'
  },
  termsPolicy: {
    width: '94%',
    flexWrap: 'wrap',
    height: '4%',
    gap: 10,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    textAlign: 'center',
    color: Black,
    fontSize: 26,
    fontWeight: 700,
    lineHeight: 39
  },
  subhead: {
    color: 'rgba(35, 35, 35, 0.50)',
    fontSize: 18,
    fontWeight: 500,
    lineHeight: 27
  },
  loginOption1Btn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10.73,
    borderWidth: 1.341,
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade,
    borderRadius: 89.959,
    height: '21.4%',
  },
  icons: {
    width: '5%',
    height: '45%',
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    color: Black,
    fontSize: 20,
    fontWeight: 600,
    lineHeight: 30,
    letterSpacing: -0.2
  },
  line: {
    width: '47.2%',
    height: 1.341,
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: 18,
    fontWeight: 600,
    lineHeight: 27,
    letterSpacing: -0.18
  },
  signUpBtn: {
   width: '100%',
   height: '100%',
   gap: 13.41,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    borderRadius: 132.792,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    color: White,
    fontSize: 20,
    fontWeight: 600,
    lineHeight: 30,
    letterSpacing: -0.2
  },
  termsPolicyBlackTxt: {
    color: Black,
    fontSize: 21.461,
    fontWeight: 500,
    lineHeight: 32.192,
    letterSpacing: -0.215
  },
  termsPolicyHappyTxt: {
    color: HappyColor,
    fontSize: 21.461,
    fontWeight: 600,
    lineHeight: 32.192,
    letterSpacing: -0.215,
    textDecorationLine: 'underline'
  }
});
export default function LoginOptions() {
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
          <TouchableOpacity 
            style={styles.backArrow}
            onPress={() => navigation.goBack()}
          >
            <BackArrow {...styles.backArrowIcon}/>
          </TouchableOpacity>
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
          <CustomText style={styles.heading}>Welcome Back!</CustomText>
          <CustomText style={styles.subhead}>Choose a way to Login to your account</CustomText>
        </View>

        <View style={styles.loginOptions1}>
          <TouchableOpacity style={styles.loginOption1Btn}>
            <Image source={FacebookIcon} style={styles.icons}/>
            <CustomText style={styles.loginOption1BtnText}>Sign in with Facebook</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.loginOption1Btn}>
            <Image source={AppleIcon} style={styles.icons}/>
            <CustomText style={styles.loginOption1BtnText}>Sign in with Apple</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.loginOption1Btn}>
            <Image source={GoogleIcon} style={styles.icons}/>
            <CustomText style={styles.loginOption1BtnText}>Sign in with Google</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.loginOption1Btn}>
            <HappyEmailIcon {...styles.icons}/>
            <CustomText style={styles.loginOption1BtnText}>Sign in with Email</CustomText>
          </TouchableOpacity>
        </View>
        <View style={styles.divider}>
          <View style={styles.line} />
          <CustomText style={styles.or}>or</CustomText>
          <View style={styles.line} />
        </View>
        <View style={styles.loginOptions2}>
          <TouchableOpacity style={styles.signUpBtn}>
            <EmailIcon {...styles.icons}/>
            <CustomText style={styles.signUpBtnText}>Sign up with Email</CustomText>
          </TouchableOpacity>
        </View>
        <View style={styles.termsPolicy}>
          <CustomText style={styles.termsPolicyBlackTxt}>
            By continuing, you agree to our 
          </CustomText>
          <CustomText style={styles.termsPolicyHappyTxt}>
            Terms of service 
          </CustomText>
          <CustomText style={styles.termsPolicyBlackTxt}>
            and
          </CustomText>
          <CustomText style={styles.termsPolicyHappyTxt}>
              Privacy Policy.
          </CustomText>
        </View>
      </View>
    </View>
  );
}