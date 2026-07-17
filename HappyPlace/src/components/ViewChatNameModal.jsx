import React from 'react';
import { Modal, View, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black, SoftGray, SemiTransparentCharcoal, VeryLightLavenderTint, VividBlueViolet } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
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
    marginBottom: scaleHeight(16)
  },
  headerTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black,
  },
  nameScroll: {
    maxHeight: scaleHeight(180),
    marginBottom: scaleHeight(16)
  },
  nameTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: Black,
  },
  closeBtn: {
    borderRadius: scaleWidth(99),
    width: '100%',
    height: scaleHeight(48),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  closeTxt: {
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
    marginBottom: scaleHeight(24)
  },
  headerTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(26.4),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black,
  },
  nameScroll: {
    maxHeight: scaleHeight(240),
    marginBottom: scaleHeight(24)
  },
  nameTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black,
  },
  closeBtn: {
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: scaleHeight(62.192),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  closeTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White,
  }
});
const ViewChatNameModal = ({
  visible,
  chatName = '',
  onClose,
}) => {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <View style={styles.overlay}>
        <View style={styles.modalContainer}>
          <View style={styles.headerView}>
            <CustomText style={styles.headerTxt}>Chat Name</CustomText>
          </View>
          <ScrollView style={styles.nameScroll} showsVerticalScrollIndicator={false}>
            <CustomText style={styles.nameTxt} selectable>{chatName}</CustomText>
          </ScrollView>
          <TouchableOpacity style={styles.closeBtn} onPress={onClose}>
            <CustomText style={styles.closeTxt}>Close</CustomText>
          </TouchableOpacity>
        </View>
      </View>
    </Modal>
  );
};

export default ViewChatNameModal;