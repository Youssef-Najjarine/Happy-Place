import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation, useFocusEffect, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import SuccessLogo from 'assets/images/passwordRecovered/password-recovered-logo.png';
const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%',
    justifyContent: 'space-between'
  },
  part1: {
      height: scaleHeight(364),
      paddingHorizontal: scaleWidth(32),
      width: '100%',
    alignItems: 'center',
    justifyContent: 'flex-end'
  },
  part2: {
    height: scaleHeight(45),
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
  passwordResetTxt: {
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
  loginView: {
    width: '100%'
  },
  loginBtn: {
    height: scaleHeight(45),
    borderRadius: scaleWidth(99),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  loginTxt: {
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
    height: scaleHeight(609.125),
    paddingHorizontal: scaleWidth(163.85),
    width: '100%',
    alignItems: 'center',
    justifyContent: 'flex-end'
  },
  part2: {
    height: scaleHeight(71.192),
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
  passwordResetTxt: {
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
  loginView: {
    width: '100%'
  },
  loginBtn: {
    height: scaleHeight(59.192),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'center',
    alignItems: 'center'
  },
  loginTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 700,
    color: HappyColor
  }
});

export default function PasswordReset() {
    const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const navigation = useNavigation();
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
          <CustomText style={styles.passwordResetTxt}>Password Reset!</CustomText>
          <CustomText style={styles.descriptionTxt}>Your password has been successfully reset!</CustomText>
        </View>
        <View style={styles.part2}>
            <View style={styles.loginView}>
                <TouchableOpacity 
                    style={styles.loginBtn}
                    onPress={() => navigation.navigate('LoginOptions')}
                >
                    <CustomText style={styles.loginTxt}>Login</CustomText>
                </TouchableOpacity>
            </View>
        </View>
    </View>
    );
}