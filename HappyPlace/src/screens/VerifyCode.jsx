import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Animated, Keyboard } from 'react-native';
import { useNavigation, useFocusEffect, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  Charcoal, 
  IndigoDye, 
  FrostedWhite,
  Rosewater,
  Graphite
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice'; 
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import authenticationService from 'services/authenticationService';

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
  part1: {
    gap: scaleHeight(24)
  },
  part2: {
    height: scaleHeight(55)
  },
  headers: {
    gap: scaleHeight(2)
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
  verifyCode: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    fontWeight: 700,
    color: Black,
  },
  verifyCodeDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 500,
    color: Charcoal,
  },
  contactType: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 500,
    color: Black
  },
  verifyCodeInputs: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  selectedInput: {
    width: scaleWidth(48),
    height: scaleHeight(48),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(12),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    borderColor: HappyColor,
    backgroundColor: Rosewater,
    color: Graphite,
    textAlign: 'center'
  },
  input: {
    width: scaleWidth(48),
    height: scaleHeight(48),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(12),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Graphite,
    textAlign: 'center',
    textAlignVertical: 'center'
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
  resendCode: {
    gap: scaleWidth(5),
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center'
  },
  resendCodeTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: Charcoal
  },
  resendCodeValue: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600
  },
  blackColor: {
    color: Black
  },
  happyColor: {
    color: HappyColor
  },
  confirmBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  confirmBtnText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
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
  part1: {
    gap: scaleHeight(32)
  },
  part2: {
    height: scaleHeight(71.192)
  },
  headers: {
    gap: scaleHeight(4)
  },
  BackArrow: {
    borderRadius: scaleWidth(132.792),
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
  verifyCode: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    fontWeight: 700,
    color: Black,
  },
  verifyCodeDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 500,
    color: Charcoal,
  },
  contactType: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 500,
    color: Black
  },
  verifyCodeInputs: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  selectedInput: {
    width: 89.31,
    height: 89.31,
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingHorizontal: scaleWidth(21.46),
    paddingVertical: scaleHeight(16.1),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    borderColor: HappyColor,
    backgroundColor: Rosewater,
    color: Graphite,
    textAlign: 'center'
  },
  input: {
    width: 89.31,
    height: 89.31,
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingHorizontal: scaleWidth(21.46),
    paddingVertical: scaleHeight(16.1),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Graphite,
    textAlign: 'center'
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
  resendCode: {
    gap: scaleWidth(7),
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center'
  },
  resendCodeTxt: {
    fontSize: scaleFont(21.461),
    lineHeight: scaleLineHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215),
    fontWeight: 500,
    color: Charcoal
  },
  resendCodeValue: {
    fontSize: scaleFont(21.461),
    lineHeight: scaleLineHeight(32.192),
    letterSpacing: scaleLetterSpacing(-0.215),
    fontWeight: 600
  },
  blackColor: {
    color: Black
  },
  happyColor: {
    color: HappyColor
  },
  confirmBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  confirmBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  }
});

