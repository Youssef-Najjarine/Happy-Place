import React, { useEffect, useMemo, useState } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black,
  Charcoal,
  VeryLightGray, 
  IndigoDye, 
  FrostedWhite,
  CharcoalNavy,
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
  cardContainer: {
    gap: scaleHeight(16),
    flex: 1
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
    height: scaleHeight(28)
  },
  messageType: {
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
  },
  messageContainer: {
    gap: scaleHeight(4)
  },
  messageHeader: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    fontWeight: 700,
    color: Black
  },
  messageSubContainer: {
    gap: scaleHeight(16)
  },
  messageSubHeader: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600,
    color: Black   
  },  
  messageGreyText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 500,
    flex: 1,
    color: Charcoal    
  },
  privacyMessageBlackText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600,
    flex: 1,
    color: Black
  },
  bulletPointRow: {
    paddingLeft: scaleWidth(15),
    flexDirection: 'row'
  },
  bulletPoint: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    marginRight: scaleWidth(8),
    fontWeight: 500,
    color: Charcoal  
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
  cardContainer: {
    gap: scaleHeight(24),
    flex: 1
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
  messageType: {
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
  },
  messageContainer: {
    gap: scaleHeight(6)
  },
  messageHeader: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    fontWeight: 700,
    color: Black
  },
  messageSubContainer: {
    gap: scaleHeight(24)
  },
  messageSubHeader: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(27),
    fontWeight: 600,
    color: Black   
  },  
  messageGreyText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 500,
    flex: 1,
    color: Charcoal    
  },
  privacyMessageBlackText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 600,
    flex: 1,
    color: Black
  },
  bulletPointRow: {
    paddingLeft: scaleWidth(20),
    flexDirection: 'row'
  },
  bulletPoint: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    marginRight: scaleWidth(10),
    fontWeight: 500,
    color: Charcoal  
  }

});

