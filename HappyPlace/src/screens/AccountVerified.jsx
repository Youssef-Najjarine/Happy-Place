import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation, useFocusEffect, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black, TranslucentWhite } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import SuccessLogo from 'assets/images/accountVerified/account-verified-success-logo.png';
import HappyCheck from 'assets/images/accountVerified/happy-check-icon.svg';
const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%',
    justifyContent: 'space-between'
  },
  part1: {
      height: scaleHeight(388),
      paddingHorizontal: scaleWidth(32),
      width: '100%',
    alignItems: 'center',
    justifyContent: 'flex-end'
  },
  part2: {
    height: scaleHeight(78.5),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    alignItems: 'center'
  },
  successLogo: {
    width: scaleWidth(124),
    height: scaleHeight(124),
    marginBottom: scaleHeight(32),
    resizeMode: 'contain'
  },
  accountVerifiedTxt: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    marginBottom: scaleHeight(8),
    fontWeight: 700,
    color: White
  },
  descriptionTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 400,
    color: White,
    textAlign: 'center'
  },
  rememberMeRow: {
    gap: scaleWidth(6),
    marginBottom: scaleHeight(12.5),
    flexDirection: 'row',
    alignItems: 'center'
  },
  checkbox: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    borderWidth: scaleWidth(1.5),
    borderRadius: scaleWidth(8),
     borderColor: White,
    backgroundColor: TranslucentWhite
  },
  checkboxSelected: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    borderRadius: scaleWidth(8),
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  happyCheckIcon: {
    width: scaleWidth(8.5),
    height: scaleHeight(5.66)
  },
  rememberMeTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    fontWeight: 500,
    color: White
  },
  getStartedView: {
    width: '100%'
  },
  getStartedBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  getStartedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 700,
    color: HappyColor
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%',
    justifyContent: 'space-between'
  },
  part1: {
      height: scaleHeight(641.125),
      paddingHorizontal: scaleWidth(163.84),
      width: '100%',
    alignItems: 'center',
    justifyContent: 'flex-end'
  },
  part2: {
    height: scaleHeight(110.192),
    paddingHorizontal: scaleWidth(24),
    width: '100%',
    alignItems: 'center'
  },
  successLogo: {
    width: scaleWidth(166.325),
    height: scaleHeight(166.325),
    marginBottom: scaleHeight(32),
    resizeMode: 'contain'
  },
  accountVerifiedTxt: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    marginBottom: scaleHeight(12),
    fontWeight: 700,
    color: White
  },
  descriptionTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(32.192),
    fontWeight: 400,
    color: White,
    textAlign: 'center'
  },
  rememberMeRow: {
    gap: scaleWidth(8),
    marginBottom: scaleHeight(12.09),
    flexDirection: 'row',
    alignItems: 'center'
  },
  checkbox: {
    width: 37.21,
    height: 37.21,
    borderWidth: scaleWidth(2),
    borderRadius: scaleWidth(10.731),
     borderColor: White,
    backgroundColor: TranslucentWhite
  },
  checkboxSelected: {
    width: 37.21,
    height: 37.21,
    borderRadius: scaleWidth(10.731),
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  happyCheckIcon: {
    width: scaleWidth(11.401),
    height: scaleHeight(7.592)
  },
  rememberMeTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.36),
    fontWeight: 500,
    color: White
  },
  getStartedView: {
    width: '100%'
  },
  getStartedBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  getStartedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: HappyColor
  }
});

export default function AccountVerified() {
    const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const navigation = useNavigation();
    const [rememberMe, setRememberMe] = useState(false);
    const route = useRoute();

    const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomSafeHeight
    };

    return (
    <View style={rootStyle}>
        <View style={styles.part1}>
          <Image
            source={SuccessLogo}
            style={styles.successLogo}
            accessible={true}
            accessibilityLabel="Success logo"
          />
          <CustomText style={styles.accountVerifiedTxt}>Account Verified</CustomText>
          <CustomText style={styles.descriptionTxt}>Your account has been verified succesfully, now let’s enjoy Happy Place features!</CustomText>
        </View>
        <View style={styles.part2}>
            <View style={styles.rememberMeRow}>
                <TouchableOpacity 
                style={rememberMe ? styles.checkboxSelected : styles.checkbox} 
                    onPress={() => setRememberMe(!rememberMe)}
                >
                    {rememberMe && (
                        <HappyCheck {...styles.happyCheckIcon}/>
                    )}
                </TouchableOpacity>
                <CustomText style={styles.rememberMeTxt}>Login & Remember me</CustomText>
            </View>
            <View style={styles.getStartedView}>
                <TouchableOpacity 
                    style={styles.getStartedBtn}
                    onPress={() => navigation.navigate('ChatGroups')}
                >
                    <CustomText style={styles.getStartedTxt}>Get Started</CustomText>
                </TouchableOpacity>
            </View>
        </View>
    </View>
    );
}