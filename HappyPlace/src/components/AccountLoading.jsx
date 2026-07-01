import React, { useState, useEffect } from 'react';
import { View, Image, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import { HappyColor, White } from 'src/constants/colors';
import Logo from 'assets/images/global/logo.png';

const phoneStyles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  logo: {
    width: scaleWidth(120),
    height: scaleWidth(120),
    resizeMode: 'contain',
    marginBottom: scaleHeight(24)
  },
  loadingTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    minWidth: scaleWidth(170),
    textAlign: 'center',
    fontWeight: 600,
    color: White
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  logo: {
    width: scaleWidth(200),
    height: scaleWidth(200),
    resizeMode: 'contain',
    marginBottom: scaleHeight(32)
  },
  loadingTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    minWidth: scaleWidth(230),
    textAlign: 'center',
    fontWeight: 600,
    color: White
  }
});

export default function AccountLoading() {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const [dotCount, setDotCount] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => setDotCount((p) => (p + 1) % 4), 500);
    return () => clearInterval(interval);
  }, []);

  return (
    <View style={styles.root}>
      <Image source={Logo} style={styles.logo} fadeDuration={0} />
      <CustomText style={styles.loadingTxt}>{`Loading account${'.'.repeat(dotCount)}`}</CustomText>
    </View>
  );
}