import React, { useState, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  Charcoal, 
  IndigoDye, 
  FrostedWhite,
  CharcoalNavy
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import CustomMaskedTextInput from 'src/components/FontFamilyMaskedTextInput';
import ProfileIcon from 'assets/images/createAccount/profile-icon.svg';
import GreenCheckIcon from 'assets/images/global/green-check-icon.svg';
import RedXIcon from 'assets/images/global/red-x-icon.svg';
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
  contentContainer: {
    paddingBottom: scaleHeight(10),
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingTop: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    backgroundColor: White,
    shadowColor: IndigoDye,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
  },
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    marginBottom: scaleHeight(16),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  createAccount: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black,
  },
  createAccountDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginBottom: scaleHeight(24),
    fontWeight: 500,
    color: Charcoal,
  },
  inputsCredentials: {
    gap: scaleHeight(12)
  },
  createAccountType: {
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(1),
    paddingHorizontal: scaleWidth(4),
    height: scaleHeight(48),
    width: '100%',
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  createAccountTypeSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  createAccountTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  createAccountTypeNotSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  createAccountTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: CharcoalNavy
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
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    paddingLeft: scaleWidth(48),
    paddingVertical: scaleHeight(12),
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  largeRightPadding: {
    paddingRight: scaleWidth(48)
  },
  eyeIcons: {
    top: scaleHeight(12),
    right: scaleWidth(16),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24)
  },
  passwordRequirementsView: {
    gap: scaleHeight(8),
    marginBottom: scaleHeight(64)
  },
  passwordRequirements: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center',
  },
  passwordCheckIcons: {
    width: scaleWidth(16),
    height: scaleHeight(16)

  },
  passwordRequirementTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(22),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  signUp: {
    marginBottom: scaleHeight(10)
  },
  signUpBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  alreadyHaveAccount: {
    gap: scaleWidth(5),
    width: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  alreadyHaveAccountTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  loginTxt: {
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
  contentContainer: {
    marginTop: scaleHeight(20),
    paddingTop: scaleHeight(26.83),
    paddingBottom: scaleHeight(12),
    paddingHorizontal: scaleWidth(24),
    elevation: moderateScale(12),
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    width: '100%',
    backgroundColor: White,
    shadowColor: IndigoDye,
    shadowOpacity: 0.10,
    shadowOffset: { width: 0, height: 10.731 },
    shadowRadius: 20.12,
    justifyContent: 'space-between'
  },
  BackArrow: {
    borderRadius: scaleWidth(132.792),
    marginBottom: scaleHeight(32),
    width: 78,
    height: 78,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557),
  },
  createAccount: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(4),
    fontWeight: 700,
    color: Black,
  },
  createAccountDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    marginBottom: scaleHeight(32),
    fontWeight: 500,
    color: Charcoal,
  },
  inputsCredentials: {
    gap: scaleHeight(16)
  },
  createAccountType: {
    borderRadius: scaleWidth(89.959),
    borderWidth: scaleWidth(1.341),
    paddingHorizontal: scaleWidth(6),
    height: scaleHeight(64),
    width: '100%',
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  createAccountTypeSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  createAccountTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  createAccountTypeNotSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  createAccountTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: CharcoalNavy
  },
  textBoxLabel: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.36),
    marginBottom: scaleHeight(6),
    fontWeight: 600,
    color: Black
  },
  textBoxIcon: {
    width: scaleWidth(32.192),
    height: scaleHeight(32.192),
    top: scaleHeight(16),
    left: scaleWidth(20),
    position: 'absolute',
  },
  input: {
    height: scaleHeight(64.192),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingLeft: scaleWidth(64.192),
    paddingVertical: scaleHeight(16),
    paddingRight: scaleWidth(20),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  largeRightPadding: {
    paddingRight: scaleWidth(64.19)
  },
  eyeIcons: {
    top: scaleHeight(16),
    right: scaleWidth(20),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(32.19),
    height: scaleHeight(32.19)
  },
  passwordRequirementsView: {
    gap: scaleHeight(12),
    marginBottom: scaleHeight(61.88)
  },
  passwordRequirements: {
    gap: scaleWidth(16.1),
    flexDirection: 'row',
    alignItems: 'center',
  },
  passwordCheckIcons: {
    width: scaleWidth(21.461),
    height: scaleHeight(21.461)

  },
  passwordRequirementTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(29.509),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  signUp: {
    marginBottom: scaleHeight(12)
  },
  signUpBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  alreadyHaveAccount: {
    gap: scaleWidth(7),
    width: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  alreadyHaveAccountTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  loginTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: HappyColor
  }
});

