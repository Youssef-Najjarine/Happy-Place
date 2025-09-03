import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import EditChatNameModal from 'src/components/EditChatNameModal';
import DeleteChatGroupModal from 'src/components/DeleteChatGroupModal';
import LeaveChatGroupModal from 'src/components/LeaveChatGroupModal';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import SearchIcon from 'assets/images/global/search-icon.svg';
import SortIcon from 'assets/images/chatGroups/sort-icon.svg';
import LinkIcon from 'assets/images/chatGroups/share-chat-link-icon.svg';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import DownArrowIcon from 'assets/images/global/arrow-down-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import PrivateIcon from 'assets/images/global/private-chat-icon.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';
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
    width: '100%',
    position: 'relative'
  },
  topNav: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    borderBottomLeftRadius: scaleWidth(24),
    borderBottomRightRadius: scaleWidth(24),
    marginBottom: scaleHeight(20),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'space-between'
  },
        //     <View style={styles.chatHeaderRow}>
        //     <View style={styles.backArrowAndfriendsRow}>
        //         <View>
        //             <TouchableOpacity
        //                 style={styles.BackArrow}
        //                 onPress={() => navigation.goBack()}
        //             >
        //                 <BackArrow {...styles.backArrowIcon}/>
        //             </TouchableOpacity>
        //         </View>
        //         <View>
        //             <CustomText style={styles.addFriendsTxt}>I'm depressed!</CustomText>
        //         </View>
        //     </View>
        //     <View styles={styles.privacyLabelAndEllipsisRow}>
        //         <View styles={styles.privacyLabel}>
        //             <CustomText styles={styles.privacyLabelTxt}>Public</CustomText>
        //         </View>
        //         <View>
        //             <TouchableOpacity
        //                 style={styles.ellipsisBackground}
        //                 onPressIn={() => handleEllipsisPress()}
        //             >
        //                 <EllipsisIcon {...styles.ellipsis} />
        //             </TouchableOpacity>
        //         </View>
        //     </View>
        //   </View>
  backArrowAndfriendsRow: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center'
  },
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  addFriendsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },  
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
  loginBg: { 
    backgroundColor: '#F9F9F9' 
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
  },
  ellipsisBackground: { 
    width: scaleWidth(36), 
    height: scaleHeight(36), 
    borderRadius: scaleWidth(99), 
    backgroundColor: '#F9F9F9', 
    justifyContent: 'center', 
    alignItems: 'center' 
  },
  ellipsis: { 
    width: scaleWidth(28), 
    height: scaleHeight(28) 
  },
  topNavIcons: { 
    width: scaleWidth(20), 
    height: scaleHeight(20), 
    resizeMode: 'contain' 
  }
});

const tabletStyles = StyleSheet.create({
  root: { 
    backgroundColor: '#F9F5EA', 
    height: '100%', 
    width: '100%',
    position: 'relative'
  },
  topNav: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(20),
    borderBottomLeftRadius: scaleWidth(32.192),
    borderBottomRightRadius: scaleWidth(32.192),
    marginBottom: scaleHeight(26.83),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'space-between'
  },
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
  loginBg: { 
    backgroundColor: '#F9F9F9' 
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
  },
  topNavIcons: { 
    width: scaleWidth(24), 
    height: scaleHeight(24), 
    resizeMode: 'contain' 
  }
});

export default function ChatGroup() {
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const cameFromLogin = route.params?.from === 'login';

  const handleLoginPressIn = useCallback(() => {
    navigation.navigate('LoginOptions');
  }, [navigation]);

  const handleConfirmEditName = useCallback((newName) => {
    setShowEditChatNameModal(false);
  }, []);

  const handleConfirmDeleteChatGroup = useCallback(() => {
    setShowDeleteChatGroupModal(false);
  }, []);

  const handleConfirmLeaveChatGroup = useCallback(() => {
    setShowLeaveChatGroupModal(false);
  }, []);

  const topNavStyle = useMemo(
    () => ({ ...styles.topNav, paddingTop: statusBarHeight }),
    [styles.topNav, statusBarHeight]
  );

  return (
    <>
      <View style={styles.root}>
        <View style={topNavStyle}>
          {cameFromLogin ? (
            <View style={styles.profileAndLogin}>
              <CustomText style={styles.welcomeBackTxt}>Welcome Back!</CustomText>
              <View>
                <TouchableOpacity onPress={() => navigation.navigate('Profile')}>
                  <Image source={Image1} style={styles.profileImage} fadeDuration={0} />
                </TouchableOpacity>
              </View>
            </View>
          ) : (
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
          )}
          <View style={styles.chatHeaderRow}>
            <View style={styles.backArrowAndfriendsRow}>
                <View>
                    <TouchableOpacity
                        style={styles.BackArrow}
                        onPress={() => navigation.goBack()}
                    >
                        <BackArrow {...styles.backArrowIcon}/>
                    </TouchableOpacity>
                </View>
                <View>
                    <CustomText style={styles.addFriendsTxt}>I'm depressed!</CustomText>
                </View>
            </View>
            <View styles={styles.privacyLabelAndEllipsisRow}>
                <View styles={styles.privacyLabel}>
                    <CustomText styles={styles.privacyLabelTxt}>Public</CustomText>
                </View>
                <View>
                    <TouchableOpacity
                        style={styles.ellipsisBackground}
                        onPressIn={() => handleEllipsisPress()}
                    >
                        <EllipsisIcon {...styles.ellipsis} />
                    </TouchableOpacity>
                </View>
            </View>
          </View>
        </View>
      </View>
      <EditChatNameModal
        visible={showEditChatNameModal}
        initialName={''}
        maxLen={100}
        onConfirm={handleConfirmEditName}
        onCancel={() => { setShowEditChatNameModal(false); }}
      />
      <DeleteChatGroupModal
        visible={showDeleteChatGroupModal}
        onConfirm={handleConfirmDeleteChatGroup}
        onCancel={() => { setShowDeleteChatGroupModal(false); }}
      /> 
      <LeaveChatGroupModal
        visible={showLeaveChatGroupModal}
        onConfirm={handleConfirmLeaveChatGroup}
        onCancel={() => { setShowLeaveChatGroupModal(false); }}
      />                  
    </>
  );
}