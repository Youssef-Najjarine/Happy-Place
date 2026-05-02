import React, { useState, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet, Animated, Keyboard } from 'react-native';
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
  CharcoalNavy,
  BlushRose
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import CustomMaskedTextInput from 'src/components/FontFamilyMaskedTextInput';
import Check from 'assets/images/login/white-check-icon.svg';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EmailIcon from 'assets/images/global/email-outline-icon.svg';
import PhoneIcon from 'assets/images/global/phone-icon.svg';
import KeyIcon from 'assets/images/global/key-icon.svg';
import EyeIcon from 'assets/images/global/eye-icon.svg';
import EyeSlashIcon from 'assets/images/global/eye-slash-icon.svg';
import authenticationService from 'services/authenticationService';
import tokenStorage from 'services/tokenStorage';

const TOAST_DISPLAY_DURATION = 4000;

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
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
    shadowColor: IndigoDye,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
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
    backgroundColor: VeryLightGray
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
    color: Charcoal,
  },
  signInType: {
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(1),
    paddingHorizontal: scaleWidth(4),
    marginBottom: scaleHeight(16),
    height: scaleHeight(48),
    width: '100%',
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
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
    color: CharcoalNavy
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
    width: scaleWidth(25.204),
    height: scaleHeight(24)
  },
  rememberMeRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  rememberMe: {
    gap: scaleWidth(6),
    flexDirection: 'row',
    alignItems: 'center'
  },
  rememberMeBtn: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    borderWidth: scaleWidth(1.5),
    borderRadius: scaleWidth(8),
    borderColor: HappyColor,
    backgroundColor: BlushRose
  },
  rememberMeBtnSelected: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    borderRadius: scaleWidth(8),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  checkIcon: {
    width: scaleWidth(8.5),
    height: scaleHeight(5.66)
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
  toastContainer: {
    position: 'absolute',
    left: scaleWidth(20),
    right: scaleWidth(20),
    zIndex: 100
  },
  toast: {
    borderRadius: scaleWidth(12),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(12),
    backgroundColor: HappyColor,
    shadowColor: Black,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 6,
    elevation: 6
  },
  toastText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(20),
    fontWeight: 600,
    color: White,
    textAlign: 'center'
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
    shadowColor: IndigoDye,
    shadowOpacity: 0.10,
    shadowOffset: { width: 0, height: 10.731 },
    shadowRadius: 20.12,
    justifyContent: 'space-between'
  },
  part2: {
    height: scaleHeight(113.2)
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
  signIn: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(4),
    fontWeight: 700,
    color: Black,
  },
  signInDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    marginBottom: scaleHeight(32),
    fontWeight: 500,
    color: Charcoal,
  },
  signInType: {
    borderRadius: scaleWidth(89.959),
    borderWidth: scaleWidth(1.341),
    paddingHorizontal: scaleWidth(6),
    marginBottom: scaleHeight(24),
    height: scaleHeight(64),
    width: '100%',
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  signInTypeSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  signInTypeNotSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  signInTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: CharcoalNavy
  },
  emailPhoneView: {
    marginBottom: scaleHeight(16)
  },
  passwordView: {
    marginBottom: scaleHeight(24)
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
    width: scaleWidth(32.19),
    height: scaleHeight(32.19),
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
    color: Black,
  },
  largeRightPadding: {
    paddingRight: scaleWidth(64.19),
  },
  eyeIcons: {
    top: scaleHeight(16),
    right: scaleWidth(20),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(33.807),
    height: scaleHeight(32.192)
  },
  rememberMeRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  rememberMe: {
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  rememberMeBtn: {
    borderWidth: scaleWidth(2),
    borderRadius: scaleWidth(10.731),
    width: 37,
    height: 37,
    borderColor: HappyColor,
    backgroundColor: BlushRose
  },
  rememberMeBtnSelected: {
    borderRadius: scaleWidth(10.731),
    width: 37,
    height: 37,
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  checkIcon: {
    width: scaleWidth(11.401),
    height: scaleHeight(7.592)
  },
  rememberMeTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.36),
    fontWeight: 500,
    color: Black
  },
  forgotPasswordTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.36),
    fontWeight: 600,
    color: HappyColor
  },
  toastContainer: {
    position: 'absolute',
    left: scaleWidth(24),
    right: scaleWidth(24),
    zIndex: 100
  },
  toast: {
    borderRadius: scaleWidth(16),
    paddingHorizontal: scaleWidth(20),
    paddingVertical: scaleHeight(16),
    backgroundColor: HappyColor,
    shadowColor: Black,
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
    elevation: 8
  },
  toastText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600,
    color: White,
    textAlign: 'center'
  },
  login: {
    marginBottom: scaleHeight(12)
  },
  loginBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  loginBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  dontHaveAccount: {
    gap: scaleWidth(7),
    width: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  dontHaveAccountTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  signUp: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: HappyColor
  }
});

