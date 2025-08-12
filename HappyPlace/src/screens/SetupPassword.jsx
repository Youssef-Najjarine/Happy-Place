import React, { useState, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import GreenCheckIcon from 'assets/images/global/green-check-icon.svg';
import RedXIcon from 'assets/images/global/red-x-icon.svg';
import KeyIcon from 'assets/images/global/key-icon.svg';
import EyeIcon from 'assets/images/global/eye-icon.svg';
import EyeSlashIcon from 'assets/images/global/eye-slash-icon.svg';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  part2: {
    height: scaleHeight(89)
  },
  card: {
    shadowRadius: scaleWidth(30),
    elevation: moderateScale(5),
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingTop: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    height: '100%',
    flex: 1,
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
  },
  headers: {
    marginBottom: scaleHeight(24),
    width: '100%',
    alignItems: 'center',
  },
  setupPasswordHeader: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black
  },
  setupPasswordDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
  },
  inputsCredentials: {
    gap: scaleHeight(12)
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: Black
  },
  inputSelected: {
    borderColor: '#E86062'
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
    gap: scaleHeight(8)
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
    color: '#232323'
  },
  setupPassword: {
    marginBottom: scaleHeight(10)
  },
  setupPasswordBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  setupPasswordBtnText: {
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
  part2: {
    height: scaleHeight(114.602)
  },
  card: {
    marginTop: scaleHeight(20),
    paddingTop: scaleHeight(26.83),
    paddingHorizontal: scaleWidth(24),
    elevation: moderateScale(12),
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    width: '100%',
    height: '100%',
    flex: 1,
    backgroundColor: White,
    shadowColor: '#094173',
    shadowOpacity: 0.10,
    shadowOffset: { width: 0, height: 10.731 },
    shadowRadius: 20.12,
    justifyContent: 'space-between'
  },
  headers: {
      marginBottom: scaleHeight(32.19),
    width: '100%',
    alignItems: 'center'
  },
  setupPasswordHeader: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(4),
    fontWeight: 700,
    color: Black
  },
  setupPasswordDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)'
  },
  inputsCredentials: {
    gap: scaleHeight(16)
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
    position: 'absolute'
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
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
    gap: scaleHeight(12)
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
    color: '#232323'
  },
  setupPassword: {
    marginBottom: scaleHeight(12)
  },
  setupPasswordBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  setupPasswordBtnText: {
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

export default function SetupPassword() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [password, setPassword] = useState('');
  const [confirmPassword,setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [focusedField, setFocusedField] = useState(null);
  useFocusEffect(
  useCallback(() => {
    setPassword('');
    setConfirmPassword('');
    }, [])
    );
  const hasMinLen = (v) => v.length >= 8;
  const hasNumber = (v) => /\d/.test(v);
  const hasLowerUpper = (v) => /[a-z]/.test(v) && /[A-Z]/.test(v);
  const rules = useMemo(() => ({
    minLen: hasMinLen(password),
    number: hasNumber(password),
    lowerUpper: hasLowerUpper(password),
    match: password.length > 0 && password === confirmPassword,
  }), [password, confirmPassword]);

const canSubmit = rules.minLen && rules.number && rules.lowerUpper && rules.match;
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
            <View style={styles.headers}>
                <CustomText style={styles.setupPasswordHeader}>Setup Password</CustomText>
                <CustomText style={styles.setupPasswordDesc}>Enter your new password below.</CustomText>
            </View>
            <View style={styles.inputsCredentials}>
                <View style={styles.passwordView}>
                    <CustomText style={styles.textBoxLabel}>New Password</CustomText>
                    <View>
                        <CustomTextInput
                            style={[
                                styles.input,
                                styles.largeRightPadding,
                                focusedField === 'password' && styles.inputSelected,
                            ]}
                            secureTextEntry={!showPassword}
                            value={password}
                            onChangeText={setPassword}
                            textContentType="newPassword"
                            autoComplete="password-new"
                            onFocus={() => setFocusedField('password')}
                            onBlur={() => setFocusedField(null)}
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
                        style={[
                            styles.input,
                            styles.largeRightPadding,
                            focusedField === 'confirm' && styles.inputSelected,
                        ]}
                        secureTextEntry={!showConfirmPassword}
                        value={confirmPassword}
                        onChangeText={setConfirmPassword}
                        textContentType="password"
                        autoComplete="password"
                        onFocus={() => setFocusedField('confirm')}
                        onBlur={() => setFocusedField(null)}
                        />
                        <TouchableOpacity
                        style={styles.eyeIcons}
                        onPress={() => setShowConfirmPassword(!showConfirmPassword)}
                        >
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
            <View style={styles.setupPassword}>
            <TouchableOpacity
                style={[
                    styles.setupPasswordBtn,
                    !canSubmit && { opacity: 0.5 }
                ]}
                disabled={!canSubmit}
            >
                <CustomText style={styles.setupPasswordBtnText}>Setup Password</CustomText>
            </TouchableOpacity>
            </View>
            <View style={styles.alreadyHaveAccount}>
                <CustomText style={styles.alreadyHaveAccountTxt}>Already have an account?</CustomText>
                <TouchableOpacity onPress={() => navigation.navigate('LoginOptions')}>
                    <CustomText style={styles.loginTxt}>Login</CustomText>
                </TouchableOpacity>
            </View>
        </View>
      </View>
    </View>
  );
}