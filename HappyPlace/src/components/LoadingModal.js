import React, { useRef, useEffect } from 'react';
import { Modal, View, StyleSheet, Platform, Animated, Easing } from 'react-native';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import InnerArc from 'assets/images/loading/inner-arc.svg';
import MiddleArc from 'assets/images/loading/middle-arc.svg';
import OuterArc from 'assets/images/loading/outer-arc.svg';
const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(35, 35, 35, 0.30)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  spinnerContainer: {
    width: scaleWidth(136),
    height: scaleHeight(136),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: '#FFF',
    shadowColor: 'rgba(83, 26, 255, 0.10)',
    shadowOffset: { width: 10.731, height: 10.731 },
    shadowOpacity: 1,
    shadowRadius: 20.12,
    ...(Platform.OS === 'android' && { elevation: 10 }),
  },
arcInner: {
  // width: scaleWidth(12),
  // height: scaleHeight(12),
  width: scaleWidth(22),
  height: scaleHeight(22),
  position: 'absolute',
  top: (scaleHeight(136 - 12)) / 2,
  left: (scaleWidth(136 - 12)) / 2
},
arcMiddle: {
  // width: scaleWidth(52),
  // height: scaleHeight(52),
  width: scaleWidth(51.333),
  height: scaleHeight(51.333),
  position: 'absolute',
  top: (scaleHeight(136 - 52)) / 2,
  left: (scaleWidth(136 - 52)) / 2
},
arcOuter: {
  // width: scaleWidth(112),
  // height: scaleHeight(112),
  width: scaleWidth(77),
  height: scaleHeight(77),
  position: 'absolute',
  top: (scaleHeight(136 - 112)) / 2,
  left: (scaleWidth(136 - 112)) / 2
},
});
const tabletStyles = StyleSheet.create({});
const LoadingModal = () => {
  const isLoading = useSelector((state) => state.loading.isLoading);
const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const outerRotate = useRef(new Animated.Value(0)).current;
  const middleRotate = useRef(new Animated.Value(0)).current;
  const innerRotate = useRef(new Animated.Value(0)).current;

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
  }, [outerRotate, middleRotate, innerRotate]);

  return (
    <Modal transparent visible={isLoading} animationType="fade" onRequestClose={() => {}}>
      <View style={styles.overlay}>
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
      </View>
    </Modal>
  );
};
export default LoadingModal;