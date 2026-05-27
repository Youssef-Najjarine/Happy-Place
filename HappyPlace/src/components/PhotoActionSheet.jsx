import React, { useEffect, useRef } from 'react';
import { Modal, View, TouchableOpacity, TouchableWithoutFeedback, StyleSheet, Animated } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { White, Black, HappyColor, VeryLightGray } from 'src/constants/colors';
import CustomText from 'src/components/FontFamilyText';

const phoneStyles = StyleSheet.create({
    backdrop: {
        flex: 1,
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        justifyContent: 'flex-end'
    },
    container: {
        backgroundColor: White,
        borderTopLeftRadius: scaleWidth(24),
        borderTopRightRadius: scaleWidth(24),
        paddingTop: scaleHeight(12),
        paddingHorizontal: scaleWidth(20)
    },
    grabber: {
        width: scaleWidth(40),
        height: scaleHeight(4),
        borderRadius: scaleWidth(99),
        alignSelf: 'center',
        marginBottom: scaleHeight(20),
        backgroundColor: VeryLightGray
    },
    title: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 700,
        color: Black,
        textAlign: 'center',
        marginBottom: scaleHeight(20)
    },
    optionButton: {
        height: scaleHeight(52),
        borderRadius: scaleWidth(12),
        justifyContent: 'center',
        alignItems: 'center',
        marginBottom: scaleHeight(8),
        backgroundColor: VeryLightGray
    },
    optionButtonText: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    removeButtonText: {
        color: HappyColor
    },
    cancelButton: {
        height: scaleHeight(52),
        borderRadius: scaleWidth(12),
        justifyContent: 'center',
        alignItems: 'center',
        marginTop: scaleHeight(8),
        backgroundColor: HappyColor
    },
    cancelButtonText: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 700,
        color: White
    }
});

const tabletStyles = StyleSheet.create({
    backdrop: {
        flex: 1,
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        justifyContent: 'flex-end'
    },
    container: {
        backgroundColor: White,
        borderTopLeftRadius: scaleWidth(32),
        borderTopRightRadius: scaleWidth(32),
        paddingTop: scaleHeight(16.1),
        paddingHorizontal: scaleWidth(26.83)
    },
    grabber: {
        width: scaleWidth(53.66),
        height: scaleHeight(5.36),
        borderRadius: scaleWidth(132.792),
        alignSelf: 'center',
        marginBottom: scaleHeight(26.83),
        backgroundColor: VeryLightGray
    },
    title: {
        fontSize: scaleFont(22),
        lineHeight: scaleLineHeight(33),
        letterSpacing: scaleLetterSpacing(-0.22),
        fontWeight: 700,
        color: Black,
        textAlign: 'center',
        marginBottom: scaleHeight(26.83)
    },
    optionButton: {
        height: scaleHeight(69.73),
        borderRadius: scaleWidth(16.096),
        justifyContent: 'center',
        alignItems: 'center',
        marginBottom: scaleHeight(10.73),
        backgroundColor: VeryLightGray
    },
    optionButtonText: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 600,
        color: Black
    },
    removeButtonText: {
        color: HappyColor
    },
    cancelButton: {
        height: scaleHeight(69.73),
        borderRadius: scaleWidth(16.096),
        justifyContent: 'center',
        alignItems: 'center',
        marginTop: scaleHeight(10.73),
        backgroundColor: HappyColor
    },
    cancelButtonText: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 700,
        color: White
    }
});

export default function PhotoActionSheet({ visible, title, hasExistingPhoto, onTakePhoto, onChooseFromLibrary, onRemove, onCancel }) {
    const { bottomSafeHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const slideY = useRef(new Animated.Value(500)).current;

    useEffect(() => {
        if (visible) {
            slideY.setValue(500);
            Animated.spring(slideY, {
                toValue: 0,
                useNativeDriver: true,
                tension: 65,
                friction: 11
            }).start();
        }
    }, [visible, slideY]);

    const containerStyle = {
        ...styles.container,
        paddingBottom: bottomSafeHeight + scaleHeight(16),
        transform: [{ translateY: slideY }]
    };

    return (
        <Modal
            transparent
            visible={visible}
            onRequestClose={onCancel}
            animationType="fade"
            statusBarTranslucent
            hardwareAccelerated
        >
            <TouchableWithoutFeedback onPress={onCancel}>
                <View style={styles.backdrop}>
                    <TouchableWithoutFeedback>
                        <Animated.View style={containerStyle}>
                            <View style={styles.grabber} />
                            <CustomText style={styles.title}>{title || ''}</CustomText>
                            <TouchableOpacity style={styles.optionButton} onPress={onTakePhoto}>
                                <CustomText style={styles.optionButtonText}>Take Photo</CustomText>
                            </TouchableOpacity>
                            <TouchableOpacity style={styles.optionButton} onPress={onChooseFromLibrary}>
                                <CustomText style={styles.optionButtonText}>Choose from Library</CustomText>
                            </TouchableOpacity>
                            {hasExistingPhoto && (
                                <TouchableOpacity style={styles.optionButton} onPress={onRemove}>
                                    <CustomText style={[styles.optionButtonText, styles.removeButtonText]}>Remove Photo</CustomText>
                                </TouchableOpacity>
                            )}
                            <TouchableOpacity style={styles.cancelButton} onPress={onCancel}>
                                <CustomText style={styles.cancelButtonText}>Cancel</CustomText>
                            </TouchableOpacity>
                        </Animated.View>
                    </TouchableWithoutFeedback>
                </View>
            </TouchableWithoutFeedback>
        </Modal>
    );
}