export default function CreateAccount() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [selectedCreateAccountType, setSelectedCreateAccountType] = useState('email');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword,setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  useFocusEffect(
  useCallback(() => {
    setName('');
    setEmail('');
    setPhone('');
    setPassword('');
    setConfirmPassword('');
    }, [])
    );
  const isEmail = (v) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim());
  const hasMinLen = (v) => v.length >= 8;
  const hasNumber = (v) => /\d/.test(v);
  const hasLowerUpper = (v) => /[a-z]/.test(v) && /[A-Z]/.test(v);
  const rules = useMemo(() => ({
    minLen: hasMinLen(password),
    number: hasNumber(password),
    lowerUpper: hasLowerUpper(password),
    match: password.length > 0 && password === confirmPassword,
  }), [password, confirmPassword]);

  const phoneValid = selectedCreateAccountType === 'phone' ? phone.replace(/\D/g, '').length >= 10 : false;
  const emailValid = selectedCreateAccountType === 'email' ? isEmail(email) : false;

  const nameValid = name.trim().length > 0;

  const canSubmit = nameValid &&
    (emailValid || phoneValid) &&
    rules.minLen && rules.number && 
    rules.lowerUpper && rules.match;

  const goToVerifyCode = () => {
    const contact = selectedCreateAccountType === 'email' ? email : phone;
    navigation.navigate('VerifyCode', { contact, source: 'createAccount' });
  };

  const rootStyle = {
  ...styles.root,
  paddingTop: statusBarHeight
  };
  const contentContainer = {
    ...styles.contentContainer,
    paddingBottom: bottomSafeHeight + styles.contentContainer.paddingBottom
  };

  return (
    <View style={rootStyle}>
      <ScrollView 
        style={{ flex: 1 }}
        contentContainerStyle={contentContainer}
      >
        <View style={styles.part1}>
            <TouchableOpacity 
                style={styles.BackArrow}
                onPress={() => navigation.goBack()}
            >
                <BackArrow {...styles.backArrowIcon}/>
            </TouchableOpacity>
            <CustomText style={styles.createAccount}>Create an Account</CustomText>
            <CustomText style={styles.createAccountDesc}>Fill the Details to setup your account</CustomText>
            <View style={styles.inputsCredentials}>
                <View style={styles.createAccountType}>
                    <TouchableOpacity
                        style={selectedCreateAccountType === 'email' ? styles.createAccountTypeSelectedBtn : styles.createAccountTypeNotSelectedBtn}
                        onPress={() => setSelectedCreateAccountType('email')}
                    >
                        <CustomText style={selectedCreateAccountType === 'email' ? styles.createAccountTypeSelectedtxt : styles.createAccountTypeNotSelectedTxt}>Email Address</CustomText>
                    </TouchableOpacity>
                    <TouchableOpacity
                        style={selectedCreateAccountType === 'phone' ? styles.createAccountTypeSelectedBtn : styles.createAccountTypeNotSelectedBtn}
                        onPress={() => setSelectedCreateAccountType('phone')}
                    >
                        <CustomText style={selectedCreateAccountType === 'phone' ? styles.createAccountTypeSelectedtxt : styles.createAccountTypeNotSelectedTxt}>Phone Number</CustomText>
                    </TouchableOpacity>
                </View>
                <View style={styles.emailPhoneView}>
                    <CustomText style={styles.textBoxLabel}>Full Name</CustomText>
                    <View>
                        <CustomTextInput
                          style={styles.input}
                          keyboardType="default"
                          autoCapitalize="words"   
                          autoCorrect={false}
                          textContentType="name"
                          autoComplete="name"  
                          value={name}
                          onChangeText={setName}
                        />
                        <ProfileIcon {...styles.textBoxIcon}/>
                    </View>
                </View>
                {selectedCreateAccountType === 'email' && (
                    <View style={styles.emailPhoneView}>
                        <CustomText style={styles.textBoxLabel}>Email</CustomText>
                        <View>
                            <CustomTextInput
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
                {selectedCreateAccountType === 'phone' && (
                    <View style={styles.emailPhoneView}>
                        <CustomText style={styles.textBoxLabel}>Phone Number</CustomText>
                        <View>
                            <CustomMaskedTextInput
                            style={styles.input}
                            mask="(999) 999-9999"
                            keyboardType="phone-pad"
                            value={phone}
                            onChangeText={(formatted, extracted) => setPhone(extracted || '')}
                            />
                            <PhoneIcon {...styles.textBoxIcon}/>
                        </View>
                    </View>
                )}
                <View style={styles.passwordView}>
                    <CustomText style={styles.textBoxLabel}>Password</CustomText>
                    <View>
                        <CustomTextInput
                            style={[styles.input, styles.largeRightPadding]}
                            secureTextEntry={!showPassword}
                            value={password}
                            onChangeText={setPassword}
                            textContentType="newPassword"
                            autoComplete="password-new"
                        />
                        <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowPassword(!showPassword)}>
                        {showPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                        </TouchableOpacity>
                        <KeyIcon {...styles.textBoxIcon}/>
                    </View>
                </View>
                <View style={styles.passwordView}>
                    <CustomText style={styles.textBoxLabel}>Confirm-Password</CustomText>
                    <View>
                        <CustomTextInput
                            style={[styles.input, styles.largeRightPadding]}
                            secureTextEntry={!showConfirmPassword}
                            value={confirmPassword}
                            onChangeText={setConfirmPassword}
                            textContentType="password"
                            autoComplete="password"
                        />
                        <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowConfirmPassword(!showConfirmPassword)}>
                        {showConfirmPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                        </TouchableOpacity>
                        <KeyIcon {...styles.textBoxIcon}/>
                    </View>
                </View>
                <View style={styles.passwordRequirementsView}>
                    {[
                        { ok: rules.minLen, text: 'Minimum 8 characters' },
                        { ok: rules.number, text: 'At least 1 number (0–9)' },
                        { ok: rules.lowerUpper, text: 'At least 1 lowercase and 1 uppercase letter' },
                        { ok: rules.match, text: 'Passwords matching' },
                    ].map((r, i) => (
                        <View key={i} style={styles.passwordRequirements}>
                        {r.ok ? <GreenCheckIcon {...styles.passwordCheckIcons}/> : <RedXIcon {...styles.passwordCheckIcons}/>}
                        <CustomText style={[styles.passwordRequirementTxt, { opacity: r.ok ? 1 : 0.7 }]}>
                            {r.text}
                        </CustomText>
                        </View>
                    ))}
                </View>
            </View>
        </View>
        <View style={styles.part2}>
            <View style={styles.signUp}>
              <TouchableOpacity
                  style={[
                      styles.signUpBtn,
                      !canSubmit && { opacity: 0.5 }
                  ]}
                  disabled={!canSubmit}
                  onPress={goToVerifyCode}
              >
                  <CustomText style={styles.signUpBtnText}>Sign up</CustomText>
              </TouchableOpacity>
            </View>
            <View style={styles.alreadyHaveAccount}>
                <CustomText style={styles.alreadyHaveAccountTxt}>Already have an account?</CustomText>
                <TouchableOpacity onPress={() => navigation.navigate('LoginOptions')}>
                    <CustomText style={styles.loginTxt}>Login</CustomText>
                </TouchableOpacity>
            </View>
        </View>
      </ScrollView>
    </View>
  );
}