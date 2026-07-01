import React, { useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import RemoteImage from 'src/components/RemoteImage';
import { HappyColor, White, Black, VeryLightGray } from 'src/constants/colors';

const phoneStyles = StyleSheet.create({
  profileAndLogin: {
    height: scaleHeight(44),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  welcomeBackTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  profileImage: {
    width: scaleWidth(44),
    height: scaleHeight(44),
    borderRadius: scaleWidth(99),
    resizeMode: 'contain'
  },
  avatarCircle: {
    width: scaleWidth(44),
    height: scaleHeight(44),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  avatarInitial: {
    fontSize: scaleFont(20),
    fontWeight: 700,
    color: White
  },
  loginBg: {
    backgroundColor: VeryLightGray
  },
  unlockAllFeaturesTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  loginView: {
    width: scaleWidth(62),
    height: scaleHeight(32)
  },
  loginBtn: {
    borderRadius: scaleWidth(99),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  loginBtnTxt: {
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontSize: scaleFont(16),
    fontWeight: 600,
    color: White
  }
});

const tabletStyles = StyleSheet.create({
  profileAndLogin: {
    height: 84,
    paddingHorizontal: scaleWidth(24),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  welcomeBackTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  profileImage: {
    width: 83.23,
    height: 83.23,
    borderRadius: scaleWidth(132.792),
    resizeMode: 'contain'
  },
  avatarCircle: {
    width: 83.23,
    height: 83.23,
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center'
  },
  avatarInitial: {
    fontSize: scaleFont(32),
    fontWeight: 700,
    color: White
  },
  loginBg: {
    backgroundColor: VeryLightGray
  },
  unlockAllFeaturesTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  loginView: {
    width: scaleWidth(79.192),
    height: scaleHeight(40.73067)
  },
  loginBtn: {
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  loginBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  }
});

export default function AccountBar({ closeMenus }) {
  const navigation = useNavigation();
  const user = useSelector((state) => state.user);
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);

  const handleLoginPressIn = useCallback(() => {
    if (closeMenus) closeMenus();
    navigation.navigate('LoginOptions');
  }, [closeMenus, navigation]);

  const handleProfilePress = useCallback(() => {
    navigation.navigate('Profile');
  }, [navigation]);

  if (user.isLoggedIn) {
    return (
      <View style={styles.profileAndLogin}>
        <CustomText style={styles.welcomeBackTxt}>Welcome Back!</CustomText>
        <View>
          <TouchableOpacity onPress={handleProfilePress}>
            {user.profilePhotoUrl ? (
              <RemoteImage uri={user.profilePhotoUrl} style={styles.profileImage} fadeDuration={0} />
            ) : (
              <View style={[styles.avatarCircle, { backgroundColor: user.avatarColor }]}>
                <CustomText style={styles.avatarInitial}>
                  {user.displayName ? user.displayName[0].toUpperCase() : '?'}
                </CustomText>
              </View>
            )}
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={[styles.profileAndLogin, styles.loginBg]}>
      <View>
        <CustomText style={styles.unlockAllFeaturesTxt}>Login to unlock all features!</CustomText>
      </View>
      <View style={styles.loginView}>
        <TouchableOpacity
          style={styles.loginBtn}
          onPressIn={handleLoginPressIn}
        >
          <CustomText style={styles.loginBtnTxt}>Login</CustomText>
        </TouchableOpacity>
      </View>
    </View>
  );
}