import React, { useRef } from 'react';
import { Modal, View, StyleSheet, Platform, Animated, Easing } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from '../utils/useResponsiveStyles';
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
  width: scaleWidth(12),
  height: scaleHeight(12),
  position: 'absolute',
  top: (scaleHeight(136 - 12)) / 2,
  left: (scaleWidth((136 - 12) + 16)) / 2
},
arcMiddle: {
  width: scaleWidth(28),
  height: scaleHeight(28),
  position: 'absolute',
  top: (scaleHeight(136 - 28)) / 2,
  left: (scaleWidth((136 - 28) + 8)) / 2
},
arcOuter: {
  width: scaleWidth(42),
  height: scaleHeight(42),
  position: 'absolute',
  top: (scaleHeight(136 -42)) / 2,
  left: (scaleWidth(136 - 42)) / 2
},
});

const tabletStyles = StyleSheet.create({});
const LoadingModal = () => {
  const isLoading = useSelector((state) => state.loading.isLoading);
const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  return (
    <Modal transparent visible={isLoading} animationType="fade" onRequestClose={() => {}}>
      <View style={styles.overlay}>
        <View style={styles.spinnerContainer}>
          <OuterArc {...styles.arcOuter}/>
          <MiddleArc {...styles.arcMiddle}/>
          <InnerArc {...styles.arcInner}/>
        </View>
      </View>
    </Modal>
  );
};

export default LoadingModal;