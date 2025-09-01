import React, {useCallback} from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale} from 'src/utils/scaleLayout';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import CustomText from 'src/components/FontFamilyText';
import Logo from 'assets/images/global/logo.png';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice'; 
const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '34.5%',
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  logoImg: {
    width: scaleWidth(188),
    height: scaleHeight(188),
    resizeMode: 'contain'
  },
  card: {
    height: '65.5%',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingTop: scaleHeight(24),
    backgroundColor: White,
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  header: {
    height: scaleHeight(73),
    justifyContent: 'space-between'
  },
  helpButtons: {
    height: scaleHeight(168),
    width: scaleWidth(311),
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: scaleWidth(271),
    height: scaleHeight(92),
    justifyContent: 'space-between'
  },
  heading: {
    fontSize: scaleFont(32),
    lineHeight: scaleLineHeight(38.4),
    letterSpacing: scaleLetterSpacing(-0.32),
    color: HappyColor,
    fontWeight: 800
  },
  subhead: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    textAlign: 'center',
    fontWeight: 600
  },
  helpMeBtn: {
    gap: scaleWidth(10),
    borderWidth: scaleWidth(1.5),
    borderRadius: scaleWidth(99),
    height: scaleHeight(76),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: Black,
    backgroundColor: White
  },
  emojis: {
    width: scaleWidth(32),
    height: scaleHeight(32),
    resizeMode: 'contain'
  },
  helpMeBtnText: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    letterSpacing: scaleLetterSpacing(-0.48),
    color: Black,
    fontWeight: 700
  },
  iCanHelpBtn: {
    gap: scaleWidth(10),
    borderWidth: scaleWidth(0),
    borderRadius: scaleWidth(99),
    height: scaleHeight(76),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    letterSpacing: scaleLetterSpacing(-0.48),
    color: White,
    fontWeight: 700,
  },
  signUp: {
    height: scaleHeight(31),
    width: '100%',
    alignItems: 'center'
  },
  signUpBtn: {
    width: scaleWidth(112),
    borderRadius: scaleWidth(99),
    backgroundColor: Black,
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: White,
    fontWeight: 800
  },
  divider: {
    gap: scaleWidth(8),
    height: scaleHeight(21),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  line: {
    width: scaleWidth(121),
    height: scaleHeight(1),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    height: scaleHeight(24),
    gap: scaleWidth(5),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  loginText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  loginLink: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 600
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: '35.4%',
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  logoImg: {
    width: scaleWidth(252.17),
    height: scaleHeight(252.17),
    resizeMode: 'contain'
  },
  card: {
    paddingTop: scaleHeight(32),
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    height: '64.6%',
    backgroundColor: White,
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  header: {
    height: scaleHeight(91.73),
    justifyContent: 'space-between'
  },
  helpButtons: {
    width: scaleWidth(696),
    height: scaleHeight(224.76668),
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: scaleWidth(584),
    height: scaleHeight(103.09533),
    justifyContent: 'space-between'
  },
  heading: {
    fontSize: scaleFont(40),
    lineHeight: scaleLineHeight(48),
    letterSpacing: scaleLetterSpacing(-0.4),
    color: HappyColor,
    fontWeight: 800
  },
  subhead: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    color: Black,
    textAlign: 'center',
    fontWeight: 500
  },
  helpMeBtn: {
    height: scaleHeight(101.65334),
    gap: scaleWidth(13.41),
    borderWidth: scaleWidth(2.012),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: Black,
    backgroundColor: White
  },
  emojis: {
    width: scaleWidth(42.92267,696),
    height: scaleHeight(42.92267),
    resizeMode: 'contain'
  },
  helpMeBtnText: {
    fontSize: scaleFont(32),
    lineHeight: scaleLineHeight(48),
    letterSpacing: scaleLetterSpacing(-0.64),
    color: Black,
    fontWeight: 700
  },
  iCanHelpBtn: {
    height: scaleHeight(101.65334),
    gap: scaleWidth(13.41),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    fontSize: scaleFont(32),
    lineHeight: scaleLineHeight(48),
    letterSpacing: scaleLetterSpacing(-0.64),
    color: White,
    fontWeight: 700
  },
  signUp: {
    height: scaleHeight(38.6533),
    width: '100%',
    alignItems: 'center'
  },
  signUpBtn: {
    width: scaleWidth(142.384),
    borderRadius: scaleWidth(132.792),
    backgroundColor: Black,
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    color: White,
    fontWeight: 800
  },
  divider: {
    gap: scaleWidth(10.73),
    height: scaleHeight(24),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  line: {
    width: scaleWidth(273.6935),
    height: scaleHeight(1.341),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600,
    opacity: 0.8
  },
  alreadyHaveAccount: {
    height: scaleHeight(30),
    gap: scaleWidth(5),
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center'
  },
  loginText: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  loginLink: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: HappyColor,
    fontWeight: 600
  }
});

