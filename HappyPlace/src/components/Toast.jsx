import React, { useState, useEffect, useRef } from 'react';
import { StyleSheet, Animated, View } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { White, HappyColor } from 'src/constants/colors';
import CustomText from 'src/components/FontFamilyText';

const SuccessGreen = '#27AE60';

let showToastFn = null;

export function showToast(message, type = 'success') {
    if (showToastFn) {
        showToastFn(message, type);
    }
}

const phoneStyles = StyleSheet.create({
    container: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        alignItems: 'center',
        zIndex: 9999
    },
    toast: {
        marginTop: scaleHeight(12),
        paddingVertical: scaleHeight(12),
        paddingHorizontal: scaleWidth(20),
        borderRadius: scaleWidth(99),
        maxWidth: '90%'
    },
    text: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: White,
        textAlign: 'center'
    }
});

const tabletStyles = StyleSheet.create({
    container: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        alignItems: 'center',
        zIndex: 9999
    },
    toast: {
        marginTop: scaleHeight(16.1),
        paddingVertical: scaleHeight(16.1),
        paddingHorizontal: scaleWidth(26.83),
        borderRadius: scaleWidth(132.792),
        maxWidth: '80%'
    },
    text: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 600,
        color: White,
        textAlign: 'center'
    }
});

export default function ToastHost() {
    const { statusBarHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const [message, setMessage] = useState('');
    const [type, setType] = useState('success');
    const [visible, setVisible] = useState(false);
    const opacity = useRef(new Animated.Value(0)).current;
    const translateY = useRef(new Animated.Value(-30)).current;
    const hideTimerRef = useRef(null);

    useEffect(() => {
        showToastFn = (newMessage, newType) => {
            setMessage(newMessage);
            setType(newType);
            setVisible(true);
            opacity.setValue(0);
            translateY.setValue(-30);
            Animated.parallel([
                Animated.timing(opacity, { toValue: 1, duration: 200, useNativeDriver: true }),
                Animated.timing(translateY, { toValue: 0, duration: 200, useNativeDriver: true })
            ]).start();
            if (hideTimerRef.current) clearTimeout(hideTimerRef.current);
            hideTimerRef.current = setTimeout(() => {
                Animated.parallel([
                    Animated.timing(opacity, { toValue: 0, duration: 200, useNativeDriver: true }),
                    Animated.timing(translateY, { toValue: -30, duration: 200, useNativeDriver: true })
                ]).start(() => setVisible(false));
            }, 2500);
        };
        return () => {
            showToastFn = null;
            if (hideTimerRef.current) clearTimeout(hideTimerRef.current);
        };
    }, [opacity, translateY]);

    if (!visible) return null;

    const backgroundColor = type === 'success' ? SuccessGreen : HappyColor;

    return (
        <View style={[styles.container, { paddingTop: statusBarHeight }]} pointerEvents="none">
            <Animated.View style={[styles.toast, { backgroundColor, opacity, transform: [{ translateY }] }]}>
                <CustomText style={styles.text}>{message}</CustomText>
            </Animated.View>
        </View>
    );
}