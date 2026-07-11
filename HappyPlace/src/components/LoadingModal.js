import React, { useRef, useEffect, useState } from 'react';
import { View, StyleSheet, Platform, Animated, Easing } from 'react-native';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleWidth } from 'src/utils/scaleLayout';
import { White, SemiTransparentCharcoal, SoftGray, VeryLightLavenderTint } from 'src/constants/colors';
import InnerArc from 'assets/images/loading/inner-arc.svg';
import MiddleArc from 'assets/images/loading/middle-arc.svg';
import OuterArc from 'assets/images/loading/outer-arc.svg';

const FadeMs = 150;
const UnmountFallbackMs = 300;
const PhoneSpinnerScale = scaleWidth(136) / 136;
const TabletSpinnerScale = 1.3415;

const phoneStyles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: SemiTransparentCharcoal,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 9000,
    elevation: 9000,
  },
  spinnerContainer: {
    width: 136,
    height: 136,
    borderRadius: 21.461,
    borderWidth: 1.341,
    borderColor: SoftGray,
    backgroundColor: White,
    shadowColor: VeryLightLavenderTint,
    shadowOffset: { width: 10.731, height: 10.731 },
    shadowOpacity: 1,
    shadowRadius: 20.12,
    transform: [{ scale: PhoneSpinnerScale }],
    ...(Platform.OS === 'android' && { elevation: 10 }),
  },
  arcInner: {
    width: 22,
    height: 22,
    position: 'absolute',
    top: (136 - 22) / 2,
    left: (136 - 22) / 2
  },
  arcMiddle: {
    width: 51.333,
    height: 51.333,
    position: 'absolute',
    top: (136 - 51.333) / 2,
    left: (136 - 51.333) / 2
  },
  arcOuter: {
    width: 77,
    height: 77,
    position: 'absolute',
    top: (136 - 77) / 2,
    left: (136 - 77) / 2
  }
});

const tabletStyles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: SemiTransparentCharcoal,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 9000,
    elevation: 9000,
  },
  spinnerContainer: {
    width: 136,
    height: 136,
    borderRadius: 21.461,
    borderWidth: 1.341,
    borderColor: SoftGray,
    backgroundColor: White,
    shadowColor: VeryLightLavenderTint,
    shadowOffset: { width: 10.731, height: 10.731 },
    shadowOpacity: 1,
    shadowRadius: 20.12,
    transform: [{ scale: TabletSpinnerScale }],
    ...(Platform.OS === 'android' && { elevation: 10 }),
  },
  arcInner: {
    width: 22,
    height: 22,
    position: 'absolute',
    top: (136 - 22) / 2,
    left: (136 - 22) / 2
  },
  arcMiddle: {
    width: 51.333,
    height: 51.333,
    position: 'absolute',
    top: (136 - 51.333) / 2,
    left: (136 - 51.333) / 2
  },
  arcOuter: {
    width: 77,
    height: 77,
    position: 'absolute',
    top: (136 - 77) / 2,
    left: (136 - 77) / 2
  }
});

const LoadingModal = () => {
  const isLoading = useSelector((state) => state.loading.isLoading);
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const [mounted, setMounted] = useState(false);
  const overlayOpacity = useRef(new Animated.Value(0)).current;
  const unmountTimerRef = useRef(null);
  const outerRotate = useRef(new Animated.Value(0)).current;
  const middleRotate = useRef(new Animated.Value(0)).current;
  const innerRotate = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (isLoading) {
      if (unmountTimerRef.current) {
        clearTimeout(unmountTimerRef.current);
        unmountTimerRef.current = null;
      }
      setMounted(true);
      Animated.timing(overlayOpacity, { toValue: 1, duration: FadeMs, useNativeDriver: true }).start();
      return;
    }
    unmountTimerRef.current = setTimeout(() => setMounted(false), UnmountFallbackMs);
    Animated.timing(overlayOpacity, { toValue: 0, duration: FadeMs, useNativeDriver: true }).start(({ finished }) => {
      if (finished) setMounted(false);
    });
    return () => {
      if (unmountTimerRef.current) {
        clearTimeout(unmountTimerRef.current);
        unmountTimerRef.current = null;
      }
    };
  }, [isLoading, overlayOpacity]);

  const outerDeg = outerRotate.interpolate({
    inputRange: [0, 1],
    outputRange: ['0deg', '-360deg'],
  });

  const middleDeg = middleRotate.interpolate({
    inputRange: [0, 1],
    outputRange: ['0deg', '360deg'],
  });

  const innerDeg = innerRotate.interpolate({
    inputRange: [0, 1],
    outputRange: ['0deg', '-360deg'],
  });

  useEffect(() => {
    if (!isLoading) return;

    outerRotate.setValue(0);
    middleRotate.setValue(0);
    innerRotate.setValue(0);

    const animOuter = Animated.loop(
      Animated.timing(outerRotate, {
        toValue: 1,
        duration: 2000,
        easing: Easing.linear,
        useNativeDriver: true,
      })
    );

    const animMiddle = Animated.loop(
      Animated.timing(middleRotate, {
        toValue: 1,
        duration: 2000,
        easing: Easing.linear,
        useNativeDriver: true,
      })
    );

    const animInner = Animated.loop(
      Animated.timing(innerRotate, {
        toValue: 1,
        duration: 2000,
        easing: Easing.linear,
        useNativeDriver: true,
      })
    );

    animOuter.start();
    animMiddle.start();
    animInner.start();

    return () => {
      animOuter.stop();
      animMiddle.stop();
      animInner.stop();
    };
  }, [isLoading, outerRotate, middleRotate, innerRotate]);

  if (!mounted) return null;

  return (
    <Animated.View style={[styles.overlay, { opacity: overlayOpacity }]} pointerEvents={isLoading ? 'auto' : 'none'}>
      <View style={styles.spinnerContainer}>
        <Animated.View style={{ ...styles.arcOuter, transform: [{ rotate: outerDeg }] }}>
          <OuterArc />
        </Animated.View>
        <Animated.View style={{ ...styles.arcMiddle, transform: [{ rotate: middleDeg }] }}>
          <MiddleArc />
        </Animated.View>
        <Animated.View style={{ ...styles.arcInner, transform: [{ rotate: innerDeg }] }}>
          <InnerArc />
        </Animated.View>
      </View>
    </Animated.View>
  );
};

export default LoadingModal;