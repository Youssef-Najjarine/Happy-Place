import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet, Animated, Keyboard } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import DeleteAccountModal from 'src/components/DeleteAccountModal';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { setUser, clearUser } from 'store/userSlice';
import { HappyColor, White, Black, VeryLightGray, FrostedWhite, Charcoal } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import ProfileIcon from 'assets/images/createAccount/profile-icon.svg';
import UsernameIcon from 'assets/images/editProfile/username-icon.svg';
import GreenCheckIcon from 'assets/images/global/green-check-icon.svg';
import RedXIcon from 'assets/images/global/red-x-icon.svg';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import KeyIcon from 'assets/images/global/key-icon.svg';
import EyeIcon from 'assets/images/global/eye-icon.svg';
import EyeSlashIcon from 'assets/images/global/eye-slash-icon.svg';
import tokenStorage from 'services/tokenStorage';
import profileService from 'services/profileService';

const TOAST_DISPLAY_DURATION = 4000;
const USERNAME_DEBOUNCE_MS = 500;
const AVAILABLE_GREEN = '#00B894';

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: White,
    flex: 1,
    width: '100%'
  },
  contentContainer: {
    paddingTop: scaleHeight(12),
    paddingBottom: scaleHeight(22),
    paddingHorizontal: scaleWidth(20)
  },
  header: {
    marginBottom: scaleHeight(20),
    height: scaleHeight(42),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  backArrowAndEditTxt: {
    gap: scaleWidth(12),
    height: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  BackArrow: {
    width: scaleWidth(42),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  editProfileTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
  },
  save: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(68),
    height: '100%'
  },
  saveBtn: {
    borderRadius: scaleWidth(99),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  saveTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  generalInfo: {
    marginBottom: scaleHeight(16)
  },
  generalInfoTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  inputCredentials: {
    gap: scaleHeight(12)
  },
  namesView: {
    gap: scaleHeight(12)
  },
  textBoxLabel: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.28),
    marginBottom: scaleHeight(4),
    fontWeight: 600,
    color: Black
  },
  textBoxIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    top: scaleHeight(12),
    left: scaleWidth(16),
    position: 'absolute',
  },
  input: {
    height: scaleHeight(48),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(67.067),
    paddingLeft: scaleWidth(48),
    paddingVertical: scaleHeight(12),
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  bioTextArea: {
    height: scaleHeight(255),
    paddingVertical: scaleHeight(12),
    paddingHorizontal: scaleWidth(16),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(24),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  line: {
    marginVertical: scaleHeight(20),
    height: scaleHeight(1),
    width: '100%',
    backgroundColor: VeryLightGray
  },
  changePasswordHeader: {
    marginBottom: scaleHeight(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  changePasswordTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    opacity: 0.6,
    fontWeight: 600,
    color: Black
  },
  savePasswordBtn: {
    width: scaleWidth(120),
    height: scaleHeight(36),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  savePasswordTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  largeRightPadding: {
    paddingRight: scaleWidth(48)
  },
  eyeIcons: {
    top: scaleHeight(12),
    right: scaleWidth(16),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24)
  },
  criteriaView: {
    marginTop: scaleHeight(8),
    gap: scaleHeight(6)
  },
  criteriaRow: {
    gap: scaleWidth(10),
    flexDirection: 'row',
    alignItems: 'center',
  },
  criteriaIcons: {
    width: scaleWidth(16),
    height: scaleHeight(16)
  },
  criteriaTxt: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(20),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  availabilityTxt: {
    marginTop: scaleHeight(4),
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(20),
    fontWeight: 600
  },
  verificationRow: {
    marginTop: scaleHeight(6),
    gap: scaleWidth(10),
    flexDirection: 'row',
    alignItems: 'center'
  },
  verificationTxt: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(20),
    fontWeight: 600
  },
  passwordRequirementsView: {
    gap: scaleHeight(8)
  },
  passwordRequirements: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center',
  },
  passwordCheckIcons: {
    width: scaleWidth(16),
    height: scaleHeight(16)
  },
  passwordRequirementTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(22),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  deleteAccount: {
    marginBottom: scaleHeight(8)
  },
  deleteAccountTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  deleteAccountWarning: {
    marginBottom: scaleHeight(16)
  },
  deleteAccountWarningTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black
  },
  deleteAccountBtn: {
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  deleteAccountBtnText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  toastContainer: {
    position: 'absolute',
    left: scaleWidth(20),
    right: scaleWidth(20),
    zIndex: 100
  },
  toast: {
    borderRadius: scaleWidth(12),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(12),
    backgroundColor: HappyColor,
    shadowColor: Black,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 6,
    elevation: 6
  },
  toastText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(20),
    fontWeight: 600,
    color: White,
    textAlign: 'center'
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: White,
    flex: 1,
    width: '100%'
  },
  contentContainer: {
    paddingTop: scaleHeight(16.1),
    paddingBottom: scaleHeight(24),
    paddingHorizontal: scaleWidth(24)
  },
  header: {
    marginBottom: scaleHeight(20),
    height: scaleHeight(56.34),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  backArrowAndEditTxt: {
    gap: scaleWidth(16.1),
    height: '100%',
    flexDirection: 'row',
    alignItems: 'center'
  },
  BackArrow: {
    width: 68.42,
    borderRadius: scaleWidth(132.792),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557),
  },
  editProfileTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  save: {
    borderRadius: scaleWidth(132.792),
    width: scaleWidth(93),
    height: '100%'
  },
  saveBtn: {
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  saveTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  generalInfo: {
    marginBottom: scaleHeight(16)
  },
  generalInfoTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  inputCredentials: {
    gap: scaleHeight(16)
  },
  namesView: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start'
  },
  namesInput: {
    width: scaleWidth(340)
  },
  textBoxLabel: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.36),
    marginBottom: scaleHeight(6),
    fontWeight: 600,
    color: Black
  },
  textBoxIcon: {
    width: scaleWidth(32.192),
    height: scaleHeight(32.192),
    top: scaleHeight(16.1),
    left: scaleWidth(21.46),
    position: 'absolute',
  },
  input: {
    height: scaleHeight(64.382),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(89.959),
    paddingLeft: scaleWidth(64.38),
    paddingVertical: scaleHeight(16.1),
    paddingRight: scaleWidth(21.46),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  bioTextArea: {
    height: scaleHeight(211.192),
    paddingVertical: scaleHeight(16.1),
    paddingHorizontal: scaleWidth(21.46),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(32.192),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    borderColor: VeryLightGray,
    backgroundColor: FrostedWhite,
    color: Black
  },
  line: {
    marginVertical: scaleHeight(20),
    height: scaleHeight(1.341),
    width: '100%',
    backgroundColor: VeryLightGray
  },
  changePasswordHeader: {
    marginBottom: scaleHeight(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  changePasswordTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    opacity: 0.6,
    fontWeight: 600,
    color: Black
  },
  savePasswordBtn: {
    width: scaleWidth(160),
    height: scaleHeight(48),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  savePasswordTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  largeRightPadding: {
    paddingRight: scaleWidth(64.382)
  },
  eyeIcons: {
    top: scaleHeight(16.1),
    right: scaleWidth(21.46),
    position: 'absolute'
  },
  eyeIcon: {
    width: scaleWidth(32.192),
    height: scaleHeight(32.192)
  },
  criteriaView: {
    marginTop: scaleHeight(10.73),
    gap: scaleHeight(8)
  },
  criteriaRow: {
    gap: scaleWidth(13.41),
    flexDirection: 'row',
    alignItems: 'center',
  },
  criteriaIcons: {
    width: scaleWidth(21.461),
    height: scaleHeight(21.461)
  },
  criteriaTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  availabilityTxt: {
    marginTop: scaleHeight(5.37),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600
  },
  verificationRow: {
    marginTop: scaleHeight(8),
    gap: scaleWidth(13.41),
    flexDirection: 'row',
    alignItems: 'center'
  },
  verificationTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600
  },
  passwordRequirementsView: {
    gap: scaleHeight(12)
  },
  passwordRequirements: {
    gap: scaleWidth(16.1),
    flexDirection: 'row',
    alignItems: 'center',
  },
  passwordCheckIcons: {
    width: scaleWidth(21.461),
    height: scaleHeight(21.461)
  },
  passwordRequirementTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(29.509),
    opacity: 0.7,
    fontWeight: 400,
    color: Black
  },
  deleteAccount: {
    marginBottom: scaleHeight(12)
  },
  deleteAccountTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  deleteAccountWarning: {
    marginBottom: scaleHeight(21.46)
  },
  deleteAccountWarningTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  deleteAccountBtn: {
    width: scaleWidth(183),
    height: scaleHeight(53.827),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  deleteAccountBtnText: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  toastContainer: {
    position: 'absolute',
    left: scaleWidth(24),
    right: scaleWidth(24),
    zIndex: 100
  },
  toast: {
    borderRadius: scaleWidth(16),
    paddingHorizontal: scaleWidth(20),
    paddingVertical: scaleHeight(16),
    backgroundColor: HappyColor,
    shadowColor: Black,
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
    elevation: 8
  },
  toastText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600,
    color: White,
    textAlign: 'center'
  }
});

