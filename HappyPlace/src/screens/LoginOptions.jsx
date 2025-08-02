import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, HappyColorFade, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidthPercent, scaleHeightPercent} from 'src/utils/scaleLayout';
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
    height: scaleHeightPercent(173),
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
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    width: scaleWidthPercent(42),
    height: scaleHeightPercent(42, 173),
    top: scaleHeightPercent(6, 173),
    left: scaleWidthPercent(20),
    backgroundColor: '#D84863',
    borderRadius: 99
  },
  backArrowIcon: {
    width: scaleWidthPercent(28, 42),
    height: scaleHeightPercent(28, 42),
    resizeMode: 'contain'
  },
  logoImg: {
    width: scaleWidthPercent(108),
    height: scaleHeightPercent(108, 173),
    resizeMode: 'contain'
  },
  card: {
    width: '100%',
    height: scaleHeightPercent(639),
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    shadowRadius: 30,
    elevation: 5,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: scaleHeightPercent(20, 595)
  },
  header: {
    height: scaleHeightPercent(62, 595),
    justifyContent: 'space-between'
  },
  loginOptions1: {
    width: scaleWidthPercent(335),
    height: scaleHeightPercent(254, 595),
    justifyContent: 'space-between',
  },
  divider: {
    width: scaleWidthPercent(335),
    height: scaleHeightPercent(21, 595),
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidthPercent(8, 335)
  },
  loginOptions2: {
    width: scaleWidthPercent(335),
    height: scaleHeightPercent(56, 595)
  },
  termsPolicy: {
    width: scaleWidthPercent(335),
    height: scaleHeightPercent(48, 595),
    flexWrap: 'wrap',
    gap: scaleWidthPercent(6, 335),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    textAlign: 'center',
    color: Black,
    fontSize: scaleFont(24),
    fontWeight: 700,
    lineHeight: scaleHeight(36)
  },
  subhead: {
    color: 'rgba(35, 35, 35, 0.50)',
    fontSize: scaleFont(16),
    fontWeight: 500,
    lineHeight: scaleHeight(24)
  },
  loginOption1Btn: {
    width: '100%',
    height: scaleHeightPercent(56, 254),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(8, 335),
    borderWidth: 1,
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade,
    borderRadius: 67.067,
  },
  icons: {
    width: scaleWidthPercent(24, 335),
    height: scaleHeightPercent(24, 56),
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    color: Black,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  line: {
    width: scaleWidthPercent(153, 335),
    height: scaleHeightPercent(1, 21),
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: scaleFont(14),
    fontWeight: 600,
    lineHeight: scaleHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14)
  },
  signUpBtn: {
    width: '100%',
    height: '100%',
    gap: scaleWidthPercent(10, 335),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    borderRadius: 99,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    color: White,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  termsPolicyBlackTxt: {
    color: Black,
    fontSize: scaleFont(16),
    fontWeight: 500,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  termsPolicyHappyTxt: {
    color: HappyColor,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
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
    height: scaleHeightPercent(265.00398, 1133),
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  backArrow: {
    width: scaleWidthPercent(56.336),
    height: scaleHeightPercent(56.336, 265.00398),
    justifyContent: 'center',
    alignItems: 'center',
    position: 'absolute',
    top: 22,
    left: 24,
    backgroundColor: '#D84863',
    borderRadius: 132.792
  },
  backArrowIcon: {
    width: scaleWidthPercent(37.55733, 56.336),
    height: scaleHeightPercent(37.55733, 56.336),
    resizeMode: 'contain'
  },
  logoImg: {
    width: scaleWidthPercent(144.86398),
    height: scaleHeightPercent(144.86398, 265.00398),
    resizeMode: 'contain'
  },
  card: {
    height: scaleHeightPercent(868),
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    shadowRadius: 30,
    elevation: 5,
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: scaleHeightPercent(24, 828)
  },
  header: {
    height: scaleHeightPercent(70, 828),
    justifyContent: 'space-between'
  },
  loginOptions1: {
    width: scaleWidthPercent(696, 744),
    height: scaleHeightPercent(352.768, 828),
    justifyContent: 'space-between',
  },
  divider: {
    width: scaleWidthPercent(696, 744),
    height: scaleHeightPercent(27, 828),
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidthPercent(10.73, 696)
  },
  loginOptions2: {
    width: scaleWidthPercent(696, 744),
    height: scaleHeightPercent(72.192, 828),
  },
  termsPolicy: {
    width: scaleWidthPercent(690.34668, 744),
    height: scaleHeightPercent(32, 828),
    flexWrap: 'wrap',
    gap: scaleWidthPercent(10, 690.34668),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  heading: {
    textAlign: 'center',
    color: Black,
    fontSize: scaleFont(26),
    fontWeight: 700,
    lineHeight: scaleHeight(39)
  },
  subhead: {
    color: 'rgba(35, 35, 35, 0.50)',
    fontSize: scaleFont(18),
    fontWeight: 500,
    lineHeight: scaleHeight(27)
  },
  loginOption1Btn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(10.73, 696),
    borderWidth: 1.341,
    borderColor: 'rgba(237, 83, 112, 0.20)',
    backgroundColor: HappyColorFade,
    borderRadius: 89.959,
    height: scaleHeightPercent(72.192, 336.768),
  },
  icons: {
    width: scaleWidthPercent(32.192, 696),
    height: scaleHeightPercent(32.192, 72.192),
    resizeMode: 'contain'
  },
  loginOption1BtnText: {
    color: Black,
    fontSize: scaleFont(20),
    fontWeight: 600,
    lineHeight: scaleHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2)
  },
  line: {
    width: scaleWidthPercent(328.76935, 696),
    height: scaleHeightPercent(1.341, 27),
    backgroundColor: 'rgba(35, 35, 35, 0.20)',
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: scaleFont(18),
    fontWeight: 600,
    lineHeight: scaleHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18)
  },
  signUpBtn: {
    width: '100%',
    height: '100%',
    gap: scaleWidthPercent(13.41, 696),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    borderRadius: 132.792,
    backgroundColor: HappyColor
  },
  signUpBtnText: {
    color: White,
    fontSize: scaleFont(20),
    fontWeight: 600,
    lineHeight: scaleHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2)
  },
  termsPolicyBlackTxt: {
    color: Black,
    fontSize: scaleFont(21.461),
    fontWeight: 500,
    lineHeight: scaleHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215)
  },
  termsPolicyHappyTxt: {
    color: HappyColor,
    fontSize: scaleFont(21.461),
    fontWeight: 600,
    lineHeight: scaleHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215),
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