export default function Home() {
  const dispatch = useDispatch();
  //   useFocusEffect(
  //   useCallback(() => {
  //     dispatch(showLoading());
  //     return () => {
  //       dispatch(hideLoading());
  //     };
  //   }, [dispatch])
  // );
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();

  const handleHelpMe = () => {
    dispatch(showLoading()); // Show modal during navigation/async
    // Simulate async (e.g., API call before navigation)
    setTimeout(() => {
      dispatch(hideLoading()); // Hide when done
      navigation.navigate('ChatGroups', { startSearching: true });
    }, 1000); // Replace with actual async logic
  };

  const handleICanHelp = () => {
    dispatch(showLoading());
    setTimeout(() => {
      dispatch(hideLoading());
      navigation.navigate('ChatGroups', { startSearching: true });
    }, 1000);
  };

  const handleSignUp = () => {
    dispatch(showLoading());
    setTimeout(() => {
      dispatch(hideLoading());
      navigation.navigate('CreateAccount');
    }, 1000);
  };

  const handleLogin = () => {
    dispatch(showLoading());
    setTimeout(() => {
      dispatch(hideLoading());
      navigation.navigate('LoginOptions');
    }, 1000);
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
      <View style={styles.topSection}>
        <View style={styles.logoBox}>
          <Image
            source={Logo}
            style={styles.logoImg}
            accessible={true}
            accessibilityLabel="App logo"
          />
        </View>
      </View>
      <View style={cardStyle}>
        <View style={styles.header}>
          <CustomText style={styles.heading}>What's your issue?</CustomText>
          <CustomText style={styles.subhead}>Someone is here to help.</CustomText>
        </View>
        <View style={styles.helpButtons}>
          <TouchableOpacity style={styles.helpMeBtn} onPress={handleHelpMe}>
            <SadEmoji {...styles.emojis}/>
            <CustomText style={styles.helpMeBtnText}>HELP ME</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.iCanHelpBtn} onPress={handleICanHelp}>
            <HappyEmoji {...styles.emojis}/>
            <CustomText style={styles.iCanHelpBtnText}>I CAN HELP</CustomText>
          </TouchableOpacity>
        </View>
        <View style={styles.signUpLogIn}>
          <View style={styles.signUp}>
            <TouchableOpacity style={styles.signUpBtn} onPress={handleSignUp}>
              <CustomText style={styles.signUpBtnText}>Sign Up</CustomText>
            </TouchableOpacity>
          </View>
          <View style={styles.divider}>
            <View style={styles.line} />
            <CustomText style={styles.or}>or</CustomText>
            <View style={styles.line} />
          </View>
          <View style={styles.alreadyHaveAccount}>
            <CustomText style={styles.loginText}>
              Already have an account?
            </CustomText>
            <CustomText style={styles.loginLink} onPress={handleLogin}>Login</CustomText>
          </View>
        </View>
      </View>
    </View>
  );
}