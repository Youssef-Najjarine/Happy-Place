import React from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { HappyColor, White } from 'src/constants/colors';
import CustomText from 'src/components/FontFamilyText';

const phoneStyles = StyleSheet.create({
    root: {
        flex: 1,
        backgroundColor: HappyColor,
        alignItems: 'center',
        justifyContent: 'center',
        paddingHorizontal: scaleWidth(40),
        gap: scaleHeight(16)
    },
    title: {
        fontSize: scaleFont(24),
        lineHeight: scaleLineHeight(36),
        letterSpacing: scaleLetterSpacing(-0.24),
        fontWeight: 700,
        color: White,
        textAlign: 'center'
    },
    subtitle: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: White,
        textAlign: 'center',
        opacity: 0.9
    },
    retryView: {
        width: scaleWidth(200),
        height: scaleHeight(52),
        marginTop: scaleHeight(8)
    },
    retryBtn: {
        width: '100%',
        height: '100%',
        borderRadius: scaleWidth(99),
        backgroundColor: White,
        alignItems: 'center',
        justifyContent: 'center'
    },
    retryTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 700,
        color: HappyColor
    }
});

const tabletStyles = StyleSheet.create({
    root: {
        flex: 1,
        backgroundColor: HappyColor,
        alignItems: 'center',
        justifyContent: 'center',
        paddingHorizontal: scaleWidth(80),
        gap: scaleHeight(20)
    },
    title: {
        fontSize: scaleFont(32),
        lineHeight: scaleLineHeight(48),
        letterSpacing: scaleLetterSpacing(-0.32),
        fontWeight: 700,
        color: White,
        textAlign: 'center'
    },
    subtitle: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 500,
        color: White,
        textAlign: 'center',
        opacity: 0.9
    },
    retryView: {
        width: scaleWidth(260),
        height: scaleHeight(62),
        marginTop: scaleHeight(10)
    },
    retryBtn: {
        width: '100%',
        height: '100%',
        borderRadius: scaleWidth(132.792),
        backgroundColor: White,
        alignItems: 'center',
        justifyContent: 'center'
    },
    retryTxt: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 700,
        color: HappyColor
    }
});

export default function ErrorFallback({ onRetry }) {
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    return (
        <View style={styles.root}>
            <CustomText style={styles.title}>Something went wrong</CustomText>
            <CustomText style={styles.subtitle}>An unexpected error occurred. Tap below to get back to your happy place.</CustomText>
            <View style={styles.retryView}>
                <TouchableOpacity style={styles.retryBtn} onPress={onRetry}>
                    <CustomText style={styles.retryTxt}>Try Again</CustomText>
                </TouchableOpacity>
            </View>
        </View>
    );
}