import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, HappyColorFade, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale} from 'src/utils/scaleLayout';
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
    height: '21.3%',
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
    width: scaleWidth(42),
    height: scaleHeight(42),
    top: scaleHeight(6),
    left: scaleWidth(20),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    backgroundColor: '#D84863'
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
    resizeMode: 'contain'
  },
  logoImg: {
    width: scaleWidth(108),
    height: scaleHeight(108),
    resizeMode: 'contain'
  },
  card: {
    flex: 1,
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    borderTopLeftRadius: scaleWidth(24),
    borderTopRightRadius: scaleWidth(24),
    paddingTop: scaleHeight(20),
    width: '100%',
    height: '78.7%',
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  header: {
    height: scaleHeight(62),
    justifyContent: 'space-between'
  },
  loginOptions1: {
    width: scaleWidth(335),
    height: scaleHeight(254),
    justifyContent: 'space-between'
  },
  divider: {
    width: scaleWidth(335),
    height: scaleHeight(21),
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  loginOptions2: {
    width: scaleWidth(335),
    height: scaleHeight(56)
  },
  termsPolicy: {
    width: scaleWidth(335),
    height: scaleHeight(52),
    gap: scaleWidth(6),
    flexWrap: 'wrap',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    textAlign: 'center',
    color: Black,
    fontWeight: 700
  },
  subhead: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    color: 'rgba(35, 35, 35, 0.50)',
    fontWeight: 500
  },
  loginOption1Btn: {
    height: scaleHeight(56),
    gap: scaleWidth(8),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade
  },
  icons: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  line: {
    width: scaleWidth(153),
    height: scaleHeight(1),
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600
  },
  signUpBtn: {
    gap: scaleWidth(10),
    borderRadius: scaleWidth(99),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  },
  termsPolicyBlackTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 500
  },
  termsPolicyHappyTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 600,
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
    width: '100%',
    height: '23.4%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  backArrow: {
    width: scaleWidth(56.336),
    height: scaleHeight(56.336),
    top: scaleHeight(22),
    left: scaleWidth(24),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    backgroundColor: '#D84863'
  },
  backArrowIcon: {
    width: scaleWidth(37.55733),
    height: scaleHeight(37.55733),
    resizeMode: 'contain'
  },
  logoImg: {
    width: scaleWidth(144.86398),
    height: scaleHeight(144.86398),
    resizeMode: 'contain'
  },
  card: {
    paddingTop: scaleHeight(24),
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    height: '76.6%',
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  header: {
    height: scaleHeight(70),
    justifyContent: 'space-between'
  },
  loginOptions1: {
    width: scaleWidth(696),
    height: scaleHeight(352.768),
    justifyContent: 'space-between',
  },
  divider: {
    width: scaleWidth(696),
    height: scaleHeight(27),
    gap: scaleWidth(10.73),
    flexDirection: 'row',
    alignItems: 'center'
  },
  loginOptions2: {
    width: scaleWidth(696),
    height: scaleHeight(72.192)
  },
  termsPolicy: {
    width: scaleWidth(690.34668),
    height: scaleHeight(32),
    flexWrap: 'wrap',
    gap: scaleWidth(10),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    textAlign: 'center',
    color: Black,
    fontWeight: 700
  },
  subhead: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    color: 'rgba(35, 35, 35, 0.50)',
    fontWeight: 500,
  },
  loginOption1Btn: {
    gap: scaleWidth(10.73),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    height: scaleHeight(72.192),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade
  },
  icons: {
    width: scaleWidth(32.192),
    height: scaleHeight(32.192),
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  line: {
    width: scaleWidth(328.76935),
    height: scaleHeight(1.341),
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    fontWeight: 600
  },
  signUpBtn: {
    gap: scaleWidth(13.41),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: White,
    fontWeight: 600
  },
  termsPolicyBlackTxt: {
    fontSize: scaleFont(21.461),
    lineHeight: scaleLineHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215),
    color: Black,
    fontWeight: 500
  },
  termsPolicyHappyTxt: {
    fontSize: scaleFont(21.461),
    lineHeight: scaleLineHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215),
    color: HappyColor,
    fontWeight: 600,
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
  };

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