export default function EditProfile() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const dispatch = useDispatch();

  const [username, setUsername] = useState('');
  const [name, setName] = useState('');
  const [bio, setBio] = useState('');
  const [originalUsername, setOriginalUsername] = useState('');
  const [originalName, setOriginalName] = useState('');
  const [originalBio, setOriginalBio] = useState('');

  const [isUsernameFocused, setIsUsernameFocused] = useState(false);
  const [usernameModified, setUsernameModified] = useState(false);
  const [usernameAvailable, setUsernameAvailable] = useState(null);
  const [isCheckingUsername, setIsCheckingUsername] = useState(false);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [currentPasswordVerified, setCurrentPasswordVerified] = useState(null);

  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [toastMessage, setToastMessage] = useState(null);
  const toastOpacity = useRef(new Animated.Value(0)).current;
  const toastTranslateY = useRef(new Animated.Value(-20)).current;
  const toastTimerRef = useRef(null);
  const usernameDebounceRef = useRef(null);

  const normalizedUsername = username.trim().toLowerCase();

  const usernameRules = useMemo(() => ({
    minLen: normalizedUsername.length >= 5,
    maxLen: normalizedUsername.length <= 20,
    alphanumeric: normalizedUsername.length > 0 && /^[a-z0-9]+$/.test(normalizedUsername),
    hasNumber: /\d/.test(normalizedUsername),
  }), [normalizedUsername]);

  const allUsernameRulesPass = usernameRules.minLen && usernameRules.maxLen && usernameRules.alphanumeric && usernameRules.hasNumber;
  const showUsernameCriteria = isUsernameFocused || (usernameModified && !allUsernameRulesPass);

  const passwordRules = useMemo(() => ({
    minLen: newPassword.length >= 8,
    number: /\d/.test(newPassword),
    lowerUpper: /[a-z]/.test(newPassword) && /[A-Z]/.test(newPassword),
    specialChar: /[^a-zA-Z0-9\s]/.test(newPassword),
    match: newPassword.length > 0 && newPassword === confirmNewPassword,
  }), [newPassword, confirmNewPassword]);

  const allPasswordRulesPass = passwordRules.minLen && passwordRules.number && passwordRules.lowerUpper && passwordRules.specialChar && passwordRules.match;

  const profileChanged = normalizedUsername !== originalUsername || name.trim() !== originalName || (bio || '') !== (originalBio || '');
  const canSaveProfile = profileChanged && allUsernameRulesPass && (usernameAvailable === true || normalizedUsername === originalUsername) && name.trim().length > 0;
  const canSavePassword = currentPasswordVerified === true && allPasswordRulesPass;

  useFocusEffect(
    useCallback(() => {
      setCurrentPassword('');
      setNewPassword('');
      setConfirmNewPassword('');
      setCurrentPasswordVerified(null);
      setUsernameModified(false);
      setUsernameAvailable(null);
      setToastMessage(null);
      toastOpacity.setValue(0);
      toastTranslateY.setValue(-20);
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);

      const fetchProfile = async () => {
        dispatch(showLoading());
        try {
          const token = await tokenStorage.getToken();
          if (!token) return;
          const response = await profileService.getMyProfile(token);
          if (response.ok) {
            const data = await response.json();
            setUsername(data.username || '');
            setName(data.displayName || '');
            setBio(data.bio || '');
            setOriginalUsername(data.username || '');
            setOriginalName(data.displayName || '');
            setOriginalBio(data.bio || '');
          }
        } catch {
        } finally {
          dispatch(hideLoading());
        }
      };
      fetchProfile();
    }, [dispatch])
  );

  useEffect(() => {
    if (!allUsernameRulesPass || normalizedUsername === originalUsername) {
      setUsernameAvailable(normalizedUsername === originalUsername ? true : null);
      setIsCheckingUsername(false);
      if (usernameDebounceRef.current) clearTimeout(usernameDebounceRef.current);
      return;
    }
    setIsCheckingUsername(true);
    setUsernameAvailable(null);
    if (usernameDebounceRef.current) clearTimeout(usernameDebounceRef.current);
    usernameDebounceRef.current = setTimeout(async () => {
      try {
        const token = await tokenStorage.getToken();
        if (!token) return;
        const response = await profileService.checkUsernameAvailability(token, normalizedUsername);
        if (response.ok) {
          const data = await response.json();
          setUsernameAvailable(data.isAvailable);
        }
      } catch {
      }
      setIsCheckingUsername(false);
    }, USERNAME_DEBOUNCE_MS);
    return () => {
      if (usernameDebounceRef.current) clearTimeout(usernameDebounceRef.current);
    };
  }, [normalizedUsername, originalUsername, allUsernameRulesPass]);

  useEffect(() => {
    return () => {
      if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
      if (usernameDebounceRef.current) clearTimeout(usernameDebounceRef.current);
    };
  }, []);

  const handleUsernameChange = (value) => {
    setUsername(value);
    if (!usernameModified) setUsernameModified(true);
  };

  const handleCurrentPasswordBlur = async () => {
    if (!currentPassword.trim()) {
      setCurrentPasswordVerified(null);
      return;
    }
    try {
      const token = await tokenStorage.getToken();
      if (!token) return;
      const response = await profileService.verifyCurrentPassword(token, currentPassword);
      if (response.ok) {
        const data = await response.json();
        setCurrentPasswordVerified(data.isValid);
      }
    } catch {
    }
  };

  const showToast = (message) => {
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    setToastMessage(message);
    toastOpacity.setValue(0);
    toastTranslateY.setValue(-20);
    Animated.parallel([
      Animated.timing(toastOpacity, { toValue: 1, duration: 250, useNativeDriver: true }),
      Animated.timing(toastTranslateY, { toValue: 0, duration: 250, useNativeDriver: true })
    ]).start();
    toastTimerRef.current = setTimeout(() => {
      Animated.parallel([
        Animated.timing(toastOpacity, { toValue: 0, duration: 200, useNativeDriver: true }),
        Animated.timing(toastTranslateY, { toValue: -20, duration: 200, useNativeDriver: true })
      ]).start(() => setToastMessage(null));
    }, TOAST_DISPLAY_DURATION);
  };

  const dismissToast = () => {
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    Animated.parallel([
      Animated.timing(toastOpacity, { toValue: 0, duration: 200, useNativeDriver: true }),
      Animated.timing(toastTranslateY, { toValue: -20, duration: 200, useNativeDriver: true })
    ]).start(() => setToastMessage(null));
  };

  const handleSaveProfile = async () => {
    Keyboard.dismiss();
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) return;
      const response = await profileService.updateProfile(token, username, name.trim(), bio);
      if (response.ok) {
        const updatedProfile = await response.json();
        dispatch(setUser({
          displayName: updatedProfile.displayName,
          username: updatedProfile.username,
          avatarColor: updatedProfile.avatarColor,
          profilePhotoUrl: updatedProfile.profilePhotoUrl,
        }));
        setOriginalUsername(updatedProfile.username);
        setOriginalName(updatedProfile.displayName);
        setOriginalBio(updatedProfile.bio || '');
        setUsername(updatedProfile.username);
        setName(updatedProfile.displayName);
        setBio(updatedProfile.bio || '');
        setUsernameModified(false);
        showToast('Profile updated successfully.');
      } else {
        showToast('Unable to update profile. Please try again.');
      }
    } catch {
      showToast('Something went wrong. Please try again.');
    } finally {
      dispatch(hideLoading());
    }
  };

  const handleSavePassword = async () => {
    Keyboard.dismiss();
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) return;
      const response = await profileService.changePassword(token, currentPassword, newPassword);
      if (response.ok) {
        setCurrentPassword('');
        setNewPassword('');
        setConfirmNewPassword('');
        setCurrentPasswordVerified(null);
        showToast('Password changed successfully.');
      } else {
        showToast('Unable to change password. Please try again.');
      }
    } catch {
      showToast('Something went wrong. Please try again.');
    } finally {
      dispatch(hideLoading());
    }
  };

  const handleDeleteAccount = async (password) => {
    setShowDeleteModal(false);
    dispatch(showLoading());
    try {
      const token = await tokenStorage.getToken();
      if (!token) return;
      const response = await profileService.deleteAccount(token, password);
      if (response.ok) {
        dispatch(clearUser());
        await tokenStorage.clearToken();
        navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
        return;
      }
      showToast('Incorrect password. Account was not deleted.');
    } catch {
      showToast('Something went wrong. Please try again.');
    } finally {
      dispatch(hideLoading());
    }
  };

  const contentContainer = {
    ...styles.contentContainer,
    paddingTop: styles.contentContainer.paddingTop + statusBarHeight,
    paddingBottom: bottomSafeHeight + styles.contentContainer.paddingBottom
  };

  return (
    <>
      <ScrollView
        style={styles.root}
        contentContainerStyle={contentContainer}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
      >
        <View>
          <View style={styles.header}>
            <View style={styles.backArrowAndEditTxt}>
              <TouchableOpacity
                style={styles.BackArrow}
                onPress={() => navigation.goBack()}
              >
                <BackArrow {...styles.backArrowIcon} />
              </TouchableOpacity>
              <CustomText style={styles.editProfileTxt}>Edit Profile</CustomText>
            </View>
            <View style={styles.save}>
              <TouchableOpacity
                style={[styles.saveBtn, !canSaveProfile && { opacity: 0.5 }]}
                disabled={!canSaveProfile}
                onPress={handleSaveProfile}
              >
                <CustomText style={styles.saveTxt}>Save</CustomText>
              </TouchableOpacity>
            </View>
          </View>
          <View style={styles.generalInfo}>
            <CustomText style={styles.generalInfoTxt}>General Information</CustomText>
          </View>
          <View style={styles.inputCredentials}>
            <View style={styles.namesView}>
              <View style={styles.namesInput}>
                <CustomText style={styles.textBoxLabel}>Username</CustomText>
                <View>
                  <CustomTextInput
                    style={styles.input}
                    keyboardType="default"
                    autoCapitalize="none"
                    autoCorrect={false}
                    value={username}
                    onChangeText={handleUsernameChange}
                    onFocus={() => setIsUsernameFocused(true)}
                    onBlur={() => setIsUsernameFocused(false)}
                  />
                  <UsernameIcon {...styles.textBoxIcon} />
                </View>
                {showUsernameCriteria && (
                  <View style={styles.criteriaView}>
                    {[
                      { ok: usernameRules.minLen, text: 'At least 5 characters' },
                      { ok: usernameRules.maxLen, text: 'No more than 20 characters' },
                      { ok: usernameRules.alphanumeric, text: 'Letters and numbers only' },
                      { ok: usernameRules.hasNumber, text: 'Must contain at least 1 number' },
                    ].map((r, i) => (
                      <View key={i} style={styles.criteriaRow}>
                        {r.ok ? <GreenCheckIcon {...styles.criteriaIcons} /> : <RedXIcon {...styles.criteriaIcons} />}
                        <CustomText style={[styles.criteriaTxt, { opacity: r.ok ? 1 : 0.7 }]}>
                          {r.text}
                        </CustomText>
                      </View>
                    ))}
                  </View>
                )}
                {allUsernameRulesPass && normalizedUsername !== originalUsername && !isCheckingUsername && usernameAvailable !== null && (
                  <CustomText style={[styles.availabilityTxt, { color: usernameAvailable ? AVAILABLE_GREEN : HappyColor }]}>
                    {usernameAvailable ? 'Username available' : 'Username is taken'}
                  </CustomText>
                )}
                {isCheckingUsername && allUsernameRulesPass && normalizedUsername !== originalUsername && (
                  <CustomText style={[styles.availabilityTxt, { color: Charcoal }]}>
                    Checking availability...
                  </CustomText>
                )}
              </View>
              <View style={styles.namesInput}>
                <CustomText style={styles.textBoxLabel}>Name</CustomText>
                <View>
                  <CustomTextInput
                    style={styles.input}
                    keyboardType="default"
                    autoCapitalize="words"
                    autoCorrect={false}
                    textContentType="name"
                    autoComplete="name"
                    value={name}
                    onChangeText={setName}
                  />
                  <ProfileIcon {...styles.textBoxIcon} />
                </View>
              </View>
            </View>
            <View>
              <CustomText style={styles.textBoxLabel}>Bio</CustomText>
              <View>
                <CustomTextInput
                  style={styles.bioTextArea}
                  keyboardType="default"
                  autoCapitalize="sentences"
                  autoCorrect={false}
                  value={bio}
                  onChangeText={setBio}
                  multiline={true}
                />
              </View>
            </View>
          </View>
          <View style={styles.line} />
          <View style={styles.changePasswordHeader}>
            <CustomText style={styles.changePasswordTxt}>Change Password</CustomText>
            <TouchableOpacity
              style={[styles.savePasswordBtn, !canSavePassword && { opacity: 0.5 }]}
              disabled={!canSavePassword}
              onPress={handleSavePassword}
            >
              <CustomText style={styles.savePasswordTxt}>Save Password</CustomText>
            </TouchableOpacity>
          </View>
          <View style={styles.inputCredentials}>
            <View>
              <CustomText style={styles.textBoxLabel}>Current Password</CustomText>
              <View>
                <CustomTextInput
                  style={[styles.input, styles.largeRightPadding]}
                  secureTextEntry={!showCurrentPassword}
                  value={currentPassword}
                  onChangeText={(value) => { setCurrentPassword(value); setCurrentPasswordVerified(null); }}
                  onBlur={handleCurrentPasswordBlur}
                  textContentType="oneTimeCode"
                  autoComplete="off"
                />
                <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowCurrentPassword(!showCurrentPassword)}>
                  {showCurrentPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                </TouchableOpacity>
                <KeyIcon {...styles.textBoxIcon} />
              </View>
              {currentPasswordVerified !== null && (
                <View style={styles.verificationRow}>
                  {currentPasswordVerified ? <GreenCheckIcon {...styles.criteriaIcons} /> : <RedXIcon {...styles.criteriaIcons} />}
                  <CustomText style={[styles.verificationTxt, { color: currentPasswordVerified ? AVAILABLE_GREEN : HappyColor }]}>
                    {currentPasswordVerified ? 'Password verified' : 'Incorrect password'}
                  </CustomText>
                </View>
              )}
            </View>
            <View>
              <CustomText style={styles.textBoxLabel}>New Password</CustomText>
              <View>
                <CustomTextInput
                  style={[styles.input, styles.largeRightPadding]}
                  secureTextEntry={!showNewPassword}
                  value={newPassword}
                  onChangeText={setNewPassword}
                  textContentType="oneTimeCode"
                  autoComplete="off"
                />
                <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowNewPassword(!showNewPassword)}>
                  {showNewPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                </TouchableOpacity>
                <KeyIcon {...styles.textBoxIcon} />
              </View>
            </View>
            <View>
              <CustomText style={styles.textBoxLabel}>Confirm New Password</CustomText>
              <View>
                <CustomTextInput
                  style={[styles.input, styles.largeRightPadding]}
                  secureTextEntry={!showConfirmPassword}
                  value={confirmNewPassword}
                  onChangeText={setConfirmNewPassword}
                  textContentType="oneTimeCode"
                  autoComplete="off"
                />
                <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowConfirmPassword(!showConfirmPassword)}>
                  {showConfirmPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                </TouchableOpacity>
                <KeyIcon {...styles.textBoxIcon} />
              </View>
            </View>
            <View style={styles.passwordRequirementsView}>
              {[
                { ok: passwordRules.minLen, text: 'Minimum 8 characters' },
                { ok: passwordRules.number, text: 'At least 1 number (0-9)' },
                { ok: passwordRules.lowerUpper, text: 'At least 1 lowercase and 1 uppercase letter' },
                { ok: passwordRules.specialChar, text: 'At least 1 special character' },
                { ok: passwordRules.match, text: 'Passwords matching' },
              ].map((r, i) => (
                <View key={i} style={styles.passwordRequirements}>
                  {r.ok ? <GreenCheckIcon {...styles.passwordCheckIcons} /> : <RedXIcon {...styles.passwordCheckIcons} />}
                  <CustomText style={[styles.passwordRequirementTxt, { opacity: r.ok ? 1 : 0.7 }]}>
                    {r.text}
                  </CustomText>
                </View>
              ))}
            </View>
          </View>
        </View>
        <View style={styles.line} />
        <View style={styles.deleteAccount}>
          <CustomText style={styles.deleteAccountTxt}>Delete Account</CustomText>
        </View>
        <View style={styles.deleteAccountWarning}>
          <CustomText style={styles.deleteAccountWarningTxt}>
            This action is permanent and cannot be undone. Once your account is deleted, 
            you will no longer be able to access it. Please ensure you have saved any 
            important information before proceeding.
          </CustomText>
        </View>
        <View>
          <TouchableOpacity
            style={styles.deleteAccountBtn}
            onPress={() => setShowDeleteModal(true)}
          >
            <CustomText style={styles.deleteAccountBtnText}>Delete Account</CustomText>
          </TouchableOpacity>
        </View>
      </ScrollView>
      <DeleteAccountModal
        visible={showDeleteModal}
        onConfirm={handleDeleteAccount}
        onCancel={() => setShowDeleteModal(false)}
      />
      {toastMessage && (
        <Animated.View
          style={[
            styles.toastContainer,
            { top: statusBarHeight + scaleHeight(12), opacity: toastOpacity, transform: [{ translateY: toastTranslateY }] }
          ]}
        >
          <TouchableOpacity style={styles.toast} activeOpacity={0.9} onPress={dismissToast}>
            <CustomText style={styles.toastText}>{toastMessage}</CustomText>
          </TouchableOpacity>
        </Animated.View>
      )}
    </>
  );
}