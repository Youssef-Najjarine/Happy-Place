import React, { useState, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
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
  forgotPassword: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(2),
    fontWeight: 700,
    color: Black,
  },
  forgotPasswordDesc: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginBottom: scaleHeight(24),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
  },
  forgotPasswordType: {
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(1),
    paddingHorizontal: scaleWidth(4),
    marginBottom: scaleHeight(24),
    height: scaleHeight(48),
    width: '100%',
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  forgotPasswordTypeSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  forgotPasswordTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  forgotPasswordTypeNotSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  forgotPasswordTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: '#1D1E25'
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
  forgotPassword: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(4),
    fontWeight: 700,
    color: Black,
  },
  forgotPasswordDesc: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    marginBottom: scaleHeight(32),
    fontWeight: 500,
    color: 'rgba(35, 35, 35, 0.50)',
  },
  forgotPasswordType: {
    borderRadius: scaleWidth(89.959),
    borderWidth: scaleWidth(1.341),
    paddingHorizontal: scaleWidth(6),
    marginBottom: scaleHeight(32),
    height: scaleHeight(64),
    width: '100%',
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  forgotPasswordTypeSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  forgotPasswordTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  forgotPasswordTypeNotSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  forgotPasswordTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: '#1D1E25'
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

export default function EditEmailOrPhone() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [selectedForgotPasswordType, setSelectedForgotPasswordType] = useState('email');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const isEmail = (v) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim());
  const phoneValid = selectedForgotPasswordType === 'phone' ? phone.length >= 10 : false;
  const emailValid = selectedForgotPasswordType === 'email' ? isEmail(email) : false;
  const canConfirm = emailValid || phoneValid;

  const goToVerifyCode = () => {
  if (!canConfirm) return;
  const contact = selectedForgotPasswordType === 'email' ? email.trim() : phone;
  navigation.navigate('VerifyCode', { contact });
  navigation.navigate('VerifyCode', { contact, source: 'forgotPassword' });
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
            <CustomText style={styles.forgotPassword}>EditEmailOrPhone</CustomText>
            <CustomText style={styles.forgotPasswordDesc}>Enter your email or phone number to reset your password.</CustomText>
            <View style={styles.forgotPasswordType}>
              <TouchableOpacity
                  style={selectedForgotPasswordType === 'email' ? styles.forgotPasswordTypeSelectedBtn : styles.forgotPasswordTypeNotSelectedBtn}
                  onPress={() => setSelectedForgotPasswordType('email')}
              >
                  <CustomText style={selectedForgotPasswordType === 'email' ? styles.forgotPasswordTypeSelectedtxt : styles.forgotPasswordTypeNotSelectedTxt}>Email Address</CustomText>
              </TouchableOpacity>
              <TouchableOpacity
                  style={selectedForgotPasswordType === 'phone' ? styles.forgotPasswordTypeSelectedBtn : styles.forgotPasswordTypeNotSelectedBtn}
                  onPress={() => setSelectedForgotPasswordType('phone')}
              >
                  <CustomText style={selectedForgotPasswordType === 'phone' ? styles.forgotPasswordTypeSelectedtxt : styles.forgotPasswordTypeNotSelectedTxt}>Phone Number</CustomText>
              </TouchableOpacity>
            </View>
            {selectedForgotPasswordType === 'email' && (
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
 {selectedForgotPasswordType === 'phone' && (
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
                  style={[styles.confirmBtn, !canConfirm && { opacity: 0.5 }]}
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