export default function TermsAndPrivacyInformation() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  // const [selectedMessageType, setSelectedMessageType] = useState('termsOfService');
  const route = useRoute();
  const initialTab = useMemo(() => {
    const v = route?.params?.initialTab;
    return v === 'privacyPolicy' ? 'privacyPolicy' : 'termsOfService';
  }, [route?.params?.initialTab]);

  const [selectedMessageType, setSelectedMessageType] = useState(initialTab);

  useEffect(() => {
    setSelectedMessageType(initialTab);
  }, [initialTab]);

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight
  };
  const cardStyle = {
    ...styles.card
  };
  const contentContainer = {
    paddingBottom: bottomSafeHeight
  };

  const purposeOfHappyPlaceBullets = [
    'Share positivity and happiness with others.',
    'Ask others to make them happy through supportive chats and uplifting messages.',
    'Connect in a safe and respectful environment.',
  ];
  const userResponsibilitiesBullets = [
    'Use Happy Place respectfully and kindly.',
    'Not harass, bully, or harm other users.',
    'Avoid sharing harmful, offensive, or illegal content.',
    'Respect the privacy of others and not share personal information without consent.'
  ];  
  const privacyAndSafetyBullets = [
    'Conversations may be monitored or reviewed to maintain a safe environment.',
    'Your personal data will be handled according to our [Privacy Policy].'
  ];
  const limitationOfLiabilityBullets = [
    'We are not responsible for the actions, messages, or behavior of users.',
    'We do not guarantee that every interaction will make you happy.'
  ];
  const informationWeCollectBullets = [
    'Account Information – such as your name, email, username, and profile details.',
    'Chat Content – messages and interactions you share with others.',
    'Usage Data – information about how you use the app (features used, time spent, etc.).',
    'Device Information – such as device type, operating system, and app version.'
  ];
  const howWeUseYourInformationBullets = [
    'Provide and improve the Happy Place experience.',
    'Enable safe and meaningful chat interactions.',
    'Personalize your experience.',
    'Monitor and prevent misuse or harmful behavior.',
    'Comply with legal obligations.'
  ];
  const sharingOfInformationBullets = [
    'With service providers who help us operate the app (e.g., hosting, analytics).',
    'For safety reasons – if required to prevent harm, abuse, or illegal activity.',
    'By law – if disclosure is required by legal authorities.'
  ];
  return (
    <View style={rootStyle}>
      <View style={cardStyle}>
        <View style={styles.cardContainer}>
          <View>
            <TouchableOpacity 
              style={styles.BackArrow}
              onPress={() => navigation.goBack()}
            >
              <BackArrow {...styles.backArrowIcon}/>
            </TouchableOpacity>
          </View>
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
          <ScrollView 
            style={{ flex: 1 }}
            contentContainerStyle={contentContainer}
            showsVerticalScrollIndicator={false}
          >
            {selectedMessageType === 'termsOfService' && (
              <View style={styles.messageContainer}>
                <View>
                  <CustomText style={styles.messageHeader}>terms of Service</CustomText>
                </View>
                <View style={styles.messageSubContainer}>
                  <View>
                    <CustomText style={styles.messageGreyText}>
                      Welcome to Happy Place – a platform designed to spread positivity, joy, and 
                      meaningful connections. By using our app, you agree to the following Terms of Service. 
                      Please read them carefully before accessing or using Happy Place.                     
                    </CustomText>                        
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      1. Acceptance of Terms
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      By creating an account, accessing, or using Happy Place, you confirm that you have 
                      read, understood, and agree to be bound by these Terms. If you do not agree, please 
                      do not use the app.                              
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      2. Purpose of Happy Place
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      Happy Place is a community-driven app where users can:
                    </CustomText>
                    {purposeOfHappyPlaceBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}
                    <View>
                      <CustomText style={styles.messageGreyText}>
                        The app is not a replacement for professional counseling, therapy, or medical advice.
                      </CustomText>
                    </View>                                          
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      3. User Responsibilities
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      You agree to:
                    </CustomText>
                    {userResponsibilitiesBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}
                    <CustomText style={styles.messageGreyText}>
                      Failure to follow these rules may result in suspension or permanent removal from the 
                      platform.                
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      4. Privacy and Safety
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We care about your safety and privacy. By using Happy Place, you acknowledge that:
                    </CustomText>
                    {privacyAndSafetyBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}
                    <CustomText style={styles.messageGreyText}>
                      You are responsible for the information you choose to share in chats.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      5. Eligibility
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      You must be at least 13 years old (or the minimum age in your country) to use Happy Place. 
                      If you are under 18, you must have parental or guardian consent.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      6. Limitation of Liability
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      Happy Place is designed for positivity and connection. However:
                    </CustomText>
                    {limitationOfLiabilityBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}
                    <CustomText style={styles.messageGreyText}>
                      The app is provided “as is,” without warranties of any kind.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      7. Termination
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We reserve the right to suspend or terminate accounts at our discretion if a user 
                      violates these Terms or engages in harmful behavior.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      8. Changes to Terms
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We may update these Terms from time to time. Changes will be effective once posted, and 
                      continued use of the app means you accept the updated Terms.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      9. Contact Us
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      If you have questions about these Terms, please contact us at: youssef@happy.place
                    </CustomText>
                  </View>
                </View>
              </View>
            )}
            {selectedMessageType === 'privacyPolicy' && (
              <View style={styles.messageContainer}>
                <CustomText style={styles.messageHeader}>Privacy Policy</CustomText>
                <View style={styles.messageSubContainer}>
                  <View>
                    <CustomText style={styles.messageGreyText}>
                      Happy Place (“we,” “our,” or “us”) values 
                      your privacy and is committed to protecting 
                      your personal information. This Privacy Policy 
                      explains how we collect, use, and 
                      safeguard your data when you use our app.
                    </CustomText>
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      1. Information We Collect
                    </CustomText>
                    <CustomText style={styles.privacyMessageBlackText}>
                      When you use Happy Place, we may collect:
                    </CustomText>
                    {informationWeCollectBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}
                    <View>
                      <CustomText style={styles.messageGreyText}>
                        We do not collect sensitive personal details unless you choose to share them voluntarily in chats.
                      </CustomText>
                    </View>                                          
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      2. How We Use Your Information
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We use the collected information to:
                    </CustomText>
                    {howWeUseYourInformationBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}                                        
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      3. Sharing of Information
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We do not sell or rent your personal information. 
                      Your data may be shared only in the 
                      following cases:
                    </CustomText>
                    {sharingOfInformationBullets.map((bullet, index) => (
                      <View key={index} style={styles.bulletPointRow}>
                        <CustomText style={styles.bulletPoint}>•</CustomText>
                        <CustomText style={styles.messageGreyText}>
                          {bullet}
                        </CustomText>
                      </View>
                    ))}                                        
                  </View>
                  <View>
                    <CustomText style={styles.messageSubHeader}>
                      4. Data Security
                    </CustomText>
                    <CustomText style={styles.messageGreyText}>
                      We take reasonable steps to protect your information through encryption, secure servers, 
                      and monitoring systems. However, no method of transmission or storage is 100% secure, and 
                      we cannot guarantee absolute protection.
                    </CustomText>                                       
                  </View>                                                                       
                </View>                        
              </View>
            )}
          </ScrollView>
        </View>
      </View>
    </View>
  );
}