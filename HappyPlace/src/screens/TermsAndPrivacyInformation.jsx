import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black,
  VeryLightGray, 
  IndigoDye, 
  FrostedWhite,
  CharcoalNavy
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
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
    shadowColor: IndigoDye,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    justifyContent: 'space-between'
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
  messageType: {
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
  messageTypeSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  messageTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: White
  },
  messageTypeNotSelectedBtn: {
    width: scaleWidth(159.5),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  messageTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: CharcoalNavy
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
  messageType: {
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
  messageTypeSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  messageTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: White
  },
  messageTypeNotSelectedBtn: {
    width: scaleWidth(336),
    height: scaleHeight(52),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  messageTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: CharcoalNavy
  }
});

export default function TermsAndPrivacyInformation() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [selectedMessageType, setSelectedMessageType] = useState('termsOfService');

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
            <View style={styles.messageType}>
              <TouchableOpacity
                  style={selectedMessageType === 'termsOfService' ? styles.messageTypeSelectedBtn : styles.messageTypeNotSelectedBtn}
                  onPress={() => setSelectedMessageType('termsOfService')}
              >
                  <CustomText style={selectedMessageType === 'termsOfService' ? styles.messageTypeSelectedtxt : styles.messageTypeNotSelectedTxt}>Terms of Service</CustomText>
              </TouchableOpacity>
              <TouchableOpacity
                  style={selectedMessageType === 'privacyPolicy' ? styles.messageTypeSelectedBtn : styles.messageTypeNotSelectedBtn}
                  onPress={() => setSelectedMessageType('privacyPolicy')}
              >
                  <CustomText style={selectedMessageType === 'privacyPolicy' ? styles.messageTypeSelectedtxt : styles.messageTypeNotSelectedTxt}>Privacy Policy</CustomText>
              </TouchableOpacity>
            </View>
            <View style={styles.messageView}>
                {selectedMessageType === 'termsOfService' && (
                    <View style={styles.messageContainer}>
                        <CustomText style={styles.messageHeader}>termsOfService</CustomText>
                        <View style={styles.messageSubContainer}>

                        </View>
                    </View>
                )}
                {selectedMessageType === 'privacyPolicy' && (
                    <View style={styles.messageContainer}>
                        <CustomText style={styles.messageHeader}>privacyPolicy</CustomText>
                        <View style={styles.messageSubContainer}>
                            <CustomText>
                            Happy Place (“we,” “our,” or “us”) values 
                            your privacy and is committed to protecting 
                            your personal information. This Privacy Policy 
                            explains how we collect, use, and 
                            safeguard your data when you use our app.

                            1. Information We Collect
                            When you use Happy Place, we may collect:
                            Account Information – such as your name,
                            email, username, and profile details.
                            Chat Content – messages and interactions you share with others.
                            Usage Data – information about how you use the app (features used, time spent, etc.).
                            Device Information – such as device type, operating system, and app version.
                            We do not collect sensitive personal details 
                            unless you choose to share them voluntarily in chats.
                            
                            2. How We Use Your Information
                            We use the collected information to:
                            Provide and improve the Happy Place experience.
                            Enable safe and meaningful chat interactions.
                            Personalize your experience.
                            Monitor and prevent misuse or harmful behavior.
                            Comply with legal obligations.

                            3. Sharing of Information
                            We do not sell or rent your personal information. Your data may be shared only in the 
                            following cases:
                            With service providers who help us operate the app (e.g., hosting, analytics).
                            For safety reasons – if required to prevent harm, abuse, or illegal activity.
                            By law – if disclosure is required by legal authorities.

                            4. Data Security
                            We take reasonable steps to protect your information through encryption, secure servers, 
                            and monitoring systems. However, no method of transmission or storage is 100% secure, and 
                            we cannot guarantee absolute protection.
                            </CustomText>
                        </View>                        
                    </View>
                )}
            </View>
        </View>
      </View>
    </View>
  );
}