import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { useNavigation } from '@react-navigation/native';
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
    // alignItems: 'center'
  },
  login: {
    width: '100%',
    height: '7%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: '5.3%',
    paddingLeft: '5.3%',
    backgroundColor: '#F9F9F9'
  },
  unlockAllFeatures: {
    color: Black,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 24,
    letterSpacing: -0.16
  },
  loginBtn: {

  },
  loginBtnTxt: {

  }
});
const tabletStyles = StyleSheet.create({

});
export default function ChatGroups() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomSafeHeight
  };
  return (
    <View style={rootStyle}>
      <View style={styles.login}>
        <View>
          <CustomText style={styles.unlockAllFeatures}>
            Unlock all features!
          </CustomText>
        </View>
        <View>
          <TouchableOpacity style={styles.loginBtn} onPress={() => navigation.navigate('LoginOptions')}>
            <CustomText style={styles.loginBtnTxt}>Login</CustomText>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
}