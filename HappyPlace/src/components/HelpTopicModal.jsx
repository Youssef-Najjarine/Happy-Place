import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Modal, View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black, LightGray, VeryLightGray, SoftGray, SemiTransparentCharcoal, VeryLightLavenderTint, VividBlueViolet } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: SemiTransparentCharcoal,
  },
  modalContainer: {
    width: scaleWidth(335),
    paddingVertical: scaleHeight(16),
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
  headerView: {
    marginBottom: scaleHeight(8)
  },
  headerTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  subheaderView: {
    marginBottom: scaleHeight(16)
  },
  subheaderTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    opacity: 0.6,
    color: Black,
  },
  inputView: {
    marginBottom: scaleHeight(4)
  },
  input: {
    height: scaleHeight(48),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(99),
    paddingHorizontal: scaleWidth(12),
    paddingVertical: scaleHeight(12),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    borderColor: LightGray,
    backgroundColor: White,
    color: Black
  },
  message: {
    marginBottom: scaleHeight(12)
  },
  messageTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    opacity: 0.2,
    fontStyle: 'italic',
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
    backgroundColor: VeryLightGray
  },
  cancelTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
  },
  confirmBtn: {
    borderRadius: scaleWidth(99),
    width: scaleWidth(145.5),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  confirmBtnDisabled: {
    opacity: 0.4
  },
  confirmTxt: {
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
    width: scaleWidth(449.347),
    paddingTop: scaleHeight(32),
    paddingBottom: scaleHeight(24),
    paddingHorizontal: scaleWidth(24),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: VividBlueViolet,
    shadowOffset: { width: scaleWidth(10.731), height: scaleHeight(10.731) },
    shadowRadius: scaleWidth(40.24),
    shadowOpacity: 0.10,
    elevation: 11,
    borderColor: SoftGray,
    backgroundColor: White,
  },
  headerView: {
    marginBottom: scaleHeight(12)
  },
  headerTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(26.4),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black,
  },
  subheaderView: {
    marginBottom: scaleHeight(24)
  },
  subheaderTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    opacity: 0.6,
    color: Black,
  },
  inputView: {
    marginBottom: scaleHeight(8)
  },
  input: {
    height: scaleHeight(59),
    borderWidth: scaleWidth(1.341),
    borderRadius: scaleWidth(132.792),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(16),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    borderColor: LightGray,
    backgroundColor: White,
    color: Black
  },
  message: {
    marginBottom: scaleHeight(16)
  },
  messageTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    opacity: 0.2,
    fontStyle: 'italic',
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
    backgroundColor: VeryLightGray
  },
  cancelTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  confirmBtn: {
    borderRadius: scaleWidth(132.792),
    width: scaleWidth(192.625),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  confirmBtnDisabled: {
    opacity: 0.4
  },
  confirmTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White,
  }
});
const PRESENTATION_CONFIRM_TIMEOUT_MS = 1000;

const HelpTopicModal = ({
  visible,
  maxLen = 100,
  onConfirm,
  onCancel,
  onPresentationFailed,
}) => {
  const [topic, setTopic] = useState('');
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const presentationConfirmedRef = useRef(false);
  const onPresentationFailedRef = useRef(onPresentationFailed);

  useEffect(() => {
    onPresentationFailedRef.current = onPresentationFailed;
  }, [onPresentationFailed]);

  useEffect(() => {
    if (!visible) return;
    presentationConfirmedRef.current = false;
    const presentationTimer = setTimeout(() => {
      if (!presentationConfirmedRef.current && onPresentationFailedRef.current) {
        onPresentationFailedRef.current();
      }
    }, PRESENTATION_CONFIRM_TIMEOUT_MS);
    return () => clearTimeout(presentationTimer);
  }, [visible]);

  const handleModalShow = () => {
    presentationConfirmedRef.current = true;
  };

  useEffect(() => {
    if (visible) setTopic('');
  }, [visible]);

  const remaining = useMemo(() => `${topic.length}/${maxLen} characters`, [topic.length, maxLen]);
  const trimmedTopic = topic.trim();
  const canConfirm = trimmedTopic.length > 0;

  const handleConfirm = () => {
    if (!canConfirm) return;
    onConfirm?.(trimmedTopic);
  };

  return (
    <Modal visible={visible} transparent animationType="fade" onShow={handleModalShow} onRequestClose={onCancel}>
      <View style={styles.overlay}>
        <View style={styles.modalContainer}>
          <View style={styles.headerView}>
            <CustomText style={styles.headerTxt}>What's on your mind?</CustomText>
          </View>
          <View style={styles.inputView}>
            <CustomTextInput
              style={styles.input}
              keyboardType="default"
              autoCapitalize="sentences"
              autoCorrect={false}
              maxLength={maxLen}
              value={topic}
              onChangeText={setTopic}
              placeholder="What would you like to talk about?"
              returnKeyType="done"
              onSubmitEditing={handleConfirm}
              blurOnSubmit
            />
          </View>
          <View style={styles.message}>
            <CustomText style={styles.messageTxt}>{remaining}</CustomText>
          </View>
          <View style={styles.buttonRow}>
            <TouchableOpacity style={styles.cancelBtn} onPress={onCancel}>
              <CustomText style={styles.cancelTxt}>Cancel</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={[styles.confirmBtn, !canConfirm && styles.confirmBtnDisabled]} onPress={handleConfirm} disabled={!canConfirm}>
              <CustomText style={styles.confirmTxt}>Find Someone</CustomText>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

export default HelpTopicModal;