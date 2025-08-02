import React from 'react';
import { View, TouchableOpacity, StyleSheet, Image } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidthPercent, scaleHeightPercent} from 'src/utils/scaleLayout';
import { useNavigation } from '@react-navigation/native';
import CustomText from 'src/components/FontFamilyText';
import Logo from 'assets/images/global/logo.png';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: scaleHeightPercent(279.90),
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoImg: {
    width: scaleWidthPercent(188),
    height: scaleHeightPercent(188, 279.90),
    resizeMode: 'contain'
  },
  card: {
    height: scaleHeightPercent(532.1),
    backgroundColor: White,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: scaleHeightPercent(24, 692)
  },
  header: {
    height: scaleHeightPercent(73,483),
    justifyContent: 'space-between'
  },
  helpButtons: {
    height: scaleHeightPercent(168,483),
    width: scaleWidthPercent(311),
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: scaleWidthPercent(271),
    height: scaleHeightPercent(92,483),
    justifyContent: 'space-between'
  },
  heading: {
    color: HappyColor,
    fontSize: scaleFont(32),
    fontWeight: 800,
    lineHeight: scaleHeight(38.4),
    letterSpacing: scaleLetterSpacing(-0.32)
  },
  subhead: {
    color: Black,
    textAlign: 'center',
    fontSize: scaleFont(18),
    fontWeight: 600,
    lineHeight: scaleHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18)
  },
  helpMeBtn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(10, 311),
    borderWidth: 1.5,
    borderColor: Black,
    borderRadius: 99,
    height: scaleHeightPercent(76, 168),
    backgroundColor: White
  },
  emojis: {
    width: scaleWidthPercent(32, 311),
    height: scaleHeightPercent(32, 76),
    resizeMode: 'contain'
  },
  helpMeBtnText: {
    color: Black,
    fontSize: scaleFont(24),
    fontWeight: 700,
    lineHeight: scaleHeight(36),
    letterSpacing: scaleLetterSpacing(-0.48)
  },
  iCanHelpBtn: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(10, 311),
    borderWidth: 0,
    borderRadius: 99,
    height: scaleHeightPercent(76, 168),
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: scaleFont(24),
    fontWeight: 700,
    lineHeight: scaleHeight(36),
    letterSpacing: scaleLetterSpacing(-0.48)
  },
  signUp: {
    width: '100%',
    height: scaleHeightPercent(31, 92),
    alignItems: 'center'
  },
  signUpBtn: {
    backgroundColor: Black,
    borderRadius: 99,
    width: scaleWidthPercent(112, 271),
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    color: White,
    fontSize: scaleFont(18),
    fontWeight: 800,
    lineHeight: scaleHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18)
  },
  divider: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidthPercent(8, 271),
    height: scaleHeightPercent(21, 92),
  },
  line: {
    width: scaleWidthPercent(121, 271),
    height: scaleHeightPercent(1, 21),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: scaleFont(14),
    fontWeight: 600,
    lineHeight: scaleHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    opacity: 0.8
  },
  alreadyHaveAccount: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: scaleHeightPercent(24, 92),
    gap: scaleWidthPercent(5, 225),
  },
  loginText: {
    color: Black,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  loginLink: {
    color: HappyColor,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: HappyColor,
    height: '100%',
    width: '100%'
  },
  topSection: {
    height: scaleHeightPercent(401),
    width: '100%'
  },
  logoBox: {
    height: '100%',
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoImg: {
    width: scaleWidthPercent(252.17),
    height: scaleHeightPercent(252.17, 401),
    resizeMode: 'contain'
  },
  card: {
    height: scaleHeightPercent(732),
    backgroundColor: White,
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: scaleHeightPercent(32, 692)
  },
  header: {
    height: scaleHeightPercent(91.73, 692),
    justifyContent: 'space-between'
  },
  helpButtons: {
    width: scaleWidthPercent(696),
    height: scaleHeightPercent(224.76668, 692),
    justifyContent: 'space-between'
  },
  signUpLogIn: {
    width: scaleWidthPercent(584),
    height: scaleHeightPercent(103.09533, 692),
    justifyContent: 'space-between'
  },
  heading: {
    color: HappyColor,
    fontSize: scaleFont(40),
    fontWeight: 800,
    lineHeight: scaleHeight(48),
    letterSpacing: scaleLetterSpacing(-0.4)
  },
  subhead: {
    color: Black,
    textAlign: 'center',
    fontSize: scaleFont(22),
    fontWeight: 500,
    lineHeight: scaleHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22)
  },
  helpMeBtn: {
    width: '100%',
    height: scaleHeightPercent(101.65334, 224.76668),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(13.41, 696),
    borderWidth: 2.012,
    borderColor: Black,
    borderRadius: 132.792,
    backgroundColor: White
  },
  emojis: {
    width: scaleWidthPercent(42.92267,696),
    height: scaleHeightPercent(42.92267, 101.65334),
    resizeMode: 'contain'
  },
  helpMeBtnText: {
    color: Black,
    fontSize: scaleFont(32),
    fontWeight: 700,
    lineHeight: scaleHeight(48),
    letterSpacing: scaleLetterSpacing(-0.64)
  },
  iCanHelpBtn: {
    width: '100%',
    height: scaleHeightPercent(101.65334, 224.76668),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidthPercent(13.41, 696),
    borderWidth: 0,
    borderRadius: 132.792,
    backgroundColor: HappyColor
  },
  iCanHelpBtnText: {
    color: White,
    fontSize: scaleFont(32),
    fontWeight: 700,
    lineHeight: scaleHeight(48),
    letterSpacing: scaleLetterSpacing(-0.64)
  },
  signUp: {
    width: '100%',
    height: scaleHeightPercent(38.6533, 103.09533),
    alignItems: 'center'
  },
  signUpBtn: {
    backgroundColor: Black,
    borderRadius: 132.792,
    width: scaleWidthPercent(142.384,584),
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
  },
  signUpBtnText: {
    color: White,
    fontSize: scaleFont(22),
    fontWeight: 800,
    lineHeight: scaleHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22)
  },
  divider: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidthPercent(10.73, 584),
    height: 24
  },
  line: {
    width: scaleWidthPercent(273.6935, 584),
    height: scaleHeightPercent(1.341, 24),
    backgroundColor: Black,
    opacity: 0.6
  },
  or: {
    color: Black,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    opacity: 0.8
  },
  alreadyHaveAccount: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: scaleHeightPercent(30, 103.09533),
    gap: scaleWidthPercent(5, 281)
  },
  loginText: {
    color: Black,
    fontSize: scaleFont(20),
    fontWeight: 600,
    lineHeight: scaleHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2)
  },
  loginLink: {
    color: HappyColor,
    fontSize: scaleFont(20),
    fontWeight: 600,
    lineHeight: scaleHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2)
  }
});

export default function Home() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
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
          <TouchableOpacity style={styles.helpMeBtn} onPress={() => navigation.navigate('ChatGroups')}>
            <SadEmoji {...styles.emojis}/>
            <CustomText style={styles.helpMeBtnText}>HELP ME</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.iCanHelpBtn} onPress={() => navigation.navigate('ChatGroups')}>
            <HappyEmoji {...styles.emojis}/>
            <CustomText style={styles.iCanHelpBtnText}>I CAN HELP</CustomText>
          </TouchableOpacity>
        </View>
        <View style={styles.signUpLogIn}>
          <View style={styles.signUp}>
            <TouchableOpacity style={styles.signUpBtn}>
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
            <CustomText style={styles.loginLink} onPress={() => navigation.navigate('LoginOptions')}>Login</CustomText>
          </View>
        </View>
      </View>
    </View>
  );
}