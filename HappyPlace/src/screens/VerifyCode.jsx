import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation, useFocusEffect, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';

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
    shadowColor: '#094173',
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
    backgroundColor: '#F9F9F9'
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
    color: 'rgba(35, 35, 35, 0.50)',
  },
  contactType: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 500,
    volor: Black
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
    backgroundColor: 'rgba(237, 83, 112, 0.10)',
    color: 'rgba(35, 35, 35, 0.80)',
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: 'rgba(35, 35, 35, 0.80)',
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
    color: 'rgba(35, 35, 35, 0.50)'
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
    shadowColor: '#094173',
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
    backgroundColor: '#F9F9F9'
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
    color: 'rgba(35, 35, 35, 0.50)',
  },
  contactType: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 500,
    volor: Black
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
    backgroundColor: 'rgba(237, 83, 112, 0.10)',
    color: 'rgba(35, 35, 35, 0.80)',
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: 'rgba(35, 35, 35, 0.80)',
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
    color: 'rgba(35, 35, 35, 0.50)'
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
    const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const navigation = useNavigation();
    const route = useRoute();
    const contact = route.params?.contact || '';
    const CODE_LENGTH = 6;
    const [code, setCode] = useState(Array(CODE_LENGTH).fill(''));
    const inputsRef = useRef(Array.from({ length: CODE_LENGTH }, () => React.createRef()));
    const canConfirm = code.every((c) => c !== '');
    const codeValue  = code.join('');
    const INITIAL_SECONDS = 60;
    const [secondsLeft, setSecondsLeft] = useState(INITIAL_SECONDS);
    const [isCounting, setIsCounting] = useState(true);
    let maskedContact = '';
    let descriptionText = '';
    const maskEmail = (email) => {
    const [name, domain] = email.split('@');
    if (name.length <= 4) return email;
    return `${name.slice(0, 4)}${'*'.repeat(name.length - 4)}@${domain}`;
    };
    const maskPhone = (phone) => {
    if (phone.length <= 4) return phone;
    const visibleStart = phone.slice(0, 5);
    const visibleEnd = phone.slice(-3);
    const maskedMiddle = '*'.repeat(phone.length - 8);
    return `${visibleStart}${maskedMiddle}${visibleEnd}`;
    };

    if (contact.includes('@')) {
        maskedContact = maskEmail(contact);
        descriptionText = 'Please enter the code we just sent to your email.';
    } else {
        maskedContact = maskPhone(contact);
        descriptionText = 'Please enter the code we just sent to your phone number.';
    }

    const focusInput = (i) => {
    inputsRef.current[i]?.current?.focus?.();
    };

    const setDigit = (i, val) => {
    const digits = String(val).replace(/\D/g, '');
    if (!digits) return;

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
    if (firstEmpty !== -1 && firstEmpty < i) {
        focusInput(firstEmpty);
    }
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
        const m = Math.floor(secondsLeft / 60);
        const s = secondsLeft % 60;
        return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }, [secondsLeft]);

    const handleResend = () => {
        setSecondsLeft(INITIAL_SECONDS);
        setIsCounting(true);
    };
    const verifyCode = () => {
        navigation.navigate('AccountVerified');
    };
    useFocusEffect(
        useCallback(() => {
            setSecondsLeft(INITIAL_SECONDS);
            setIsCounting(true);
        }, [])
    );
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
            <View style={styles.headers}>
                <CustomText style={styles.verifyCode}>Verify Code</CustomText>
                <CustomText style={styles.verifyCodeDesc}>{descriptionText}</CustomText>
                <CustomText style={styles.contactType}>{maskedContact}</CustomText>
            </View>
            <View style={styles.verifyCodeInputs}>
                {Array.from({ length: CODE_LENGTH }).map((_, i) => {
                    const isFilled = code[i] !== '';
                    const isSelected =
                    !isFilled && (i === code.findIndex((c) => c === '') || (i === CODE_LENGTH - 1 && code.every((c) => c !== '')));

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
                    style={[
                        styles.confirmBtn,
                        { opacity: canConfirm ? 1 : 0.5 }
                    ]}
                    onPress={() => {
                        if (canConfirm) verifyCode()
                        
                    }}
                    disabled={!canConfirm} 
                >
                    <CustomText style={styles.confirmBtnText}>Confirm</CustomText>
                </TouchableOpacity>
            </View>
        </View>
        </View>
    </View>
    );
}