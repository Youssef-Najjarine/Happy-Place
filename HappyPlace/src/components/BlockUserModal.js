import React from 'react';
import { Modal, View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black, VeryLightGray, SoftGray, SemiTransparentCharcoal, VeryLightLavenderTint } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import BlockIcon from 'assets/images/modals/delete-profile-modal-icon.svg';
const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: SemiTransparentCharcoal,
  },
  modalContainer: {
    gap: scaleHeight(16),
    width: scaleWidth(335),
    paddingTop: scaleHeight(24),
    paddingBottom: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowColor: VeryLightLavenderTint,
    shadowOffset: { width: scaleWidth(8), height: scaleHeight(8) },
    shadowOpacity: 1,
    shadowRadius: scaleWidth(30),
    elevation: 8,
    borderColor: SoftGray,
    backgroundColor: White,
  },
  logoAndBodyText: {
    gap: scaleHeight(12),
    alignItems: 'center'
  },
  blockIcon: {
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
    width: scaleWidth(147),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  cancelTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
  },
  blockBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(147),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  blockTxt: {
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
    backgroundColor: SemiTransparentCharcoal,
  },
  modalContainer: {
    gap: scaleHeight(21.46),
    width: scaleWidth(449.283),
    paddingTop: scaleHeight(32.19),
    paddingBottom: scaleHeight(21.46),
    paddingHorizontal: scaleWidth(21.46),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: VeryLightLavenderTint,
    shadowOffset: { width: scaleWidth(10.73), height: scaleHeight(10.73) },
    shadowOpacity: 1,
    shadowRadius: scaleWidth(40.23),
    elevation: 8,
    borderColor: SoftGray,
    backgroundColor: White,
  },
  logoAndBodyText: {
    gap: scaleHeight(16.1),
    alignItems: 'center'
  },
  blockIcon: {
    width: scaleWidth(80.48),
    height: scaleHeight(80.48)
  },
  headerTxt: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(32.19),
    letterSpacing: scaleLetterSpacing(-0.26),
    fontWeight: 600,
    color: Black,
  },
  messageTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    opacity: 0.7,
    textAlign: 'center',
    color: Black,
  },
  buttonRow: {
    height: scaleHeight(64.38),
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
    backgroundColor: VeryLightGray
  },
  cancelTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  blockBtn: {
    borderRadius: scaleWidth(132.792),
    width: scaleWidth(192.625),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  blockTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White,
  }
});
const BlockUserModal = ({ visible, username, onConfirm, onCancel }) => {
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
                    <BlockIcon {...styles.blockIcon}/>
                </View>
                <View>
                    <CustomText style={styles.headerTxt}>Block{username ? ` @${username}` : ''}?</CustomText>
                </View>
                <View>
                    <CustomText style={styles.messageTxt}>
                      They will not be able to find your profile, see your
                      friends, or send you requests. Any friendship or pending
                      requests between you will be removed.
                    </CustomText>
                </View>
            </View>
            <View style={styles.buttonRow}>
                <TouchableOpacity style={styles.cancelBtn} onPress={onCancel}>
                    <CustomText style={styles.cancelTxt}>Cancel</CustomText>
                </TouchableOpacity>
                <TouchableOpacity style={styles.blockBtn} onPress={onConfirm}>
                    <CustomText style={styles.blockTxt}>Block</CustomText>
                </TouchableOpacity>
            </View>
        </View>
      </View>
    </Modal>
  );
};
export default BlockUserModal;