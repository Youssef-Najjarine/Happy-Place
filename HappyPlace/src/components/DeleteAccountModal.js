import React from 'react';
import { Modal, View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import DeleteProfileIcon from 'assets/images/modals/delete-profile-modal-icon.svg';
const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(35, 35, 35, 0.30)',
  },
  modalContainer: {
    gap: scaleHeight(16),
    width: scaleWidth(335),
    paddingTop: scaleHeight(24),
    paddingBottom: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowColor: 'rgba(83, 26, 255, 0.10)',
    shadowOffset: { width: scaleWidth(8), height: scaleHeight(8) },
    shadowOpacity: 1,
    shadowRadius: scaleWidth(30),
    elevation: 8, 
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: White,
  },
  logoAndBodyText: {
    gap: scaleHeight(12),
    alignItems: 'center'
  },
  deleteIcon: {
    width: scaleWidth(60),
    height: scaleHeight(60)
  },
  headerTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  messageTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    opacity: 0.7,
    textAlign: 'center',
    color: Black,
  },
  buttonRow: {
    height: scaleHeight(48),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  cancelBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(145.5),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  cancelTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
  },
  deleteBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(145.5),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  deleteTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White,
  }
});
const tabletStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(35, 35, 35, 0.30)',
  },
  modalContainer: {
    gap: scaleHeight(24),
    width: scaleWidth(449.347),
    paddingTop: scaleHeight(32),
    paddingBottom: scaleHeight(24),
    paddingHorizontal: scaleWidth(24),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: 'rgb(83, 26, 255)',
    shadowOffset: { width: scaleWidth(10.731), height: scaleHeight(10.731) },
    shadowRadius: scaleWidth(40.24),
    shadowOpacity: 0.10,
    elevation: 11, 
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: White,
  },
  logoAndBodyText: {
    gap: scaleHeight(16.1),
    alignItems: 'center'
  },
  deleteIcon: {
    width: scaleWidth(80.48),
    height: scaleHeight(80.48)
  },
  headerTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(26.4),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black,
  },
  messageTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    opacity: 0.7,
    textAlign: 'center',
    color: Black,
  },
  buttonRow: {
    height: scaleHeight(62.192),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  cancelBtn: {
    borderRadius: scaleWidth(132.792),
    width: scaleWidth(192.625),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  cancelTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  deleteBtn: {
    borderRadius: scaleWidth(132.792),
    width: scaleWidth(192.625),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  deleteTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White,
  }
});
const DeleteAccountModal = ({ visible, onConfirm, onCancel }) => {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  return (
    <Modal
      visible={visible}
      transparent={true}
      animationType="fade"
      onRequestClose={onCancel}
    >
      <View style={styles.overlay}>
        <View style={styles.modalContainer}>
            <View style={styles.logoAndBodyText}>
                <View>
                    <DeleteProfileIcon {...styles.deleteIcon}/>
                </View>
                <View>
                    <CustomText style={styles.headerTxt}>Delete Account</CustomText>
                </View>
                <View>
                    <CustomText style={styles.messageTxt}>
                        Are you sure you want to delete your account? 
                        This action is permanent and cannot be undone. 
                        Please make sure to back up any important 
                        information before proceeding.
                    </CustomText>
                </View>
            </View>
            <View style={styles.buttonRow}>
                <TouchableOpacity style={styles.cancelBtn} onPress={onCancel}>
                    <CustomText style={styles.cancelTxt}>Cancel</CustomText>
                </TouchableOpacity>
                <TouchableOpacity style={styles.deleteBtn} onPress={onConfirm}>
                    <CustomText style={styles.deleteTxt}>Delete</CustomText>
                </TouchableOpacity>                
            </View>
        </View>
      </View>
    </Modal>
  );
};
export default DeleteAccountModal;