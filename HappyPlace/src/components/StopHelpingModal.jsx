import React from 'react';
import { View, StyleSheet, Modal, Pressable, TouchableOpacity } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { HappyColor, White, Black, VeryLightGray, TranslucentBlack } from 'src/constants/colors';
import CustomText from 'src/components/FontFamilyText';

const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: TranslucentBlack,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: scaleWidth(32)
  },
  card: {
    width: '100%',
    borderRadius: scaleWidth(24),
    backgroundColor: White,
    paddingTop: scaleHeight(28),
    paddingHorizontal: scaleWidth(24),
    paddingBottom: scaleHeight(20),
    alignItems: 'center'
  },
  title: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(28),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 800,
    textAlign: 'center',
    marginBottom: scaleHeight(8)
  },
  message: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    opacity: 0.6,
    fontWeight: 500,
    textAlign: 'center',
    marginBottom: scaleHeight(24)
  },
  buttons: {
    width: '100%',
    gap: scaleHeight(10)
  },
  confirmBtn: {
    height: scaleHeight(50),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  confirmTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 700
  },
  keepBtn: {
    height: scaleHeight(50),
    borderRadius: scaleWidth(99),
    backgroundColor: VeryLightGray,
    justifyContent: 'center',
    alignItems: 'center'
  },
  keepTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 700
  }
});

const tabletStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: TranslucentBlack,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: scaleWidth(160)
  },
  card: {
    width: '100%',
    borderRadius: scaleWidth(32),
    backgroundColor: White,
    paddingTop: scaleHeight(36),
    paddingHorizontal: scaleWidth(32),
    paddingBottom: scaleHeight(26),
    alignItems: 'center'
  },
  title: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(36),
    letterSpacing: scaleLetterSpacing(-0.26),
    color: Black,
    fontWeight: 800,
    textAlign: 'center',
    marginBottom: scaleHeight(12)
  },
  message: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    opacity: 0.6,
    fontWeight: 500,
    textAlign: 'center',
    marginBottom: scaleHeight(32)
  },
  buttons: {
    width: '100%',
    gap: scaleHeight(14)
  },
  confirmBtn: {
    height: scaleHeight(62),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  confirmTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: White,
    fontWeight: 700
  },
  keepBtn: {
    height: scaleHeight(62),
    borderRadius: scaleWidth(132.792),
    backgroundColor: VeryLightGray,
    justifyContent: 'center',
    alignItems: 'center'
  },
  keepTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 700
  }
});

export default function StopHelpingModal({ visible, offeredCount, onConfirm, onCancel }) {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const count = offeredCount || 0;
  const message = count === 1
    ? 'This withdraws your offer to 1 person.'
    : `This withdraws your offers to ${count} people.`;

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable style={styles.overlay} onPress={onCancel}>
        <Pressable style={styles.card} onPress={() => {}}>
          <CustomText style={styles.title}>Stop helping?</CustomText>
          <CustomText style={styles.message}>{message}</CustomText>
          <View style={styles.buttons}>
            <TouchableOpacity style={styles.confirmBtn} onPress={onConfirm} activeOpacity={0.85}>
              <CustomText style={styles.confirmTxt}>Stop helping</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.keepBtn} onPress={onCancel} activeOpacity={0.85}>
              <CustomText style={styles.keepTxt}>Keep helping</CustomText>
            </TouchableOpacity>
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}