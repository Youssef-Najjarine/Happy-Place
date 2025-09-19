import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, Pressable, ScrollView } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  WarmIvory,
  Rosewater
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EditRedIcon from 'assets/images/profile/edit-red-icon.svg';
import LogoutIcon from 'assets/images/profile/logout-icon.svg';
import EditWhiteIcon from 'assets/images/profile/edit-white-icon.svg';
import PhoneIcon from 'assets/images/profile/grey-phone-icon.svg';
import MailIcon from 'assets/images/profile/grey-mail-icon.svg';
import ProfileBg from 'assets/images/placeholderProfiles/profile-bg.jpg';
import Image1 from 'assets/images/placeholderProfiles/profile-1.png';
const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    width: '100%',
    height: '100%'
  },
  ProfileBg: {
    width: '100%',
    height: scaleHeight(205)
  },
  backgroundImage: {
    width: '100%',
    height: '100%'
  },
  BackArrow: {
    width: scaleWidth(39),
    height: scaleHeight(39),
    borderRadius: scaleWidth(99),
    top: scaleHeight(20),
    left: scaleWidth(20),
    position: 'absolute',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  iconsMatchingSize: {
    width: scaleWidth(20),
    height: scaleHeight(20)
  },
  editAndLogout: {
    top: scaleHeight(20),
    right: scaleWidth(20),
    height: scaleHeight(39),
    gap: scaleWidth(12),
    position: 'absolute',
    flexDirection: 'row',
    alignItems: 'center'
  },
  editProfile: {
    width: scaleWidth(122),
    borderRadius: scaleWidth(99),
    gap: scaleWidth(8),
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  editProfileTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  },
  logout: {
    width: scaleWidth(39),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  editBackground: {
    width: scaleWidth(39),
    height: scaleHeight(39),
    borderRadius: scaleWidth(99),
    bottom: scaleHeight(20),
    right: scaleWidth(20),
    position: 'absolute',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  profilePhotoContainer: {
    width: scaleWidth(100),
    height: scaleHeight(100),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(4),
    bottom: scaleHeight(-50),
    right: scaleWidth(137.5),
    position: 'absolute',
    borderColor: WarmIvory
  },
  profilePhoto: {
    borderRadius: scaleWidth(99),
    width: '100%',
    height: '100%'
  },
  whiteEditBackground: {
    width: scaleWidth(31),
    height: scaleHeight(31),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(3),
    top: scaleHeight(69),
    right: scaleWidth(0.5),
    position: 'absolute',
    borderColor: WarmIvory,
      justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  whiteEditIcon: {
    width: scaleWidth(18),
    height: scaleHeight(18)
  },
  profileDetails: {
    marginTop: scaleHeight(62),
    paddingHorizontal: scaleWidth(20),
    gap: scaleHeight(16),
    width: '100%'
  },
  namesAndFriends: {
    alignItems: 'center'
  },
  usernameTxt: {
    marginBottom: scaleHeight(4),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black,
    opacity: 0.6
  },
  nameTxt: {
    marginBottom: scaleHeight(12),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  friends: {
    width: scaleWidth(127),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
      justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  friendsTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  bioTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black
  },
  readMoreTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: HappyColor
  },
  bioContainer: {
    width: '100%',
    overflow: 'hidden'
  },
  information: {
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    borderRadius: scaleWidth(12),
    width: '100%',
    backgroundColor: White
  },
  informationTxt: {
    marginBottom: scaleHeight(16),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  informationDetails: {
    gap: scaleHeight(12)
  },
  informationDetail: {
    gap: scaleHeight(6)
  },
  informationDetailMissing: {
    gap: scaleHeight(8)
  },
  informationDetailsLabelAndEditRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  informationDetailsLabelRow: {
    gap: scaleHeight(7),
    flexDirection: 'row'
  },
  informationDetailsLabelTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black
  },
  informationDetailTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black
  },
  addInformationBtn: {
    height: scaleHeight(37),
    borderRadius: scaleWidth(99),
    width: '100%',
    backgroundColor: Rosewater,
    justifyContent: 'center',
    alignItems: 'center'
  },
  addInformationTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: HappyColor
  }
});
const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    width: '100%',
    height: '100%'
  },
  ProfileBg: {
    width: '100%',
    height: scaleHeight(274.973)
  },
  backgroundImage: {
    width: '100%',
    height: '100%'
  },
  BackArrow: {
    width: 72.56,
    height: 72.56,
    borderRadius: scaleWidth(132.792),
    top: scaleHeight(26.83),
    left: scaleWidth(26.83),
    position: 'absolute',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  iconsMatchingSize: {
    width: scaleWidth(26.827),
    height: scaleHeight(26.827)
  },
  editAndLogout: {
    top: scaleHeight(26.83),
    right: scaleWidth(26.83),
    height: 72.56,
    gap: scaleWidth(16.1),
    position: 'absolute',
    flexDirection: 'row',
    alignItems: 'center'
  },
  editProfile: {
    width: scaleWidth(145.65334),
    borderRadius: scaleWidth(132.792),
    gap: scaleWidth(8),
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  editProfileTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: HappyColor
  },
  logout: {
    borderRadius: scaleWidth(132.792),
    width: 72.56,
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  editBackground: {
    borderRadius: scaleWidth(132.792),
    bottom: scaleHeight(26.83),
    right: scaleWidth(26.83),
    width: 72.56,
     height: 72.56,
    position: 'absolute',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  profilePhotoContainer: {
    width: 186.06,
    height: 186.06,
    borderRadius: scaleWidth(132.792),
    borderWidth: scaleWidth(5.365),
    bottom: -93.03,
    right: scaleWidth(304.93),
    position: 'absolute',
    borderColor: WarmIvory
  },
  profilePhoto: {
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: '100%'
  },
  whiteEditBackground: {
    width: 57.68,
    height: 57.68,
    borderRadius: scaleWidth(132.792),
    borderWidth: scaleWidth(4.024),
    top: scaleHeight(92.55),
    right: scaleWidth(0.67),
    position: 'absolute',
    borderColor: WarmIvory,
      justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  whiteEditIcon: {
    width: scaleWidth(24.144),
    height: scaleHeight(24.144)
  },
  profileDetails: {
    marginTop: scaleHeight(109.03),
    paddingHorizontal: scaleWidth(26.83),
    gap: scaleHeight(21.46),
    width: '100%'
  },
  namesAndFriends: {
    alignItems: 'center'
  },
  usernameTxt: {
    marginBottom: scaleHeight(5.37),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black,
    opacity: 0.6
  },
  nameTxt: {
    marginBottom: scaleHeight(16.1),
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 500,
    color: Black
  },
  friends: {
    width: scaleWidth(165.84534),
    height: scaleHeight(53.82667),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  friendsTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  bioTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  readMoreTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: HappyColor
  },
  bioContainer: {
    width: '100%',
    overflow: 'hidden'
  },
  information: {
    paddingVertical: scaleHeight(21.46),
    paddingHorizontal: scaleWidth(21.46),
    borderRadius: scaleWidth(16.096),
    width: '100%',
    backgroundColor: White
  },
  informationTxt: {
    marginBottom: scaleHeight(21.46),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  informationDetails: {
    gap: scaleHeight(16.1)
  },
  informationDetail: {
    gap: scaleHeight(8.05)
  },
  informationDetailMissing: {
    gap: scaleHeight(10.73)
  },
  informationDetailsLabelAndEditRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  informationDetailsLabelRow: {
    gap: scaleHeight(9.39),
    flexDirection: 'row'
  },
  informationDetailsLabelTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  informationDetailTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  addInformationBtn: {
    height: scaleHeight(48.46133),
    borderRadius: scaleWidth(132.792),
    width: '100%',
    backgroundColor: Rosewater,
    justifyContent: 'center',
    alignItems: 'center'
  },
  addInformationTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: HappyColor
  }
});
export default function Profile() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const profile = {
    background: ProfileBg,
    photo: Image1,
    username: 'youssef34Youssef Najjarine',
    name: 'Youssef Najjarine',
    friends: 48,
    bio: 'Sometimes the world feels heavy, and the colors fade to grey — but I’m here, still searching for the light. Life hasn’t been the easiest, and some days I carry more sadness than I’d like to admit. But I believe in the small magic that happens when people share a smile, a laugh, or even just a moment of kindness. Ready to heal, to open my heart, and to find happiness again — not just in myself, but in the warmth of others. If you’ve got a little sunshine to spare, I’d be glad to share it with you. Sometimes the world feels heavy, and the colors fade to grey — but I’m here, still searching for the light. Life hasn’t been the easiest, and some days I carry more sadness than I’d like to admit. But I believe in the small magic that happens when people share a smile, a laugh, or even just a moment of kindness. Ready to heal, to open my heart, and to find happiness again — not just in myself, but in the warmth of others. If you’ve got a little sunshine to spare, I’d be glad to share it with you.',
    phoneNumber: '',
    email: 'ynajjarine@gmail.com'
  }
  const formatPhoneNumber = (number) => {
    if (!number) return '';
    const cleaned = ('' + number).replace(/\D/g, '');
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    return number;
  };
  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomSafeHeight
  };
  const [bioExpanded, setBioExpanded] = useState(false);
  const isTablet = styles.bioTxt.fontSize === scaleFont(18);
  const BIO_LIMIT = isTablet ? 501 : 313;
  const BIO_COLLAPSED_HEIGHT = isTablet ? scaleHeight(187.28) : scaleHeight(132);
  const BIO_EXPANDED_HEIGHT = isTablet ? scaleHeight(187.28) : scaleHeight(132);
  const isLongBio = profile.bio.length > BIO_LIMIT;
  const shownBio = bioExpanded ? profile.bio : (isLongBio ? profile.bio.slice(0, BIO_LIMIT) : profile.bio);
  return (
    <View style={rootStyle}>
      <View style={styles.ProfileBg}>
        <Image
          source={profile.background}
          fadeDuration={0}
          progressiveRenderingEnabled={false}
          style={styles.backgroundImage}
        />
        <TouchableOpacity
          style={styles.BackArrow}
          onPress={() => navigation.goBack()}
        >
          <BackArrow {...styles.iconsMatchingSize}/>
        </TouchableOpacity>
        <View style={styles.editAndLogout}>
          <TouchableOpacity
            style={styles.editProfile}
            onPress={() => navigation.navigate('EditProfile')}
          >
            <EditRedIcon {...styles.iconsMatchingSize}/>
            <CustomText style={styles.editProfileTxt}>Edit Profile</CustomText>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.logout}
          >
            <LogoutIcon {...styles.iconsMatchingSize}/>
          </TouchableOpacity>
        </View>
        <TouchableOpacity
          style={styles.editBackground}
        >
          <EditRedIcon {...styles.iconsMatchingSize}/>
        </TouchableOpacity>
        <View style={styles.profilePhotoContainer}>
          <Image
            source={profile.photo}
            fadeDuration={0}
            progressiveRenderingEnabled={false}
            style={styles.profilePhoto}
          />
          <TouchableOpacity style={styles.whiteEditBackground}>
            <EditWhiteIcon {...styles.whiteEditIcon}/>
          </TouchableOpacity>
        </View>
      </View>
      <View style={styles.profileDetails}>
        <View style={styles.namesAndFriends}>
          <CustomText
            style={styles.usernameTxt}
            numberOfLines={1}
            ellipsizeMode="tail"
          >
            @{profile.username}
          </CustomText>
          <CustomText
            style={styles.nameTxt}
            numberOfLines={1}
            ellipsizeMode="tail"
          >
            {profile.name}
          </CustomText>
          <TouchableOpacity 
            style={styles.friends}
            onPress={() => navigation.navigate('Friends')}
          >
            <CustomText style={styles.friendsTxt}>{profile.friends} Friends</CustomText>
          </TouchableOpacity>
        </View>
        <View
          style={[
            styles.bioContainer,
            { 
              height: bioExpanded ? BIO_EXPANDED_HEIGHT : BIO_COLLAPSED_HEIGHT,
              maxHeight: bioExpanded ? BIO_EXPANDED_HEIGHT : BIO_COLLAPSED_HEIGHT 
            }
          ]}
        >
          <ScrollView
            scrollEnabled={bioExpanded}
            showsVerticalScrollIndicator={false}
          >
            <CustomText style={styles.bioTxt}>
              {shownBio}
              {isLongBio && (
                <CustomText
                  style={styles.readMoreTxt}
                  onPress={() => setBioExpanded(v => !v)}
                >
                  {bioExpanded ? ' Read less' : ' Read more...'}
                </CustomText>
              )}
            </CustomText>
          </ScrollView>
        </View>
        <View style={styles.information}>
          <CustomText style={styles.informationTxt}>Information</CustomText>
          <View style={styles.informationDetails}>
            <View style={!profile.phoneNumber ? styles.informationDetailMissing : styles.informationDetail}>
              <View style={styles.informationDetailsLabelAndEditRow}>
                <View style={styles.informationDetailsLabelRow}>
                  <PhoneIcon {...styles.iconsMatchingSize}/>
                  <CustomText style={styles.informationDetailsLabelTxt}>Mobile Number:</CustomText>
                </View>
                {profile.phoneNumber &&
                  <View>
                    <TouchableOpacity
                      onPress={() => navigation.navigate('EditEmailOrPhone', { source: 'phone' })}
                    >
                      <EditRedIcon {...styles.iconsMatchingSize}/>
                    </TouchableOpacity>
                  </View>
                }
              </View>
              {!profile.phoneNumber ?
                (
                  <View>
                    <TouchableOpacity 
                      style={styles.addInformationBtn}
                      onPress={() => navigation.navigate('AddNewEmailOrPhone', { source: 'phone' })}
                    >
                      <CustomText style={styles.addInformationTxt}>Add Mobile Number</CustomText>
                    </TouchableOpacity>
                  </View>
                )
              :
                (
                  <View>
                    <CustomText
                      style={styles.informationDetailTxt}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      {formatPhoneNumber(profile.phoneNumber)}
                    </CustomText>
                  </View>
                )
              }
            </View>
            <View style={!profile.email ? styles.informationDetailMissing : styles.informationDetail}>
              <View style={styles.informationDetailsLabelAndEditRow}>
                <View style={styles.informationDetailsLabelRow}>
                  <MailIcon {...styles.iconsMatchingSize}/>
                  <CustomText style={styles.informationDetailsLabelTxt}>Email Address:</CustomText>
                </View>
                {profile.email &&
                  <View>
                    <TouchableOpacity
                      onPress={() => navigation.navigate('EditEmailOrPhone', { source: 'email' })}
                    >
                      <EditRedIcon {...styles.iconsMatchingSize}/>
                    </TouchableOpacity>
                  </View>
                }
              </View>
              {!profile.email ?
                (
                  <View>
                    <TouchableOpacity 
                      style={styles.addInformationBtn}
                      onPress={() => navigation.navigate('AddNewEmailOrPhone', { source: 'email' })}
                    >
                      <CustomText style={styles.addInformationTxt}>Add Email Address</CustomText>
                    </TouchableOpacity>
                  </View>
                )
              :
                (
                  <View>
                    <CustomText
                      style={styles.informationDetailTxt}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      {profile.email}
                    </CustomText>
                  </View>
                )
              }
            </View>
          </View>
        </View>
      </View>
    </View>
  );
}