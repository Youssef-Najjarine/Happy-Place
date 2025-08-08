import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet, TextInput } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, HappyColorFade, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import Check from 'assets/images/login/white-check-icon.svg';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EmailIcon from 'assets/images/global/email-outline-icon.svg';
import PhoneIcon from 'assets/images/global/phone-icon.svg';
import KeyIcon from 'assets/images/global/key-icon.svg';
import EyeIcon from 'assets/images/global/eye-icon.svg';
import EyeSlashIcon from 'assets/images/global/eye-slash-icon.svg';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  card: {
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    borderTopLeftRadius: scaleWidth(24),
    borderTopRightRadius: scaleWidth(24),
    width: '100%',
    height: '100%',
    paddingTop: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
  },
  part1: {
    height: scaleHeight(467)
  },
  part2: {
    height: scaleHeight(89)
  },
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    marginBottom: scaleHeight(24),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  signIn: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black,
  },
  signInDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginBottom: scaleHeight(24),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
  },
  signInType: {
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(1),
    paddingHorizontal: scaleWidth(4),
    marginBottom: scaleHeight(16),
    height: scaleHeight(48),
    width: '100%',
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  signInTypeSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  signInTypeNotSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: '#1D1E25'
  },
  emailPhoneView: {
    marginBottom: scaleHeight(12)
  },
  passwordView: {
    marginBottom: scaleHeight(16)
  },
  textBoxLabel: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    marginBottom: scaleHeight(4),
    fontWeight: 600,
    color: Black
  },
  textBoxIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    top: scaleHeight(12),
    left: scaleWidth(16),
    position: 'absolute',
  },
  input: {
    height: scaleHeight(48),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingLeft: scaleWidth(48),
    paddingVertical: scaleHeight(12),
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(16),
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: Black,
  },
  largeRightPadding: {
    paddingRight: scaleWidth(49.204),
  },
  eyeIcons: {
    top: scaleHeight(12),
    right: scaleWidth(16),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(25.204),
    height: scaleHeight(24)
  },
  rememberMeRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  rememberMe: {
    flexDirection: 'row',
    gap: scaleWidth(6)

  },
  rememberMeBtn: {
    width: scaleWidth(26.827),
    height: scaleHeight(26.827),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(10.731),
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',

  },
  rememberMeBtnSelected: {
    width: scaleWidth(26.827),
    height: scaleHeight(26.827),
    borderRadius: scaleWidth(10.731),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  checkIcon: {
    width: scaleWidth(11.401),
    height: scaleHeight(7.592)
  },
  rememberMeTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    fontWeight: 500,
    color: Black
  },
  forgotPasswordTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    fontWeight: 600,
    color: HappyColor
  },
  login: {
    marginBottom: scaleHeight(10)
  },
  loginBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  loginBtnText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  dontHaveAccount: {
    gap: scaleWidth(5),
    width: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  dontHaveAccountTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  signUp: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  card: {
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    borderTopLeftRadius: scaleWidth(24),
    borderTopRightRadius: scaleWidth(24),
    width: '100%',
    height: '100%',
    paddingTop: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
  },
  part1: {
    height: scaleHeight(467)
  },
  part2: {
    height: scaleHeight(89)
  },
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    marginBottom: scaleHeight(24),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  signIn: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black,
  },
  signInDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginBottom: scaleHeight(24),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
  },
  signInType: {
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(1),
    paddingHorizontal: scaleWidth(4),
    marginBottom: scaleHeight(16),
    height: scaleHeight(48),
    width: '100%',
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  signInTypeSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  signInTypeNotSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: '#1D1E25'
  },
  emailPhoneView: {
    marginBottom: scaleHeight(12)
  },
  passwordView: {
    marginBottom: scaleHeight(16)
  },
  textBoxLabel: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    marginBottom: scaleHeight(4),
    fontWeight: 600,
    color: Black
  },
  textBoxIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    top: scaleHeight(12),
    left: scaleWidth(16),
    position: 'absolute',
  },
  input: {
    height: scaleHeight(48),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingLeft: scaleWidth(48),
    paddingVertical: scaleHeight(12),
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(16),
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: Black,
  },
  largeRightPadding: {
    paddingRight: scaleWidth(49.204),
  },
  eyeIcons: {
    top: scaleHeight(12),
    right: scaleWidth(16),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(25.204),
    height: scaleHeight(24)
  },
  rememberMeRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  rememberMe: {
    flexDirection: 'row',
    gap: scaleWidth(6)

  },
  rememberMeBtn: {
    width: scaleWidth(26.827),
    height: scaleHeight(26.827),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(10.731),
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',

  },
  rememberMeBtnSelected: {
    width: scaleWidth(26.827),
    height: scaleHeight(26.827),
    borderRadius: scaleWidth(10.731),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  checkIcon: {
    width: scaleWidth(11.401),
    height: scaleHeight(7.592)
  },
  rememberMeTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    fontWeight: 500,
    color: Black
  },
  forgotPasswordTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    fontWeight: 600,
    color: HappyColor
  },
  login: {
    marginBottom: scaleHeight(10)
  },
  loginBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  loginBtnText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  dontHaveAccount: {
    gap: scaleWidth(5),
    width: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  dontHaveAccountTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  signUp: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  }
});

