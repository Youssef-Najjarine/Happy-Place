import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidthPercent, scaleHeightPercent} from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import Image1 from 'assets/images/placeholderProfiles/profile-1.png';
import Image2 from 'assets/images/placeholderProfiles/profile-2.png';
import Image3 from 'assets/images/placeholderProfiles/profile-3.png';
import Image4 from 'assets/images/placeholderProfiles/profile-4.png';
import Image5 from 'assets/images/placeholderProfiles/profile-5.png';
import Image6 from 'assets/images/placeholderProfiles/profile-6.png';
import Image7 from 'assets/images/placeholderProfiles/profile-7.jpg';
import Image8 from 'assets/images/placeholderProfiles/profile-8.jpg';
import Image9 from 'assets/images/placeholderProfiles/profile-9.jpg';
import Image10 from 'assets/images/placeholderProfiles/profile-10.jpg';
import Image11 from 'assets/images/placeholderProfiles/profile-11.jpg';
import Image12 from 'assets/images/placeholderProfiles/profile-12.jpg';
import Image13 from 'assets/images/placeholderProfiles/profile-13.jpg';
import Image14 from 'assets/images/placeholderProfiles/profile-14.jpg';
import Image15 from 'assets/images/placeholderProfiles/profile-15.jpg';
import Image16 from 'assets/images/placeholderProfiles/profile-16.jpg';
import Image17 from 'assets/images/placeholderProfiles/profile-17.jpg';
import Image18 from 'assets/images/placeholderProfiles/profile-18.jpg';
import Image19 from 'assets/images/placeholderProfiles/profile-19.jpg';
import Image20 from 'assets/images/placeholderProfiles/profile-20.jpg';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: '#F9F5EA',
    height: '100%',
    width: '100%'
  },
  topNav: {
    backgroundColor: White,
    width: '100%',
    height: scaleHeightPercent(158, 812),
    justifyContent: 'space-between',
    paddingBottom: scaleHeightPercent(16, 158)
  },
  login: {
    width: '100%',
    height: scaleHeightPercent(44, 158),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: scaleWidthPercent(20),
    paddingLeft: scaleWidthPercent(20),
    backgroundColor: '#F9F9F9'
  },
  unlockAllFeatures: {
    color: Black,
    fontSize: scaleFont(14),
    fontWeight: 600,
    lineHeight: scaleHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14)
  },
  loginView: {
    width: scaleWidthPercent(62),
    height: scaleHeightPercent(32, 44),
  },
  loginBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor,
    borderBlockColor: HappyColor,
    borderRadius: 99
  },
  loginBtnTxt: {
    color: White,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  searchingView: {
    height: scaleHeightPercent(42, 158),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: scaleWidthPercent(20),
    paddingLeft: scaleWidthPercent(20),
  },
  cancelView: {
    
  },
  cancelBtn: {

  },
  cancelTxt: {
    
  }
});

const tabletStyles = StyleSheet.create({});

export default function ChatGroups() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const rootStyle = {
    ...styles.root,
    paddingBottom: bottomSafeHeight
  };

  const topNav = {
    ...styles.topNav,
    paddingTop: statusBarHeight
  }
  return (
    <View style={rootStyle}>
      <View style={topNav}>
        <View style={styles.login}>
          <View>
            <CustomText style={styles.unlockAllFeatures}>
              Unlock all features!
            </CustomText>
          </View>
          <View style={styles.loginView}>
            <TouchableOpacity style={styles.loginBtn} onPress={() => navigation.navigate('LoginOptions')}>
              <CustomText style={styles.loginBtnTxt}>Login</CustomText>
            </TouchableOpacity>
          </View>
        </View>
        <View style={styles.searchingView}>
          <View style={styles.searching}>
            <CustomText style={styles.searchingTxt}>
              Searching...
            </CustomText>
          </View>
          <View style={styles.cancelView}>
            <TouchableOpacity style={styles.cancelBtn}>
              <CustomText style={styles.cancelTxt}>Cancel</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </View>
  );
}