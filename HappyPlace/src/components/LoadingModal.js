import React, { useRef } from 'react';
import { Modal, View, StyleSheet, Platform, Animated, Easing } from 'react-native';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from '../utils/useResponsiveStyles';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';

const useRotation = () => {
  const rotation = useRef(new Animated.Value(0)).current;
  React.useEffect(() => {
    Animated.loop(
      Animated.timing(rotation, {
        toValue: 1,
        duration: 1200,
        easing: Easing.linear,
        useNativeDriver: true,
      })
    ).start();
  }, [rotation]);
  return rotation.interpolate({
    inputRange: [0, 1],
    outputRange: ['0deg', '360deg'],
  });
};

const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(35, 35, 35, 0.30)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  spinnerContainer: {
    width: scaleWidth(96),
    height: scaleHeight(96),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: '#FFF',
    shadowColor: 'rgba(83, 26, 255, 0.10)',
    shadowOffset: { width: 10.731, height: 10.731 },
    shadowOpacity: 1,
    shadowRadius: 20.12,
    ...(Platform.OS === 'android' && { elevation: 10 }),
    justifyContent: 'center',
    alignItems: 'center',
    position: 'relative',
  },
  arcBase: {
    position: 'absolute',
    top: '50%', // Center vertically
    left: '50%', // Center horizontally
    transform: [{ translateX: -scaleWidth(24) }, { translateY: -scaleHeight(12) }], // Adjust for half width/height
  },
  arcOuter: {
    width: scaleWidth(48), // Diameter of 48px
    height: scaleHeight(24), // Half circle height
    borderTopWidth: 4, // Thickness
    borderLeftWidth: 4,
    borderRightWidth: 4,
    borderBottomWidth: 0,
    borderColor: '#FF69B4', // Dark pink
    borderTopLeftRadius: scaleWidth(24), // Half of width for semi-circle
    borderTopRightRadius: scaleWidth(24),
  },
  arcMiddle: {
    width: scaleWidth(32), // Smaller diameter
    height: scaleHeight(16),
    borderTopWidth: 4,
    borderLeftWidth: 4,
    borderRightWidth: 4,
    borderBottomWidth: 0,
    borderColor: '#FFB6C1', // Medium pink
    borderTopLeftRadius: scaleWidth(16),
    borderTopRightRadius: scaleWidth(16),
    top: scaleHeight(12), // 8px spacing from outer (12px total offset accounts for half height)
  },
  arcInner: {
    width: scaleWidth(16), // Smallest diameter
    height: scaleHeight(8),
    borderTopWidth: 4,
    borderLeftWidth: 4,
    borderRightWidth: 4,
    borderBottomWidth: 0,
    borderColor: '#FFE4E1', // Light pink
    borderTopLeftRadius: scaleWidth(8),
    borderTopRightRadius: scaleWidth(8),
    top: scaleHeight(20), // 8px spacing from middle (20px total offset)
  },
});

const tabletStyles = StyleSheet.create({});

const LoadingModal = () => {
  const isLoading = useSelector((state) => state.loading.isLoading);
  const rotation = useRotation(); // Optional animation

  return (
    <Modal transparent visible={isLoading} animationType="fade" onRequestClose={() => {}}>
      <View style={phoneStyles.overlay}>
        <View style={phoneStyles.spinnerContainer}>
          <Animated.View style={[phoneStyles.arcBase, phoneStyles.arcOuter, { transform: [{ rotate: rotation }] }]}>
          </Animated.View>
          <Animated.View style={[phoneStyles.arcBase, phoneStyles.arcMiddle, { transform: [{ rotate: rotation }] }]}>
          </Animated.View>
          <Animated.View style={[phoneStyles.arcBase, phoneStyles.arcInner, { transform: [{ rotate: rotation }] }]}>
          </Animated.View>
        </View>
      </View>
    </Modal>
  );
};

export default LoadingModal;