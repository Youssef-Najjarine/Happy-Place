import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black, VeryLightGray } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import { useNavigation } from '@react-navigation/native';
import CustomText from 'src/components/FontFamilyText';
import Logo from 'assets/images/global/logo.png';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '34.5%',
    width: '100%'
  },
  BackArrowView: {
    paddingHorizontal: scaleWidth(20),
  },  
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },  
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  logoImg: {
    width: scaleWidth(188),
    height: scaleHeight(188),
    resizeMode: 'contain'
  },
  card: {
    height: '65.5%',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingHorizontal: scaleWidth(20),
    backgroundColor: White,
    alignItems: 'center'
  },
  content: {
    flex: 1,
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleHeight(40)
  },
  header: {
    gap: scaleHeight(8),
    alignItems: 'center'
  },
  heading: {
    fontSize: scaleFont(32),
    lineHeight: scaleLineHeight(38.4),
    letterSpacing: scaleLetterSpacing(-0.32),
    color: HappyColor,
    fontWeight: 800,
    textAlign: 'center'
  },
  subhead: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    textAlign: 'center',
    fontWeight: 600
  },
  signUpLogIn: {
    width: scaleWidth(271),
    alignItems: 'center',
    gap: scaleHeight(16)
  },
  signUp: {
    height: scaleHeight(31),
    width: '100%',
    alignItems: 'center'
  },
  signUpBtn: {
    width: scaleWidth(112),
    borderRadius: scaleWidth(99),
    backgroundColor: Black,
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: White,
    fontWeight: 800
  },
  divider: {
    gap: scaleWidth(8),
    height: scaleHeight(21),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  line: {
    width: scaleWidth(121),
    height: scaleHeight(1),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    height: scaleHeight(24),
    gap: scaleWidth(5),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  loginText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  loginLink: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 600
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
  BackArrowView: {
    paddingHorizontal: scaleWidth(24),
  },   
  BackArrow: {
      borderRadius: scaleWidth(132.792),
      width: 78.14,
      height: 78.14,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557),
  },  
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  logoImg: {
    width: scaleWidth(252.17),
    height: scaleHeight(252.17),
    resizeMode: 'contain'
  },
  card: {
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    height: '64.6%',
    paddingHorizontal: scaleWidth(24),
    backgroundColor: White,
    alignItems: 'center'
  },
  content: {
    flex: 1,
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleHeight(48)
  },
  header: {
    gap: scaleHeight(12),
    alignItems: 'center'
  },
  heading: {
    fontSize: scaleFont(40),
    lineHeight: scaleLineHeight(48),
    letterSpacing: scaleLetterSpacing(-0.4),
    color: HappyColor,
    fontWeight: 800,
    textAlign: 'center'
  },
  subhead: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    color: Black,
    textAlign: 'center',
    fontWeight: 500
  },
  signUpLogIn: {
    width: scaleWidth(584),
    alignItems: 'center',
    gap: scaleHeight(20)
  },
  signUp: {
    height: scaleHeight(38.6533),
    width: '100%',
    alignItems: 'center'
  },
  signUpBtn: {
    width: scaleWidth(142.384),
    borderRadius: scaleWidth(132.792),
    backgroundColor: Black,
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    color: White,
    fontWeight: 800
  },
  divider: {
    gap: scaleWidth(10.73),
    height: scaleHeight(24),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  line: {
    width: scaleWidth(273.6935),
    height: scaleHeight(1.341),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    height: scaleHeight(30),
    gap: scaleWidth(5),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  loginText: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  loginLink: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: HappyColor,
    fontWeight: 600
  }
});

export default function FinishAccount() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();

  const handleSignUp = () => {
    navigation.navigate('CreateAccount');
  };

  const handleLogin = () => {
    navigation.navigate('LoginOptions');
  };

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
        <View style={styles.BackArrowView}>
        <TouchableOpacity
            style={styles.BackArrow}
            onPress={() => navigation.goBack()}
        >
            <BackArrow {...styles.backArrowIcon}/>
        </TouchableOpacity>
        </View>

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
        <View style={styles.content}>
          <View style={styles.header}>
            <CustomText style={styles.heading}>Finish creating your account</CustomText>
            <CustomText style={styles.subhead}>Save your progress and unlock all of Happy Place.</CustomText>
          </View>
          <View style={styles.signUpLogIn}>
            <View style={styles.signUp}>
              <TouchableOpacity style={styles.signUpBtn} onPress={handleSignUp}>
                <CustomText style={styles.signUpBtnText}>Sign Up</CustomText>
              </TouchableOpacity>
            </View>
            <View style={styles.divider}>
              <View style={styles.line} />
              <CustomText style={styles.or}>or</CustomText>
              <View style={styles.line} />
            </View>
            <View style={styles.alreadyHaveAccount}>
              <CustomText style={styles.loginText}>Already have an account?</CustomText>
              <CustomText style={styles.loginLink} onPress={handleLogin}>Login</CustomText>
            </View>
          </View>
        </View>
      </View>
    </View>
  );
}