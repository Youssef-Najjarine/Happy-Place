import React, { useState, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import DeleteAccountModal from 'src/components/DeleteAccountModal';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
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
    backgroundColor: '#F9F9F9'
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: Black
  },
  line: {
    marginVertical: scaleHeight(20),
    height: scaleHeight(1),
    width: '100%',
    backgroundColor: '#F9F9F9'
  },
  changePassword: {
    marginBottom: scaleHeight(16)
  },
  changePasswordTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    opacity: 0.6,
    fontWeight: 600,
    color: Black
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
    backgroundColor: '#F9F9F9'
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
    alignItems: 'center'
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
  namesInput: {
    width: scaleWidth(340)
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
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
    borderColor: '#F9F9F9',
    backgroundColor: 'rgba(249, 249, 249, 0.30)',
    color: Black
  },
  line: {
    marginVertical: scaleHeight(20),
    height: scaleHeight(1.341),
    width: '100%',
    backgroundColor: '#F9F9F9'
  },
  changePassword: {
    marginBottom: scaleHeight(16)
  },
  changePasswordTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    opacity: 0.6,
    fontWeight: 600,
    color: Black
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
    scaleLetterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  deleteAccountWarning: {
    marginBottom: scaleHeight(21.46)
  },
  deleteAccountWarningTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    scaleLetterSpacing: scaleLetterSpacing(-0.18),
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
  }
});

