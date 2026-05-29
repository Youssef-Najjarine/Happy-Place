import React, { useState, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Keyboard } from 'react-native';
import { useNavigation, useRoute, useFocusEffect } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black, VeryLightGray, Charcoal, IndigoDye, FrostedWhite } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import { showToast } from 'src/components/Toast';
import tokenStorage from 'services/tokenStorage';
import profileService from 'services/profileService';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import CustomMaskedTextInput from 'src/components/FontFamilyMaskedTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EmailIcon from 'assets/images/global/email-outline-icon.svg';
import PhoneIcon from 'assets/images/global/phone-icon.svg';

const formatPhoneNumber = (number) => {
  if (!number) return '';
  const cleaned = ('' + number).replace(/\D/g, '');
  if (cleaned.length === 10) {
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
  }
  return number;
};

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
    height: scaleHeight(55)
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
  addType: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black,
  },
  addTypeDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginBottom: scaleHeight(16),
    fontWeight: 500,
    color: Charcoal,
  },
  currentValueView: {
    marginBottom: scaleHeight(20)
  },
  currentValueLabel: {
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    marginBottom: scaleHeight(2),
    fontWeight: 500,
    color: Charcoal
  },
  currentValueText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
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
  passwordView: {
    marginTop: scaleHeight(16)
  },
  passwordInput: {
    height: scaleHeight(48),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    paddingHorizontal: scaleWidth(20),
    paddingVertical: scaleHeight(12),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  confirmBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
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
  part2: {
    height: scaleHeight(71.192)
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
  addType: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(4),
    fontWeight: 700,
    color: Black,
  },
  addTypeDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    marginBottom: scaleHeight(20),
    fontWeight: 500,
    color: Charcoal,
  },
  currentValueView: {
    marginBottom: scaleHeight(28)
  },
  currentValueLabel: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    marginBottom: scaleHeight(4),
    fontWeight: 500,
    color: Charcoal
  },
  currentValueText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: Black
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
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  passwordView: {
    marginTop: scaleHeight(20)
  },
  passwordInput: {
    height: scaleHeight(64.192),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingHorizontal: scaleWidth(28),
    paddingVertical: scaleHeight(16),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  confirmBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
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

export default function EditEmailOrPhone() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const dispatch = useDispatch();
  const source = (route.params?.source === 'phone' || route.params?.source === 'email') ? route.params.source : 'email';
  const currentValue = route.params?.currentValue || '';
  const isPhone = source === 'phone';
  const isEmail = source === 'email';

  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [currentPassword, setCurrentPassword] = useState('');

  useFocusEffect(
    useCallback(() => {
      setEmail('');
      setPhone('');
      setCurrentPassword('');
    }, [])
  );

  const emailTypeHeader = 'Email Address';
  const phoneTypeHeader = 'Phone Number';
  const emailTypeDesc = 'Enter the new email address you\'d like to use and your current password. We\'ll send a verification code to confirm the change.';
  const phoneTypeDesc = 'Enter the new phone number you\'d like to use and your current password. We\'ll send a verification code to confirm the change.';

  const validateEmail = (val) =>
    /^\s*[^@\s]+@[^@\s]+\.[^@\s]+\s*$/.test(val);
  const validatePhone = (val) =>
    (val || '').replace(/\D/g, '').length === 10;

  const canConfirm = useMemo(() => {
    const contactValid = isEmail ? validateEmail(email) : validatePhone(phone);
    const passwordValid = currentPassword.length > 0;
    return contactValid && passwordValid;
  }, [isEmail, email, phone, currentPassword]);

  const rootStyle = { ...styles.root, paddingTop: statusBarHeight };
  const cardStyle = { ...styles.card, paddingBottom: bottomSafeHeight };

  const submitRequest = () => {
    if (!canConfirm) return;
    Keyboard.dismiss();
    dispatch(showLoading());
    setTimeout(async () => {
      try {
        const token = await tokenStorage.getToken();
        if (!token) {
          showToast('Session expired. Please log in again.', 'error');
          return;
        }
        const contact = isEmail ? email.trim() : phone;
        let response;
        if (isEmail) {
          response = await profileService.requestEmailChange(token, currentPassword, contact);
        } else {
          response = await profileService.requestPhoneChange(token, currentPassword, contact);
        }
        
        if (!response.ok) {
          try {
            const data = await response.json();
            const errors = Array.isArray(data) ? data : data?.errorMessages;
            if (errors?.length > 0) {
              showToast(errors[0], 'error');
              return;
            }
          } catch {}
          showToast('Unable to send verification code. Please try again.', 'error');
          return;
        }
        const verifyCodeSource = isEmail ? 'changeEmail' : 'changePhone';
        navigation.navigate('VerifyCode', {
          contact,
          source: verifyCodeSource,
          currentPassword
        });
      } catch {
        showToast('Network error. Please check your connection.', 'error');
      } finally {
        dispatch(hideLoading());
      }
    }, 100);
  };

  const formattedCurrentValue = isEmail ? currentValue : formatPhoneNumber(currentValue);

  return (
    <View style={rootStyle}>
      <View style={cardStyle}>
        <View style={styles.part1}>
          <TouchableOpacity style={styles.BackArrow} onPress={() => navigation.goBack()}>
            <BackArrow {...styles.backArrowIcon}/>
          </TouchableOpacity>

          <CustomText style={styles.addType}>Edit {isEmail ? emailTypeHeader : phoneTypeHeader}</CustomText>
          <CustomText style={styles.addTypeDesc}>{isEmail ? emailTypeDesc : phoneTypeDesc}</CustomText>

          {!!currentValue && (
            <View style={styles.currentValueView}>
              <CustomText style={styles.currentValueLabel}>
                Current {isEmail ? 'Email' : 'Phone Number'}
              </CustomText>
              <CustomText style={styles.currentValueText} numberOfLines={1} ellipsizeMode="tail">
                {formattedCurrentValue}
              </CustomText>
            </View>
          )}

          {isEmail && (
            <View style={styles.emailPhoneView}>
              <CustomText style={styles.textBoxLabel}>New Email</CustomText>
              <View>
                <CustomTextInput
                  style={styles.input}
                  keyboardType="email-address"
                  autoCapitalize="none"
                  autoCorrect={false}
                  value={email}
                  onChangeText={setEmail}
                />
                <EmailIcon {...styles.textBoxIcon}/>
              </View>
            </View>
          )}

          {isPhone && (
            <View style={styles.emailPhoneView}>
              <CustomText style={styles.textBoxLabel}>New Phone Number</CustomText>
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
            <CustomText style={styles.textBoxLabel}>Current Password</CustomText>
            <View>
              <CustomTextInput
                style={styles.passwordInput}
                value={currentPassword}
                onChangeText={setCurrentPassword}
                secureTextEntry
                autoCapitalize="none"
                autoCorrect={false}
                textContentType="password"
              />
            </View>
          </View>
        </View>

        <View style={styles.part2}>
          <View style={styles.confirm}>
            <TouchableOpacity
              style={[
                styles.confirmBtn,
                { backgroundColor: canConfirm ? HappyColor : 'rgba(237,83,112,0.4)' }
              ]}
              disabled={!canConfirm}
              onPress={submitRequest}
            >
              <CustomText style={styles.confirmBtnText}>Confirm</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </View>
  );
}