export default function VerifyCode() {
  const dispatch = useDispatch();
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();

  const contact = route.params?.contact || '';
  const source  = route.params?.source  || 'createAccount';

  const CODE_LENGTH = 6;
  const [code, setCode] = useState(Array(CODE_LENGTH).fill(''));
  const [toastMessage, setToastMessage] = useState(null);
  const inputsRef = useRef(Array.from({ length: CODE_LENGTH }, () => React.createRef()));
  const canConfirm = code.every((c) => c !== '');
  const codeValue = code.join('');
  const toastOpacity = useRef(new Animated.Value(0)).current;
  const toastTranslateY = useRef(new Animated.Value(-20)).current;
  const toastTimerRef = useRef(null);

  const INITIAL_SECONDS = 60;
  const [secondsLeft, setSecondsLeft] = useState(INITIAL_SECONDS);
  const [isCounting, setIsCounting] = useState(true);

  const isEmailContact = contact.includes('@');
  const maskEmail = (email) => {
    const [localPart, domain] = email.split('@');
    if (!domain) return email;
    if (localPart.length <= 1) return `${localPart}**@${domain}`;
    const visibleStart = localPart.slice(0, 2);
    const maskedRemainder = '*'.repeat(Math.max(localPart.length - 2, 2));
    return `${visibleStart}${maskedRemainder}@${domain}`;
  };
  const maskPhone = (phoneNumber) => {
    if (phoneNumber.length <= 4) return phoneNumber;
    const visibleStart = phoneNumber.slice(0, 3);
    const visibleEnd = phoneNumber.slice(-2);
    const maskedMiddle = '*'.repeat(Math.max(phoneNumber.length - 5, 2));
    return `${visibleStart}${maskedMiddle}${visibleEnd}`;
  };

  const maskedContact = isEmailContact ? maskEmail(contact) : maskPhone(contact);
  const descriptionText = isEmailContact
    ? 'Please enter the code we just sent to your email.'
    : 'Please enter the code we just sent to your phone number.';

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

  const focusInput = (i) => inputsRef.current[i]?.current?.focus?.();

  const setDigit = (i, val) => {
    const digits = String(val).replace(/\D/g, '');
    if (!digits) return;
    if (toastMessage) dismissToast();

    setCode((prev) => {
      const next = [...prev];
      let idx = i;
      for (let d of digits) {
        if (idx >= CODE_LENGTH) break;
        next[idx] = d;
        idx += 1;
      }
      const firstEmpty = next.findIndex((c) => c === '');
      if (firstEmpty === -1) {
        focusInput(CODE_LENGTH - 1);
      } else {
        focusInput(Math.max(i + digits.length, i + 1, firstEmpty));
      }
      return next;
    });
  };

  const handleKeyPress = (i, e) => {
    if (e.nativeEvent.key === 'Backspace') {
      if (toastMessage) dismissToast();
      setCode((prev) => {
        const next = [...prev];
        if (next[i]) {
          next[i] = '';
          return next;
        }
        for (let j = i - 1; j >= 0; j--) {
          if (next[j]) {
            next[j] = '';
            focusInput(j);
            break;
          } else if (j === 0) {
            focusInput(0);
          }
        }
        return next;
      });
    }
  };

  const onFocus = (i) => {
    const firstEmpty = code.findIndex((c) => c === '');
    if (firstEmpty !== -1 && firstEmpty < i) focusInput(firstEmpty);
  };

  useEffect(() => {
    if (!isCounting) return;
    const id = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          clearInterval(id);
          setIsCounting(false);
          return 0;
        }
        return s - 1;
      });
    }, 1000);
    return () => clearInterval(id);
  }, [isCounting]);

  const timeLeft = useMemo(() => {
    const minutes = Math.floor(secondsLeft / 60);
    const seconds = secondsLeft % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  }, [secondsLeft]);

  const handleResend = () => {
    if (toastMessage) dismissToast();
    setCode(Array(CODE_LENGTH).fill(''));
    Keyboard.dismiss();
    dispatch(showLoading());
    setTimeout(async () => {
      try {
        if (source === 'forgotPassword') {
          if (isEmailContact) {
            await authenticationService.forgotPasswordWithEmail(contact);
          } else {
            await authenticationService.forgotPasswordWithPhone(contact);
          }
        } else {
          if (isEmailContact) {
            await authenticationService.resendEmailCode(contact);
          } else {
            await authenticationService.resendPhoneCode(contact);
          }
        }
        setSecondsLeft(INITIAL_SECONDS);
        setIsCounting(true);
      } catch (err) {
        showToast('Unable to resend code. Please try again.');
      } finally {
        dispatch(hideLoading());
      }
    }, 100);
  };

  const verifyCode = () => {
    if (!canConfirm) return;
    Keyboard.dismiss();
    if (toastMessage) dismissToast();
    dispatch(showLoading());
    setTimeout(async () => {
      try {
        let response;
        if (source === 'forgotPassword') {
          if (isEmailContact) {
            response = await authenticationService.verifyForgotPasswordEmail(contact, codeValue);
          } else {
            response = await authenticationService.verifyForgotPasswordPhone(contact, codeValue);
          }
        } else {
          if (isEmailContact) {
            response = await authenticationService.verifyEmail(contact, codeValue);
          } else {
            response = await authenticationService.verifyPhone(contact, codeValue);
          }
        }
        if (!response.ok) {
          showToast('The code entered is incorrect or has expired. Please try again.');
          return;
        }
        const responseData = await response.json();
        if (source === 'forgotPassword') {
          navigation.replace('SetupPassword', { contact, resetToken: responseData.resetToken });
        } else {
          navigation.replace('AccountVerified', { contact, source, authToken: responseData.authToken });
        }
      } catch (err) {
        showToast('Something went wrong. Please try again.');
      } finally {
        dispatch(hideLoading());
      }
    }, 100);
  };

  useFocusEffect(
    useCallback(() => {
      setSecondsLeft(INITIAL_SECONDS);
      setIsCounting(true);
      setToastMessage(null);
      toastOpacity.setValue(0);
      toastTranslateY.setValue(-20);
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
      setCode(Array(CODE_LENGTH).fill(''));
    }, [])
  );

  const rootStyle = { ...styles.root, paddingTop: statusBarHeight };
  const cardStyle = { ...styles.card, paddingBottom: bottomSafeHeight };

  return (
    <View style={rootStyle}>
      <View style={cardStyle}>
        <View style={styles.part1}>
          <TouchableOpacity style={styles.BackArrow} onPress={() => navigation.goBack()}>
            <BackArrow {...styles.backArrowIcon}/>
          </TouchableOpacity>

          <View style={styles.headers}>
            <CustomText style={styles.verifyCode}>Verify Code</CustomText>
            <CustomText style={styles.verifyCodeDesc}>{descriptionText}</CustomText>
            <CustomText style={styles.contactType}>{maskedContact}</CustomText>
          </View>

          <View style={styles.verifyCodeInputs}>
            {Array.from({ length: CODE_LENGTH }).map((_, i) => {
              const isFilled   = code[i] !== '';
              const isSelected =
                !isFilled &&
                (i === code.findIndex((c) => c === '') ||
                  (i === CODE_LENGTH - 1 && code.every((c) => c !== '')));

              return (
                <CustomTextInput
                  key={i}
                  ref={inputsRef.current[i]}
                  style={isSelected ? styles.selectedInput : styles.input}
                  value={code[i]}
                  keyboardType="number-pad"
                  maxLength={1}
                  textContentType="oneTimeCode"
                  autoComplete="one-time-code"
                  onFocus={() => onFocus(i)}
                  onChangeText={(txt) => setDigit(i, txt)}
                  onKeyPress={(e) => handleKeyPress(i, e)}
                  accessibilityLabel={`Verification digit ${i + 1}`}
                  returnKeyType="none"
                />
              );
            })}
          </View>

          {isCounting ? (
            <View style={styles.resendCode}>
              <CustomText style={styles.resendCodeTxt}>Resend Code in</CustomText>
              <CustomText style={[styles.resendCodeValue, styles.blackColor]}>
                {timeLeft}
              </CustomText>
            </View>
          ) : (
            <View style={styles.resendCode}>
              <CustomText style={styles.resendCodeTxt}>Not receive code yet?</CustomText>
              <TouchableOpacity onPress={handleResend}>
                <CustomText style={[styles.resendCodeValue, styles.happyColor]}>
                  Resend Code
                </CustomText>
              </TouchableOpacity>
            </View>
          )}
        </View>

        <View style={styles.part2}>
          <View>
            <TouchableOpacity
              style={[styles.confirmBtn, { opacity: canConfirm ? 1 : 0.5 }]}
              onPress={verifyCode}
              disabled={!canConfirm}
            >
              <CustomText style={styles.confirmBtnText}>Confirm</CustomText>
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