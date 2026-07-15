import React, { useState, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, ScrollView, Alert, Linking } from 'react-native';
import { useNavigation, useRoute, useFocusEffect } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import useLogout from 'src/hooks/useLogout';
import LinearGradient from 'react-native-linear-gradient';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { setUser } from 'store/userSlice';
import ImagePicker from 'react-native-image-crop-picker';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  WarmIvory,
  Rosewater,
  Charcoal
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import RemoteImage from 'src/components/RemoteImage';
import PhotoActionSheet from 'src/components/PhotoActionSheet';
import UnfriendModal from 'src/components/UnfriendModal';
import BlockUserModal from 'src/components/BlockUserModal';
import { showToast } from 'src/components/Toast';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EditRedIcon from 'assets/images/profile/edit-red-icon.svg';
import LogoutIcon from 'assets/images/profile/logout-icon.svg';
import EditWhiteIcon from 'assets/images/profile/edit-white-icon.svg';
import PhoneIcon from 'assets/images/profile/grey-phone-icon.svg';
import MailIcon from 'assets/images/profile/grey-mail-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import tokenStorage from 'services/tokenStorage';
import profileService from 'services/profileService';
import {
  useSendFriendRequestMutation,
  useCancelFriendRequestMutation,
  useAcceptFriendRequestMutation,
  useDeclineFriendRequestMutation,
  useUnfriendMutation,
  useBlockUserMutation,
} from 'store/friendsApi';

const ACTION_SHEET_DISMISS_DELAY_MS = 600;

const PROFILE_PHOTO_PICKER_OPTIONS = {
  width: 800,
  height: 800,
  cropping: true,
  forceJpg: true,
  mediaType: 'photo',
  compressImageQuality: 0.9,
  cropperToolbarTitle: 'Crop Profile Photo'
};

const BACKGROUND_PHOTO_PICKER_OPTIONS = {
  width: 1200,
  height: 400,
  cropping: true,
  forceJpg: true,
  mediaType: 'photo',
  compressImageQuality: 0.9,
  cropperToolbarTitle: 'Crop Background Photo'
};

const getBackgroundGradient = (avatarColor) => {
  if (!avatarColor) return ['#E17055', '#C0392B'];
  const r = parseInt(avatarColor.slice(1, 3), 16);
  const g = parseInt(avatarColor.slice(3, 5), 16);
  const b = parseInt(avatarColor.slice(5, 7), 16);
  return [
    `rgb(${Math.min(255, r + 40)}, ${Math.min(255, g + 40)}, ${Math.min(255, b + 40)})`,
    `rgb(${Math.max(0, r - 40)}, ${Math.max(0, g - 40)}, ${Math.max(0, b - 40)})`
  ];
};

const formatPhoneNumber = (number) => {
  if (!number) return '';
  const cleaned = ('' + number).replace(/\D/g, '');
  if (cleaned.length === 10) {
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
  }
  return number;
};

