import React from 'react';
import { Modal, View, ActivityIndicator, StyleSheet } from 'react-native';
import { useSelector } from 'react-redux';
import { useResponsiveStyles } from '../utils/useResponsiveStyles';

const phoneStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  spinnerContainer: {
    padding: 15,
    backgroundColor: '#fff',
    borderRadius: 8,
  },
});

const tabletStyles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  spinnerContainer: {
    padding: 25,
    backgroundColor: '#fff',
    borderRadius: 12,
  },
});

const LoadingModal = () => {
  const isLoading = useSelector((state) => state.loading.isLoading);
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);

  return (
    <Modal
      transparent
      visible={isLoading}
      animationType="fade"
      onRequestClose={() => {}}
    >
      <View style={styles.overlay}>
        <View style={styles.spinnerContainer}>
          <ActivityIndicator size="large" color="#0000ff" />
        </View>
      </View>
    </Modal>
  );
};

export default LoadingModal;