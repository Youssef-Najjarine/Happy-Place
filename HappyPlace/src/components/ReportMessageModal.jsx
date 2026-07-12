import React, { useState, useEffect } from 'react';
import { Modal, View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black, VeryLightGray, SoftGray, SemiTransparentCharcoal, VeryLightLavenderTint } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';

const MAX_REASON_LENGTH = 500;

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
  headerTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    textAlign: 'center',
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
  reasonInputView: {
    borderRadius: scaleWidth(12),
    borderWidth: scaleWidth(1),
    borderColor: SoftGray,
    paddingHorizontal: scaleWidth(12),
    paddingVertical: scaleHeight(8),
    minHeight: scaleHeight(88),
    backgroundColor: White,
  },
  reasonInput: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black,
    textAlignVertical: 'top',
  },
  buttonRow: {
    height: scaleHeight(48),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cancelBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(145.5),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray,
  },
  cancelTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
  },
  reportBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(145.5),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor,
  },
  reportTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White,
  },
});

const tabletStyles = phoneStyles;

const ReportMessageModal = ({ visible, onSubmit, onCancel }) => {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (visible) setReason('');
  }, [visible]);

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <View style={styles.overlay}>
        <View style={styles.modalContainer}>
          <CustomText style={styles.headerTxt}>Report message</CustomText>
          <CustomText style={styles.messageTxt}>
            Tell us what's wrong with this message. Our team will review it.
          </CustomText>
          <View style={styles.reasonInputView}>
            <CustomTextInput
              style={styles.reasonInput}
              value={reason}
              onChangeText={setReason}
              placeholder="Reason (optional)"
              placeholderTextColor="rgba(35, 35, 35, 0.50)"
              multiline
              maxLength={MAX_REASON_LENGTH}
              autoCapitalize="sentences"
              autoCorrect
            />
          </View>
          <View style={styles.buttonRow}>
            <TouchableOpacity style={styles.cancelBtn} onPress={onCancel}>
              <CustomText style={styles.cancelTxt}>Cancel</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.reportBtn} onPress={() => onSubmit(reason.trim())}>
              <CustomText style={styles.reportTxt}>Report</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

export default ReportMessageModal;