export default function Login() {
  const dispatch = useDispatch();
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [selectedSignInType, setSelectedSignInType] = useState('email');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [toastMessage, setToastMessage] = useState(null);
  const toastOpacity = useRef(new Animated.Value(0)).current;
  const toastTranslateY = useRef(new Animated.Value(-20)).current;
  const toastTimerRef = useRef(null);

  const isEmail = (emailValue) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailValue.trim());
  const emailValid = selectedSignInType === 'email' ? isEmail(email) : false;
  const phoneValid = selectedSignInType === 'phone' ? phone.replace(/\D/g, '').length >= 10 : false;
  const passwordValid = password.trim().length > 0;
  const canSignIn = passwordValid && (emailValid || phoneValid);
  useFocusEffect(
    useCallback(() => {
      setEmail('');
      setPhone('');
      setPassword('');
      setToastMessage(null);
      toastOpacity.setValue(0);
      toastTranslateY.setValue(-20);
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    }, [])
  );

  const showToast = (message) => {
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    setToastMessage(message);
    toastOpacity.setValue(0);
    toastTranslateY.setValue(-20);
    Animated.parallel([
      Animated.timing(toastOpacity, { toValue: 1, duration: 250, useNativeDriver: true }),
      Animated.timing(toastTranslateY, { toValue: 0, duration: 250, useNativeDriver: true })
    ]).start();
    toastTimerRef.current = setTimeout(() => {
      Animated.parallel([
        Animated.timing(toastOpacity, { toValue: 0, duration: 200, useNativeDriver: true }),
        Animated.timing(toastTranslateY, { toValue: -20, duration: 200, useNativeDriver: true })
      ]).start(() => setToastMessage(null));
    }, TOAST_DISPLAY_DURATION);
  };

  const dismissToast = () => {
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    Animated.parallel([
      Animated.timing(toastOpacity, { toValue: 0, duration: 200, useNativeDriver: true }),
      Animated.timing(toastTranslateY, { toValue: -20, duration: 200, useNativeDriver: true })
    ]).start(() => setToastMessage(null));
  };

  useEffect(() => {
    return () => {
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    };
  }, []);

  const handleSignIn = async () => {
    if (!canSignIn) return;
    Keyboard.dismiss();
    setToastMessage(null);
    toastOpacity.setValue(0);
    toastTranslateY.setValue(-20);
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    dispatch(showLoading());
    try {
      let response;
      if (selectedSignInType === 'email') {
        response = await authenticationService.signInWithEmail(email.trim(), password);
      } else {
        response = await authenticationService.signInWithPhone(phone.replace(/\D/g, ''), password);
      }
      if (!response.ok) {
        showToast('Unable to sign in. Please check your information and try again.');
        return;
      }
      const responseData = await response.json();
      if (responseData.status === 'pending') {
        navigation.navigate('VerifyCode', { contact: responseData.contact, source: 'signIn' });
      } else if (responseData.status === 'verified') {
        if (rememberMe) {
          await tokenStorage.saveToken(responseData.authToken);
        }
        navigation.reset({ index: 0, routes: [{ name: 'ChatGroups' }] });
      }
    } catch (err) {
      showToast('Something went wrong. Please try again.');
    } finally {
      dispatch(hideLoading());
    }
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
          {selectedSignInType === 'phone' && (
            <View style={styles.emailPhoneView}>
              <CustomText style={styles.textBoxLabel}>Phone Number</CustomText>
              <View>
                <CustomMaskedTextInput
                  style={styles.input}
                  mask="(999) 999-9999"
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
              <CustomTextInput
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
                )}
              </TouchableOpacity>
              <CustomText style={styles.rememberMeTxt}>Remember me</CustomText>
            </View>
            <View>
              <TouchableOpacity onPress={() => navigation.navigate('ForgotPassword')}>
                <CustomText style={styles.forgotPasswordTxt}>Forgot Password?</CustomText>
              </TouchableOpacity>
            </View>
          </View>
        </View>
        <View style={styles.part2}>
          <View style={styles.login}>
            <TouchableOpacity
              style={[styles.loginBtn, !canSignIn && { opacity: 0.5 }]}
              disabled={!canSignIn}
              onPress={handleSignIn}
            >
              <CustomText style={styles.loginBtnText}>Login</CustomText>
            </TouchableOpacity>
          </View>
          <View style={styles.dontHaveAccount}>
            <CustomText style={styles.dontHaveAccountTxt}>Don't have an account?</CustomText>
            <TouchableOpacity onPress={() => navigation.navigate('CreateAccount')}>
              <CustomText style={styles.signUp}>Sign up</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
      {toastMessage && (
        <Animated.View
          style={[
            styles.toastContainer,
            { top: statusBarHeight + scaleHeight(12), opacity: toastOpacity, transform: [{ translateY: toastTranslateY }] }
          ]}
        >
          <TouchableOpacity style={styles.toast} activeOpacity={0.9} onPress={dismissToast}>
            <CustomText style={styles.toastText}>{toastMessage}</CustomText>
          </TouchableOpacity>
        </Animated.View>
      )}
    </View>
  );
}