export default function Login() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [selectedSignInType, setSelectedSignInType] = useState('email');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
    const [rememberMe, setRememberMe] = useState(false);

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
      <View style={cardStyle}>
        <View style={styles.part1}>
            <TouchableOpacity 
            style={styles.BackArrow}
            onPress={() => navigation.goBack()}
            >
            <BackArrow {...styles.backArrowIcon}/>
            </TouchableOpacity>
            <CustomText style={styles.signIn}>Sign in</CustomText>
            <CustomText style={styles.signInDesc}>Sign In to your account</CustomText>
            <View style={styles.signInType}>
            <TouchableOpacity
                style={selectedSignInType === 'email' ? styles.signInTypeSelectedBtn : styles.signInTypeNotSelectedBtn}
                onPress={() => setSelectedSignInType('email')}
            >
                <CustomText style={selectedSignInType === 'email' ? styles.signInTypeSelectedtxt : styles.signInTypeNotSelectedTxt}>Email Address</CustomText>
            </TouchableOpacity>
            <TouchableOpacity
                style={selectedSignInType === 'phone' ? styles.signInTypeSelectedBtn : styles.signInTypeNotSelectedBtn}
                onPress={() => setSelectedSignInType('phone')}
            >
                <CustomText style={selectedSignInType === 'phone' ? styles.signInTypeSelectedtxt : styles.signInTypeNotSelectedTxt}>Phone Number</CustomText>
            </TouchableOpacity>
            </View>
            {selectedSignInType === 'email' && (
            <View style={styles.emailPhoneView}>
                <CustomText style={styles.textBoxLabel}>Email</CustomText>
                <View>
                    <TextInput
                    style={styles.input}
                    keyboardType="email-address"
                    autoCapitalize="none"
                    value={email}
                    onChangeText={setEmail}
                    />
                    <EmailIcon {...styles.textBoxIcon}/>
                </View>
            </View>
            )}
            {selectedSignInType === 'phone' && (
                <View style={styles.emailPhoneView}>
                    <CustomText style={styles.textBoxLabel}>Phone Number</CustomText>
                    <View>
                        <TextInput
                        style={styles.input}
                        keyboardType="phone-pad"
                        value={phone}
                        onChangeText={setPhone}
                        />
                        <PhoneIcon {...styles.textBoxIcon}/>
                    </View>
                </View>
            )}
            <View style={styles.passwordView}>
                <CustomText style={styles.textBoxLabel}>Password</CustomText>
                <View>
                    <TextInput
                        style={[styles.input, styles.largeRightPadding]}
                        secureTextEntry={!showPassword}
                        value={password}
                        onChangeText={setPassword}
                    />
                    <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowPassword(!showPassword)}>
                    {showPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                    </TouchableOpacity>
                    <KeyIcon {...styles.textBoxIcon}/>
                </View>
            </View>
            <View style={styles.rememberMeRow}>
                <View style={styles.rememberMe}>
                    <TouchableOpacity 
                        style={rememberMe ? styles.rememberMeBtnSelected : styles.rememberMeBtn}
                        onPress={() => setRememberMe(!rememberMe)}
                    >
                        {rememberMe && (
                                <Check {...styles.checkIcon}/>
                            )
                        }
                    </TouchableOpacity>
                    <CustomText style={styles.rememberMeTxt}>Remember me</CustomText>
                </View>
                <View>
                    <TouchableOpacity>
                        <CustomText style={styles.forgotPasswordTxt}>Forgot Password?</CustomText>
                    </TouchableOpacity>
                </View>
            </View>
        </View>
        <View style={styles.part2}>
            <View style={styles.login}>
                <TouchableOpacity style={styles.loginBtn}>
                <CustomText style={styles.loginBtnText}>Login</CustomText>
                </TouchableOpacity>
            </View>
            <View style={styles.dontHaveAccount}>
                <CustomText style={styles.dontHaveAccountTxt}>Don't have an account?</CustomText>
                <TouchableOpacity>
                    <CustomText style={styles.signUp}>Sign up</CustomText>
                </TouchableOpacity>
            </View>
        </View>
      </View>
    </View>
  );
}