export default function EditProfile() {
  console.log("TESTING MODal: ", DeleteAccountModal);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [username, setUsername] = useState('');
  const [name, setName] = useState('');
  const [bio, setBio] = useState('');
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  function saveEditProfile() {
     navigation.navigate('Profile');
  }
  function handleDelete() {
    setShowDeleteModal(false);
    navigation.navigate('Home');
  }
  useFocusEffect(
    useCallback(() => {
        setName('');
        setCurrentPassword('');
        setNewPassword('');
        setConfirmNewPassword('');
    }, [])
);
  const hasMinLen = (v) => v.length >= 8;
  const hasNumber = (v) => /\d/.test(v);
  const hasLowerUpper = (v) => /[a-z]/.test(v) && /[A-Z]/.test(v);
  const rules = useMemo(() => ({
    minLen: hasMinLen(newPassword),
    number: hasNumber(newPassword),
    lowerUpper: hasLowerUpper(newPassword),
    match: newPassword.length > 0 && newPassword === confirmNewPassword,
  }), [newPassword, confirmNewPassword]);

  const nameValid = name.trim().length > 0;
  const usernameValid = username.trim().length > 0;

  const canSubmit = nameValid && usernameValid &&
    currentPassword.length > 0 && newPassword.length > 0 && confirmNewPassword.length > 0 &&
    rules.minLen && rules.number && 
    rules.lowerUpper && rules.match;

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
      >
        <View style={styles.part1}>
          <View style={styles.header}>
            <View style={styles.backArrowAndEditTxt}>
              <View>
                <TouchableOpacity 
                  style={styles.BackArrow}
                  onPress={() => navigation.goBack()}
                >
                  <BackArrow {...styles.backArrowIcon}/>
                </TouchableOpacity>
              </View>
              <View>
                <CustomText style={styles.editProfileTxt}>Edit Profile</CustomText>            
              </View>
            </View>
            <View style={styles.save}>
              <TouchableOpacity 
                style={[styles.saveBtn, !canSubmit && { opacity: 0.5 }]}
                disabled={!canSubmit}
                onPress={() => { saveEditProfile();}}
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
              <View>
                <CustomText style={styles.textBoxLabel}>Username</CustomText>
                <View>
                  <CustomTextInput
                    style={[styles.input, styles.namesInput]}
                    keyboardType="default"
                    autoCapitalize="words"   
                    autoCorrect={false}
                    textContentType="name"
                    autoComplete="name"  
                    value={username}
                    onChangeText={setUsername}
                  />
                  <UsernameIcon {...styles.textBoxIcon}/>
                </View>
              </View>
              <View>
                <CustomText style={styles.textBoxLabel}>Full Name</CustomText>
                <View>
                  <CustomTextInput
                    style={[styles.input, styles.namesInput]}
                    keyboardType="default"
                    autoCapitalize="words"   
                    autoCorrect={false}
                    textContentType="name"
                    autoComplete="name"  
                    value={name}
                    onChangeText={setName}
                  />
                  <ProfileIcon {...styles.textBoxIcon}/>
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
          <View style={styles.line}/>
          <View style={styles.changePassword}>
            <CustomText style={styles.changePasswordTxt}>
              Change Password
            </CustomText>
          </View>
          <View style={styles.inputCredentials}>
            <View style={styles.passwordView}>
              <CustomText style={styles.textBoxLabel}>Current Password</CustomText>
              <View>
                <CustomTextInput
                    style={[styles.input, styles.largeRightPadding]}
                    secureTextEntry={!showCurrentPassword}
                    value={currentPassword}
                    onChangeText={setCurrentPassword}
                    textContentType="password"
                    autoComplete="password"
                />
                <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowCurrentPassword(!showCurrentPassword)}>
                {showCurrentPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                </TouchableOpacity>
                <KeyIcon {...styles.textBoxIcon}/>
              </View>
            </View>          
            <View style={styles.passwordView}>
              <CustomText style={styles.textBoxLabel}>New Password</CustomText>
              <View>
                <CustomTextInput
                    style={[styles.input, styles.largeRightPadding]}
                    secureTextEntry={!showNewPassword}
                    value={newPassword}
                    onChangeText={setNewPassword}
                    textContentType="newPassword"
                    autoComplete="password-new"
                />
                <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowNewPassword(!showNewPassword)}>
                {showNewPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                </TouchableOpacity>
                <KeyIcon {...styles.textBoxIcon}/>
              </View>
            </View>
            <View style={styles.passwordView}>
              <CustomText style={styles.textBoxLabel}>Confirm New Password</CustomText>
              <View>
                  <CustomTextInput
                      style={[styles.input, styles.largeRightPadding]}
                      secureTextEntry={!showConfirmPassword}
                      value={confirmNewPassword}
                      onChangeText={setConfirmNewPassword}
                      textContentType="newPassword"
                      autoComplete="password-new"
                  />
                  <TouchableOpacity style={styles.eyeIcons} onPress={() => setShowConfirmPassword(!showConfirmPassword)}>
                  {showConfirmPassword ? <EyeSlashIcon {...styles.eyeIcon} /> : <EyeIcon {...styles.eyeIcon} />}
                  </TouchableOpacity>
                  <KeyIcon {...styles.textBoxIcon}/>
              </View>
            </View>
            <View style={styles.passwordRequirementsView}>
                {[
                    { ok: rules.minLen, text: 'Minimum 8 characters' },
                    { ok: rules.number, text: 'At least 1 number (0–9)' },
                    { ok: rules.lowerUpper, text: 'At least 1 lowercase and 1 uppercase letter' },
                    { ok: rules.match, text: 'Passwords matching' },
                ].map((r, i) => (
                    <View key={i} style={styles.passwordRequirements}>
                    {r.ok ? <GreenCheckIcon {...styles.passwordCheckIcons}/> : <RedXIcon {...styles.passwordCheckIcons}/>}
                    <CustomText style={[styles.passwordRequirementTxt, { opacity: r.ok ? 1 : 0.7 }]}>
                        {r.text}
                    </CustomText>
                    </View>
                ))}
            </View>          
          </View>
        </View>
        <View style={styles.line}/>
        <View style={styles.deleteAccount}>
          <CustomText style={styles.deleteAccountTxt}>
            Delete Account
          </CustomText>
        </View>
        <View style={styles.deleteAccountWarning}>
          <CustomText style={styles.deleteAccountWarningTxt}>
            This action is permanent and cannot be undone. Once your account is deleted, 
            you will no longer be able to access it. Please ensure you have saved any 
            important information before proceeding.
          </CustomText>
        </View>
        <View style={styles.part2}>
          <View>
            <TouchableOpacity
                style={styles.deleteAccountBtn}
                onPress={() => setShowDeleteModal(true)}
            >
                <CustomText style={styles.deleteAccountBtnText}>Delete Account</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </ScrollView>
      <DeleteAccountModal 
        visible={showDeleteModal} 
        onConfirm={handleDelete} 
        onCancel={() => setShowDeleteModal(false)} 
      />
    </>
  );
}