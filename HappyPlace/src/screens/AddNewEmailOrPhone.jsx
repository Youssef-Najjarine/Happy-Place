import React, { useState, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import CustomMaskedTextInput from 'src/components/FontFamilyMaskedTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EmailIcon from 'assets/images/global/email-outline-icon.svg';
import PhoneIcon from 'assets/images/global/phone-icon.svg';

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
    backgroundColor: '#F9F9F9'
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
    marginBottom: scaleHeight(24),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
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
    shadowColor: '#094173',
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
    backgroundColor: '#F9F9F9'
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
    marginBottom: scaleHeight(32),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
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

export default function AddNewEmailOrPhone() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const source = (route.params?.source === 'phone' || route.params?.source === 'email') ? route.params.source : 'email';
  const isPhone = source === 'phone';
  const isEmail = source === 'email';

  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');

  const emailTypeHeader = 'Email Address';
  const phoneTypeHeader = 'Phone Number';
  const emailTypeDesc = 'Please enter your email address so we can verify your profile with this method.';
  const phoneTypeDesc = 'Please enter your phone number so we can verify your profile with this method.';

  const validateEmail = (val) =>
    /^\s*[^@\s]+@[^@\s]+\.[^@\s]+\s*$/.test(val);
  const validatePhone = (val) =>
    (val || '').replace(/\D/g, '').length === 10;

  const canConfirm = useMemo(() => {
    return isEmail ? validateEmail(email) : validatePhone(phone);
  }, [isEmail, email, phone]);

  const rootStyle = { ...styles.root, paddingTop: statusBarHeight };
  const cardStyle = { ...styles.card, paddingBottom: bottomSafeHeight };

  const goToVerifyCode = () => {
    if (!canConfirm) return;
    const contact = isEmail ? email.trim() : phone;
    navigation.navigate('VerifyCode', { contact, type: source });
  };

  return (
    <View style={rootStyle}>
      <View style={cardStyle}>
        <View style={styles.part1}>
          <TouchableOpacity style={styles.BackArrow} onPress={() => navigation.goBack()}>
            <BackArrow {...styles.backArrowIcon}/>
          </TouchableOpacity>

          <CustomText style={styles.addType}>Add {isEmail ? emailTypeHeader : phoneTypeHeader}</CustomText>
          <CustomText style={styles.addTypeDesc}>{isEmail ? emailTypeDesc : phoneTypeDesc}</CustomText>

          {isEmail && (
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

          {isPhone && (
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
        </View>

        <View style={styles.part2}>
          <View style={styles.confirm}>
            <TouchableOpacity
              style={[
                styles.confirmBtn,
                { backgroundColor: canConfirm ? HappyColor : 'rgba(237,83,112,0.4)' }
              ]}
              disabled={!canConfirm}
              onPress={goToVerifyCode}
            >
              <CustomText style={styles.confirmBtnText}>Confirm</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </View>
  );
}
