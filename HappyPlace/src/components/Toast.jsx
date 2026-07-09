import React, { useState, useEffect, useRef, useCallback } from 'react';
import { StyleSheet, Animated, View, TouchableOpacity } from 'react-native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { White, HappyColor } from 'src/constants/colors';
import CustomText from 'src/components/FontFamilyText';

const SuccessGreen = '#27AE60';
const DefaultDurationMs = 2500;
const StickyDurationMs = 10000;
const ShowAnimationMs = 200;
const HideAnimationMs = 200;
const UnmountFallbackMs = 350;

let showToastFn = null;
let hideToastFn = null;

export function showToast(message, type = 'success', action = null, key = null) {
    if (showToastFn) {
        showToastFn(message, type, action, key, false);
    }
}

export function updateToastIfVisible(message, type = 'info', action = null, key = null) {
    if (showToastFn) {
        showToastFn(message, type, action, key, true);
    }
}

export function hideToast(key) {
    if (hideToastFn) {
        hideToastFn(key);
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
    },
    actionToast: {
        marginTop: scaleHeight(12),
        paddingVertical: scaleHeight(10),
        paddingLeft: scaleWidth(20),
        paddingRight: scaleWidth(10),
        borderRadius: scaleWidth(99),
        maxWidth: '94%',
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(12)
    },
    actionText: {
        flexShrink: 1,
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: White
    },
    actionBtn: {
        paddingVertical: scaleHeight(8),
        paddingHorizontal: scaleWidth(18),
        borderRadius: scaleWidth(99),
        backgroundColor: White
    },
    actionBtnTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 700
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
    },
    actionToast: {
        marginTop: scaleHeight(16.1),
        paddingVertical: scaleHeight(13),
        paddingLeft: scaleWidth(26.83),
        paddingRight: scaleWidth(13),
        borderRadius: scaleWidth(132.792),
        maxWidth: '84%',
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(16)
    },
    actionText: {
        flexShrink: 1,
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 600,
        color: White
    },
    actionBtn: {
        paddingVertical: scaleHeight(11),
        paddingHorizontal: scaleWidth(24),
        borderRadius: scaleWidth(132.792),
        backgroundColor: White
    },
    actionBtnTxt: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 700
    }
});

export default function ToastHost() {
    const { statusBarHeight } = useSafeAreaPadding();
    const styles = useResponsiveStyles(phoneStyles, tabletStyles);
    const [message, setMessage] = useState('');
    const [type, setType] = useState('success');
    const [action, setAction] = useState(null);
    const [visible, setVisible] = useState(false);
    const [dismissing, setDismissing] = useState(false);
    const opacity = useRef(new Animated.Value(0)).current;
    const translateY = useRef(new Animated.Value(-30)).current;
    const hideTimerRef = useRef(null);
    const unmountTimerRef = useRef(null);
    const dismissalSeqRef = useRef(0);
    const currentKeyRef = useRef(null);
    const visibleRef = useRef(false);
    const dismissingRef = useRef(false);

    useEffect(() => {
        visibleRef.current = visible;
    }, [visible]);

    useEffect(() => {
        dismissingRef.current = dismissing;
    }, [dismissing]);

    const clearTimers = useCallback(() => {
        if (hideTimerRef.current) {
            clearTimeout(hideTimerRef.current);
            hideTimerRef.current = null;
        }
        if (unmountTimerRef.current) {
            clearTimeout(unmountTimerRef.current);
            unmountTimerRef.current = null;
        }
    }, []);

    const finishHide = useCallback((dismissalSeq) => {
        if (dismissalSeq !== dismissalSeqRef.current) return;
        if (unmountTimerRef.current) {
            clearTimeout(unmountTimerRef.current);
            unmountTimerRef.current = null;
        }
        setVisible(false);
        setDismissing(false);
        setAction(null);
    }, []);

    const beginHide = useCallback(() => {
        dismissalSeqRef.current += 1;
        const dismissalSeq = dismissalSeqRef.current;
        clearTimers();
        currentKeyRef.current = null;
        setDismissing(true);
        unmountTimerRef.current = setTimeout(() => finishHide(dismissalSeq), UnmountFallbackMs);
        Animated.parallel([
            Animated.timing(opacity, { toValue: 0, duration: HideAnimationMs, useNativeDriver: true }),
            Animated.timing(translateY, { toValue: -30, duration: HideAnimationMs, useNativeDriver: true })
        ]).start(({ finished }) => {
            if (finished) finishHide(dismissalSeq);
        });
    }, [clearTimers, finishHide, opacity, translateY]);

    useEffect(() => {
        showToastFn = (newMessage, newType, newAction, newKey, updateOnly) => {
            const normalizedKey = newKey != null ? newKey : null;
            const isSameKeyUpdate = normalizedKey != null && normalizedKey === currentKeyRef.current && visibleRef.current && !dismissingRef.current;
            if (updateOnly && !isSameKeyUpdate) {
                return;
            }
            dismissalSeqRef.current += 1;
            clearTimers();
            currentKeyRef.current = normalizedKey;
            setMessage(newMessage);
            setType(newType);
            setAction(newAction || null);
            setDismissing(false);
            setVisible(true);
            if (isSameKeyUpdate) {
                opacity.setValue(1);
                translateY.setValue(0);
            } else {
                opacity.setValue(0);
                translateY.setValue(-30);
                Animated.parallel([
                    Animated.timing(opacity, { toValue: 1, duration: ShowAnimationMs, useNativeDriver: true }),
                    Animated.timing(translateY, { toValue: 0, duration: ShowAnimationMs, useNativeDriver: true })
                ]).start();
            }
            const duration = newAction && newAction.label ? StickyDurationMs : DefaultDurationMs;
            hideTimerRef.current = setTimeout(beginHide, duration);
        };
        hideToastFn = (key) => {
            if (key != null && key === currentKeyRef.current) {
                beginHide();
            }
        };
        return () => {
            showToastFn = null;
            hideToastFn = null;
            clearTimers();
        };
    }, [beginHide, clearTimers, opacity, translateY]);

    if (!visible) return null;

    const backgroundColor = type === 'success' ? SuccessGreen : HappyColor;
    const hasAction = !!(action && action.label);

    const handleActionPress = () => {
        const onPress = action && action.onPress;
        beginHide();
        if (onPress) onPress();
    };

    return (
        <View style={[styles.container, { paddingTop: statusBarHeight }]} pointerEvents="box-none">
            {hasAction ? (
                <Animated.View style={[styles.actionToast, { backgroundColor, opacity, transform: [{ translateY }] }]} pointerEvents={dismissing ? 'none' : 'auto'}>
                    <CustomText style={styles.actionText} numberOfLines={2}>{message}</CustomText>
                    <TouchableOpacity style={styles.actionBtn} onPress={handleActionPress} activeOpacity={0.85}>
                        <CustomText style={[styles.actionBtnTxt, { color: backgroundColor }]}>{action.label}</CustomText>
                    </TouchableOpacity>
                </Animated.View>
            ) : (
                <Animated.View style={[styles.toast, { backgroundColor, opacity, transform: [{ translateY }] }]} pointerEvents="none">
                    <CustomText style={styles.text}>{message}</CustomText>
                </Animated.View>
            )}
        </View>
    );
}