const waitForActionSheetDismiss = () => new Promise(resolve => setTimeout(resolve, ACTION_SHEET_DISMISS_DELAY_MS));

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
  relationshipActions: {
    top: scaleHeight(20),
    right: scaleWidth(20),
    height: scaleHeight(39),
    gap: scaleWidth(12),
    position: 'absolute',
    flexDirection: 'row',
    alignItems: 'center'
  },
  relationshipBtn: {
    width: scaleWidth(122),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  relationshipBtnTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  },
  relationshipAcceptBtn: {
    width: scaleWidth(92),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  relationshipAcceptTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  relationshipCircleBtn: {
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
  avatarCircle: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  avatarInitial: {
    fontSize: scaleFont(36),
    fontWeight: 700,
    color: White
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
  },
  errorBackArrowContainer: {
    paddingTop: scaleHeight(20),
    paddingLeft: scaleWidth(20)
  },
  errorBackArrow: {
    width: scaleWidth(39),
    height: scaleHeight(39),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  errorContent: {
    flex: 1,
    paddingHorizontal: scaleWidth(40),
    justifyContent: 'center',
    alignItems: 'center'
  },
  errorHeading: {
    marginBottom: scaleHeight(8),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 700,
    color: Black,
    textAlign: 'center'
  },
  errorMessage: {
    marginBottom: scaleHeight(24),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Charcoal,
    textAlign: 'center'
  },
  retryBtn: {
    width: scaleWidth(140),
    height: scaleHeight(44),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  retryBtnTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 700,
    color: White
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
  relationshipActions: {
    top: scaleHeight(26.83),
    right: scaleWidth(26.83),
    height: 72.56,
    gap: scaleWidth(16.1),
    position: 'absolute',
    flexDirection: 'row',
    alignItems: 'center'
  },
  relationshipBtn: {
    width: scaleWidth(145.65334),
    borderRadius: scaleWidth(132.792),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: White
  },
  relationshipBtnTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: HappyColor
  },
  relationshipAcceptBtn: {
    width: scaleWidth(112),
    borderRadius: scaleWidth(132.792),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  relationshipAcceptTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  relationshipCircleBtn: {
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
  avatarCircle: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center'
  },
  avatarInitial: {
    fontSize: scaleFont(56),
    fontWeight: 700,
    color: White
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
  },
  errorBackArrowContainer: {
    paddingTop: scaleHeight(26.83),
    paddingLeft: scaleWidth(26.83)
  },
  errorBackArrow: {
    width: 72.56,
    height: 72.56,
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  errorContent: {
    flex: 1,
    paddingHorizontal: scaleWidth(60),
    justifyContent: 'center',
    alignItems: 'center'
  },
  errorHeading: {
    marginBottom: scaleHeight(10.73),
    fontSize: scaleFont(24),
    lineHeight: scaleLineHeight(36),
    letterSpacing: scaleLetterSpacing(-0.24),
    fontWeight: 700,
    color: Black,
    textAlign: 'center'
  },
  errorMessage: {
    marginBottom: scaleHeight(32),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Charcoal,
    textAlign: 'center'
  },
  retryBtn: {
    width: scaleWidth(188),
    height: scaleHeight(56),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  retryBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 700,
    color: White
  }
});

export default function Profile() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const logout = useLogout();
  const dispatch = useDispatch();

  const targetUsername = route.params?.username;
  const isOwnProfile = !targetUsername;
  const isTabInstance = route.name === 'MyProfile';
  const bottomInsetHeight = isTabInstance ? 0 : bottomSafeHeight;

  const [profile, setProfile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [bioExpanded, setBioExpanded] = useState(false);
  const [photoActionSheet, setPhotoActionSheet] = useState(null);
  const [unfriendModalVisible, setUnfriendModalVisible] = useState(false);
  const [blockModalVisible, setBlockModalVisible] = useState(false);

  const [sendFriendRequest] = useSendFriendRequestMutation();
  const [cancelFriendRequest] = useCancelFriendRequestMutation();
  const [acceptFriendRequest] = useAcceptFriendRequestMutation();
  const [declineFriendRequest] = useDeclineFriendRequestMutation();
  const [unfriend] = useUnfriendMutation();
  const [blockUser] = useBlockUserMutation();

  const fetchProfile = useCallback(async () => {
    setProfile(null);
    setIsLoading(true);
    setHasError(false);
    setBioExpanded(false);
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) {
        setHasError(true);
        return;
      }
      let response;
      if (isOwnProfile) {
        response = await profileService.getMyProfile(token);
      } else {
        response = await profileService.getPublicUserProfile(token, targetUsername);
      }
      if (response.ok) {
        const data = await response.json();
        setProfile(data);
      } else {
        setHasError(true);
      }
    } catch {
      setHasError(true);
    } finally {
      setIsLoading(false);
      dispatch(hideLoading());
    }
  }, [isOwnProfile, targetUsername, dispatch]);

  useFocusEffect(
    useCallback(() => {
      fetchProfile();
    }, [fetchProfile])
  );

  const showPermissionDeniedAlert = () => {
    Alert.alert(
      'Permission Required',
      'Please enable camera and photo access in Settings to upload photos.',
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Open Settings', onPress: () => Linking.openSettings() }
      ]
    );
  };

  const handlePickerError = (error) => {
    if (error?.code === 'E_PICKER_CANCELLED') return;
    if (error?.code === 'E_PICKER_CANNOT_RUN_CAMERA_ON_SIMULATOR') {
      showToast('Camera is not available in the iOS Simulator. Use a real device to take a photo.', 'error');
      return;
    }
    if (error?.code === 'E_NO_CAMERA_PERMISSION' || error?.code === 'E_NO_LIBRARY_PERMISSION' || error?.code === 'E_PERMISSION_MISSING') {
      showPermissionDeniedAlert();
      return;
    }
    showToast('Unable to open photo picker. Please try again.', 'error');
  };

  const uploadPhoto = async (photoType, image) => {
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) {
        showToast('Session expired. Please log in again.', 'error');
        return;
      }
      const fileName = image.filename || 'photo.jpg';
      const mimeType = image.mime || 'image/jpeg';
      let response;
      if (photoType === 'profile') {
        response = await profileService.uploadProfilePhoto(token, image.path, fileName, mimeType);
      } else {
        response = await profileService.uploadBackgroundPhoto(token, image.path, fileName, mimeType);
      }
      if (response.ok) {
        const data = await response.json();
        setProfile(data);
        if (photoType === 'profile') {
          dispatch(setUser({
            displayName: data.displayName,
            username: data.username,
            avatarColor: data.avatarColor,
            profilePhotoUrl: data.profilePhotoUrl
          }));
        }
        showToast(photoType === 'profile' ? 'Profile photo updated' : 'Background photo updated', 'success');
      } else if (response.status === 413) {
        showToast('Photo is too large. Maximum 50 MB.', 'error');
      } else if (response.status === 401) {
        showToast('Session expired. Please log in again.', 'error');
      } else {
        showToast('Unable to upload photo. Please try again.', 'error');
      }
    } catch {
      showToast('Network error. Please check your connection.', 'error');
    } finally {
      dispatch(hideLoading());
    }
  };

  const removePhoto = async (photoType) => {
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) {
        showToast('Session expired. Please log in again.', 'error');
        return;
      }
      let response;
      if (photoType === 'profile') {
        response = await profileService.removeProfilePhoto(token);
      } else {
        response = await profileService.removeBackgroundPhoto(token);
      }
      if (response.ok) {
        const data = await response.json();
        setProfile(data);
        if (photoType === 'profile') {
          dispatch(setUser({
            displayName: data.displayName,
            username: data.username,
            avatarColor: data.avatarColor,
            profilePhotoUrl: data.profilePhotoUrl
          }));
        }
        showToast(photoType === 'profile' ? 'Profile photo removed' : 'Background photo removed', 'success');
      } else if (response.status === 401) {
        showToast('Session expired. Please log in again.', 'error');
      } else {
        showToast('Unable to remove photo. Please try again.', 'error');
      }
    } catch {
      showToast('Network error. Please check your connection.', 'error');
    } finally {
      dispatch(hideLoading());
    }
  };

  const handleTakePhoto = async () => {
    const photoType = photoActionSheet;
    setPhotoActionSheet(null);
    await waitForActionSheetDismiss();
    try {
      const options = photoType === 'profile' ? PROFILE_PHOTO_PICKER_OPTIONS : BACKGROUND_PHOTO_PICKER_OPTIONS;
      const image = await ImagePicker.openCamera(options);
      await uploadPhoto(photoType, image);
    } catch (error) {
      handlePickerError(error);
    }
  };

  const handleChooseFromLibrary = async () => {
    const photoType = photoActionSheet;
    setPhotoActionSheet(null);
    await waitForActionSheetDismiss();
    try {
      const options = photoType === 'profile' ? PROFILE_PHOTO_PICKER_OPTIONS : BACKGROUND_PHOTO_PICKER_OPTIONS;
      const image = await ImagePicker.openPicker(options);
      await uploadPhoto(photoType, image);
    } catch (error) {
      handlePickerError(error);
    }
  };

  const handleRemovePhoto = async () => {
    const photoType = photoActionSheet;
    setPhotoActionSheet(null);
    await waitForActionSheetDismiss();
    await removePhoto(photoType);
  };

  const applyFriendshipUpdate = useCallback((friendshipStatus, friendCountDelta = 0) => {
    setProfile((prev) => prev ? { ...prev, friendshipStatus, friendCount: Math.max(0, (prev.friendCount ?? 0) + friendCountDelta) } : prev);
  }, []);

  const handleRelationshipError = useCallback((error) => {
    if (error?.status === 429) {
      showToast('Too many friend requests. Please try again later.', 'error');
      return;
    }
    showToast('Something went wrong. Please try again.', 'error');
  }, []);

  const handleAccountRequired = useCallback(() => {
    showToast('Create an account to use friends', 'error');
    navigation.navigate('FinishAccount');
  }, [navigation]);

  const handleSendRequest = async () => {
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await sendFriendRequest({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'requested') applyFriendshipUpdate('requestSent');
      else if (data.status === 'accepted') applyFriendshipUpdate('friends', 1);
      else if (data.status === 'alreadyFriends') applyFriendshipUpdate('friends');
      else if (data.status === 'alreadyRequested') applyFriendshipUpdate('requestSent');
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleCancelRequest = async () => {
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await cancelFriendRequest({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'canceled') applyFriendshipUpdate('none');
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleAcceptRequest = async () => {
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await acceptFriendRequest({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'accepted') applyFriendshipUpdate('friends', 1);
      else if (data.status === 'alreadyFriends') applyFriendshipUpdate('friends');
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleDeclineRequest = async () => {
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await declineFriendRequest({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'declined') applyFriendshipUpdate('none');
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleConfirmUnfriend = async () => {
    setUnfriendModalVisible(false);
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await unfriend({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'unfriended') applyFriendshipUpdate('none', -1);
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleConfirmBlock = async () => {
    setBlockModalVisible(false);
    try {
      const token = await tokenStorage.getToken();
      if (!token || !profile) return;
      const data = await blockUser({ authToken: token, username: profile.username }).unwrap();
      if (data.status === 'blocked') {
        showToast('User blocked', 'success');
        navigation.goBack();
      }
      else if (data.status === 'accountRequired') handleAccountRequired();
      else fetchProfile();
    } catch (error) {
      handleRelationshipError(error);
    }
  };

  const handleFriendsPress = () => {
    if (isOwnProfile) {
      navigation.navigate('Friends');
    } else if (profile) {
      navigation.navigate('Friends', { username: profile.username, displayName: profile.displayName });
    }
  };

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomInsetHeight
  };

  if (isLoading) {
    return <View style={rootStyle} />;
  }

  if (hasError || !profile) {
    return (
      <View style={rootStyle}>
        <View style={styles.errorBackArrowContainer}>
          {!isTabInstance && (
            <TouchableOpacity
              style={styles.errorBackArrow}
              onPress={() => navigation.goBack()}
            >
              <BackArrow {...styles.iconsMatchingSize} />
            </TouchableOpacity>
          )}
        </View>
        <View style={styles.errorContent}>
          <CustomText style={styles.errorHeading}>Unable to load profile</CustomText>
          <CustomText style={styles.errorMessage}>Please check your connection and try again.</CustomText>
          <TouchableOpacity style={styles.retryBtn} onPress={fetchProfile}>
            <CustomText style={styles.retryBtnTxt}>Try Again</CustomText>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  const isTablet = styles.bioTxt.fontSize === scaleFont(18);
  const BIO_LIMIT = isTablet ? 501 : 313;
  const BIO_COLLAPSED_HEIGHT = isTablet ? scaleHeight(187.28) : scaleHeight(132);
  const BIO_EXPANDED_HEIGHT = isTablet ? scaleHeight(187.28) : scaleHeight(132);
  const hasBio = profile.bio && profile.bio.length > 0;
  const isLongBio = hasBio && profile.bio.length > BIO_LIMIT;
  const shownBio = hasBio ? (bioExpanded ? profile.bio : (isLongBio ? profile.bio.slice(0, BIO_LIMIT) : profile.bio)) : '';
  const friendshipStatus = profile.friendshipStatus;

  return (
    <View style={rootStyle}>
      <View style={styles.ProfileBg}>
        {profile.backgroundPhotoUrl ? (
          <RemoteImage
            uri={profile.backgroundPhotoUrl}
            fadeDuration={0}
            progressiveRenderingEnabled={false}
            style={styles.backgroundImage}
          />
        ) : (
          <LinearGradient
            colors={getBackgroundGradient(profile.avatarColor)}
            start={{ x: 0, y: 0 }}
            end={{ x: 1, y: 1 }}
            style={styles.backgroundImage}
          />
        )}
        {!isTabInstance && (
          <TouchableOpacity
            style={styles.BackArrow}
            onPress={() => navigation.goBack()}
          >
            <BackArrow {...styles.iconsMatchingSize} />
          </TouchableOpacity>
        )}
        {isOwnProfile && (
          <View style={styles.editAndLogout}>
            <TouchableOpacity
              style={styles.editProfile}
              onPress={() => navigation.navigate('EditProfile')}
            >
              <EditRedIcon {...styles.iconsMatchingSize} />
              <CustomText style={styles.editProfileTxt}>Edit Profile</CustomText>
            </TouchableOpacity>
            <TouchableOpacity
              style={styles.logout}
              onPress={logout}
            >
              <LogoutIcon {...styles.iconsMatchingSize} />
            </TouchableOpacity>
          </View>
        )}
        {!isOwnProfile && (
          <View style={styles.relationshipActions}>
            {friendshipStatus === 'none' && (
              <TouchableOpacity
                style={styles.relationshipAcceptBtn}
                onPress={handleSendRequest}
              >
                <CustomText style={styles.relationshipAcceptTxt}>Add Friend</CustomText>
              </TouchableOpacity>
            )}
            {friendshipStatus === 'requestSent' && (
              <TouchableOpacity
                style={styles.relationshipBtn}
                onPress={handleCancelRequest}
              >
                <CustomText style={styles.relationshipBtnTxt}>Requested</CustomText>
              </TouchableOpacity>
            )}
            {friendshipStatus === 'requestReceived' && (
              <>
                <TouchableOpacity
                  style={styles.relationshipAcceptBtn}
                  onPress={handleAcceptRequest}
                >
                  <CustomText style={styles.relationshipAcceptTxt}>Accept</CustomText>
                </TouchableOpacity>
                <TouchableOpacity
                  style={styles.relationshipCircleBtn}
                  onPress={handleDeclineRequest}
                >
                  <XIcon {...styles.iconsMatchingSize} />
                </TouchableOpacity>
              </>
            )}
            {friendshipStatus === 'friends' && (
              <TouchableOpacity
                style={styles.relationshipBtn}
                onPress={() => setUnfriendModalVisible(true)}
              >
                <CustomText style={styles.relationshipBtnTxt}>Friends</CustomText>
              </TouchableOpacity>
            )}
            <TouchableOpacity
              style={styles.relationshipCircleBtn}
              onPress={() => setBlockModalVisible(true)}
            >
              <EllipsisIcon {...styles.iconsMatchingSize} />
            </TouchableOpacity>
          </View>
        )}
        {isOwnProfile && (
          <TouchableOpacity
            style={styles.editBackground}
            onPress={() => setPhotoActionSheet('background')}
          >
            <EditRedIcon {...styles.iconsMatchingSize} />
          </TouchableOpacity>
        )}
        <View style={styles.profilePhotoContainer}>
          {profile.profilePhotoUrl ? (
            <RemoteImage
              uri={profile.profilePhotoUrl}
              fadeDuration={0}
              progressiveRenderingEnabled={false}
              style={styles.profilePhoto}
            />
          ) : (
            <View style={[styles.avatarCircle, { backgroundColor: profile.avatarColor }]}>
              <CustomText style={styles.avatarInitial}>
                {profile.displayName ? profile.displayName[0].toUpperCase() : profile.username ? profile.username[0].toUpperCase() : '?'}
              </CustomText>
            </View>
          )}
          {isOwnProfile && (
            <TouchableOpacity
              style={styles.whiteEditBackground}
              onPress={() => setPhotoActionSheet('profile')}
            >
              <EditWhiteIcon {...styles.whiteEditIcon} />
            </TouchableOpacity>
          )}
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
          {isOwnProfile && (
            <CustomText
              style={styles.nameTxt}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {profile.displayName}
            </CustomText>
          )}
          <TouchableOpacity
            style={styles.friends}
            onPress={handleFriendsPress}
          >
            <CustomText style={styles.friendsTxt}>{profile.friendCount ?? 0} Friends</CustomText>
          </TouchableOpacity>
        </View>
        {hasBio && (
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
        )}
        {isOwnProfile && (
          <View style={styles.information}>
            <CustomText style={styles.informationTxt}>Information</CustomText>
            <View style={styles.informationDetails}>
              <View style={!profile.phoneNumber ? styles.informationDetailMissing : styles.informationDetail}>
                <View style={styles.informationDetailsLabelAndEditRow}>
                  <View style={styles.informationDetailsLabelRow}>
                    <PhoneIcon {...styles.iconsMatchingSize} />
                    <CustomText style={styles.informationDetailsLabelTxt}>Mobile Number:</CustomText>
                  </View>
                  {profile.phoneNumber && (
                    <View>
                      <TouchableOpacity
                        onPress={() => navigation.navigate('EditEmailOrPhone', { source: 'phone', currentValue: profile.phoneNumber })}
                      >
                        <EditRedIcon {...styles.iconsMatchingSize} />
                      </TouchableOpacity>
                    </View>
                  )}
                </View>
                {!profile.phoneNumber ? (
                  <View>
                    <TouchableOpacity
                      style={styles.addInformationBtn}
                      onPress={() => navigation.navigate('AddNewEmailOrPhone', { source: 'phone' })}
                    >
                      <CustomText style={styles.addInformationTxt}>Add Mobile Number</CustomText>
                    </TouchableOpacity>
                  </View>
                ) : (
                  <View>
                    <CustomText
                      style={styles.informationDetailTxt}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      {formatPhoneNumber(profile.phoneNumber)}
                    </CustomText>
                  </View>
                )}
              </View>
              <View style={!profile.emailAddress ? styles.informationDetailMissing : styles.informationDetail}>
                <View style={styles.informationDetailsLabelAndEditRow}>
                  <View style={styles.informationDetailsLabelRow}>
                    <MailIcon {...styles.iconsMatchingSize} />
                    <CustomText style={styles.informationDetailsLabelTxt}>Email Address:</CustomText>
                  </View>
                  {profile.emailAddress && (
                    <View>
                      <TouchableOpacity
                        onPress={() => navigation.navigate('EditEmailOrPhone', { source: 'email', currentValue: profile.emailAddress })}
                      >
                        <EditRedIcon {...styles.iconsMatchingSize} />
                      </TouchableOpacity>
                    </View>
                  )}
                </View>
                {!profile.emailAddress ? (
                  <View>
                    <TouchableOpacity
                      style={styles.addInformationBtn}
                      onPress={() => navigation.navigate('AddNewEmailOrPhone', { source: 'email' })}
                    >
                      <CustomText style={styles.addInformationTxt}>Add Email Address</CustomText>
                    </TouchableOpacity>
                  </View>
                ) : (
                  <View>
                    <CustomText
                      style={styles.informationDetailTxt}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      {profile.emailAddress}
                    </CustomText>
                  </View>
                )}
              </View>
            </View>
          </View>
        )}
      </View>
      <PhotoActionSheet
        visible={photoActionSheet !== null}
        title={photoActionSheet === 'profile' ? 'Profile Photo' : 'Background Photo'}
        hasExistingPhoto={
          photoActionSheet === 'profile'
            ? !!profile.profilePhotoUrl
            : !!profile.backgroundPhotoUrl
        }
        onTakePhoto={handleTakePhoto}
        onChooseFromLibrary={handleChooseFromLibrary}
        onRemove={handleRemovePhoto}
        onCancel={() => setPhotoActionSheet(null)}
      />
      <UnfriendModal
        visible={unfriendModalVisible}
        username={profile.username}
        onConfirm={handleConfirmUnfriend}
        onCancel={() => setUnfriendModalVisible(false)}
      />
      <BlockUserModal
        visible={blockModalVisible}
        username={profile.username}
        onConfirm={handleConfirmBlock}
        onCancel={() => setBlockModalVisible(false)}
      />
    </View>
  );
}