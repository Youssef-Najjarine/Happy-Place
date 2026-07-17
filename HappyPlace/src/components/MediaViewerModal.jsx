import React, { useState, useRef, useEffect } from 'react';
import { Modal, View, TouchableOpacity, Image, StyleSheet, Platform, PermissionsAndroid, Animated, PanResponder, Pressable, FlatList, useWindowDimensions } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { Svg, Path } from 'react-native-svg';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { White, Black, HappyColor, SemiTransparentCharcoal } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import CustomText from 'src/components/FontFamilyText';
import RemoteImage from 'src/components/RemoteImage';
import baseService from 'src/services/baseService';
import { showToast } from 'src/components/Toast';
function loadOptionalModule(loader) {
    try {
        const loadedModule = loader();
        return loadedModule && loadedModule.default ? loadedModule.default : loadedModule;
    } catch (error) {
        return null;
    }
}
const Video = loadOptionalModule(() => require('react-native-video'));
const CameraRollModule = loadOptionalModule(() => require('@react-native-camera-roll/camera-roll'));
const CameraRoll = CameraRollModule && CameraRollModule.CameraRoll ? CameraRollModule.CameraRoll : CameraRollModule;
const BlobUtil = loadOptionalModule(() => require('react-native-blob-util'));

function CloseGlyph({ size, color }) {
    return (
        <Svg width={size} height={size} viewBox="0 0 24 24">
            <Path d="M6.4 6.4l11.2 11.2M17.6 6.4L6.4 17.6" stroke={color} strokeWidth={2.4} strokeLinecap="round" />
        </Svg>
    );
}

function touchDistance(touches) {
    const deltaX = touches[0].pageX - touches[1].pageX;
    const deltaY = touches[0].pageY - touches[1].pageY;
    return Math.sqrt(deltaX * deltaX + deltaY * deltaY);
}

function ZoomableMedia({ children, allowTapZoom, backdropOpacity, onDismiss, onZoomChange }) {
    const scaleValue = useRef(new Animated.Value(1)).current;
    const translateXValue = useRef(new Animated.Value(0)).current;
    const translateYValue = useRef(new Animated.Value(0)).current;
    const lastScaleRef = useRef(1);
    const lastTranslateRef = useRef({ x: 0, y: 0 });
    const currentScaleRef = useRef(1);
    const pinchStartDistanceRef = useRef(null);
    const pinchBaseScaleRef = useRef(1);
    const gestureModeRef = useRef('none');
    const movedRef = useRef(false);
    const lastTapRef = useRef(0);

    const reportZoom = () => {
        if (onZoomChange) onZoomChange(lastScaleRef.current > 1.01);
    };

    const springTo = (scale, translateX, translateY) => {
        lastScaleRef.current = scale;
        currentScaleRef.current = scale;
        lastTranslateRef.current = { x: translateX, y: translateY };
        Animated.parallel([
            Animated.spring(scaleValue, { toValue: scale, useNativeDriver: true }),
            Animated.spring(translateXValue, { toValue: translateX, useNativeDriver: true }),
            Animated.spring(translateYValue, { toValue: translateY, useNativeDriver: true }),
            Animated.timing(backdropOpacity, { toValue: 1, duration: 160, useNativeDriver: true }),
        ]).start();
        reportZoom();
    };

    const handleTap = () => {
        const now = Date.now();
        if (now - lastTapRef.current < 280) {
            lastTapRef.current = 0;
            if (lastScaleRef.current > 1.01) {
                springTo(1, 0, 0);
            } else {
                springTo(2.4, 0, 0);
            }
            return;
        }
        lastTapRef.current = now;
    };

    const panResponder = useRef(PanResponder.create({
        onStartShouldSetPanResponderCapture: (event) => event.nativeEvent.touches.length >= 2,
        onMoveShouldSetPanResponderCapture: (event, gestureState) => {
            if (event.nativeEvent.touches.length >= 2) return true;
            if (lastScaleRef.current > 1.01) return true;
            return gestureState.dy > 15 && Math.abs(gestureState.dy) > Math.abs(gestureState.dx) * 1.5;
        },
        onPanResponderTerminationRequest: () => false,
        onPanResponderGrant: () => {
            movedRef.current = false;
            gestureModeRef.current = 'none';
            pinchStartDistanceRef.current = null;
        },
        onPanResponderMove: (event, gestureState) => {
            const touches = event.nativeEvent.touches;
            if (touches.length >= 2) {
                gestureModeRef.current = 'pinch';
                movedRef.current = true;
                const distance = touchDistance(touches);
                if (pinchStartDistanceRef.current == null) {
                    pinchStartDistanceRef.current = distance;
                    pinchBaseScaleRef.current = lastScaleRef.current;
                    return;
                }
                const nextScale = Math.min(4, Math.max(1, pinchBaseScaleRef.current * (distance / pinchStartDistanceRef.current)));
                currentScaleRef.current = nextScale;
                scaleValue.setValue(nextScale);
                return;
            }
            if (gestureModeRef.current === 'pinch') return;
            if (Math.abs(gestureState.dx) + Math.abs(gestureState.dy) > 4) movedRef.current = true;
            if (lastScaleRef.current > 1.01) {
                gestureModeRef.current = 'pan';
                translateXValue.setValue(lastTranslateRef.current.x + gestureState.dx);
                translateYValue.setValue(lastTranslateRef.current.y + gestureState.dy);
                return;
            }
            gestureModeRef.current = 'dismiss';
            const pullDown = Math.max(0, gestureState.dy);
            translateYValue.setValue(pullDown);
            backdropOpacity.setValue(1 - Math.min(0.7, pullDown / 320));
        },
        onPanResponderRelease: (event, gestureState) => {
            if (gestureModeRef.current === 'pinch') {
                pinchStartDistanceRef.current = null;
                lastScaleRef.current = currentScaleRef.current;
                if (lastScaleRef.current < 1.05) {
                    springTo(1, 0, 0);
                } else {
                    reportZoom();
                }
                gestureModeRef.current = 'none';
                return;
            }
            if (gestureModeRef.current === 'pan') {
                lastTranslateRef.current = {
                    x: lastTranslateRef.current.x + gestureState.dx,
                    y: lastTranslateRef.current.y + gestureState.dy,
                };
                gestureModeRef.current = 'none';
                return;
            }
            if (gestureModeRef.current === 'dismiss') {
                gestureModeRef.current = 'none';
                if (gestureState.dy > 130 || gestureState.vy > 0.9) {
                    Animated.parallel([
                        Animated.timing(translateYValue, { toValue: 620, duration: 180, useNativeDriver: true }),
                        Animated.timing(backdropOpacity, { toValue: 0, duration: 180, useNativeDriver: true }),
                    ]).start(() => onDismiss());
                    return;
                }
                springTo(lastScaleRef.current, lastTranslateRef.current.x, lastTranslateRef.current.y);
                return;
            }
        },
        onPanResponderTerminate: () => {
            pinchStartDistanceRef.current = null;
            gestureModeRef.current = 'none';
            springTo(lastScaleRef.current, lastTranslateRef.current.x, lastTranslateRef.current.y);
        },
    })).current;

    return (
        <Animated.View
            style={{ flex: 1, transform: [{ translateX: translateXValue }, { translateY: translateYValue }, { scale: scaleValue }] }}
            {...panResponder.panHandlers}
        >
            {allowTapZoom ? (
                <Pressable style={{ flex: 1 }} onPress={handleTap}>
                    {children}
                </Pressable>
            ) : children}
        </Animated.View>
    );
}
const phoneStyles = StyleSheet.create({
  root: {
    flex: 1
  },
  backdrop: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: Black
  },
  media: {
    width: '100%',
    height: '100%'
  },
  chromeBar: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0
  },
  chromeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: scaleWidth(16)
  },
  counterTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  closeBtn: {
    width: scaleWidth(36),
    height: scaleWidth(36),
    borderRadius: scaleWidth(99),
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: SemiTransparentCharcoal
  },
  saveBtn: {
    height: scaleHeight(34),
    paddingHorizontal: scaleWidth(16),
    borderRadius: scaleWidth(99),
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HappyColor
  },
  savedBtn: {
    backgroundColor: SemiTransparentCharcoal
  },
  saveBtnHidden: {
    opacity: 0
  },
  saveTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  unavailableTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    fontWeight: 600,
    textAlign: 'center',
    color: White
  }
});
const tabletStyles = StyleSheet.create({
  root: {
    flex: 1
  },
  backdrop: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: Black
  },
  media: {
    width: '100%',
    height: '100%'
  },
  chromeBar: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0
  },
  chromeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: scaleWidth(20)
  },
  counterTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  closeBtn: {
    width: scaleWidth(44),
    height: scaleWidth(44),
    borderRadius: scaleWidth(132.792),
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: SemiTransparentCharcoal
  },
  saveBtn: {
    height: scaleHeight(42),
    paddingHorizontal: scaleWidth(20),
    borderRadius: scaleWidth(132.792),
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HappyColor
  },
  savedBtn: {
    backgroundColor: SemiTransparentCharcoal
  },
  saveBtnHidden: {
    opacity: 0
  },
  saveTxt: {
    fontSize: scaleFont(17),
    lineHeight: scaleLineHeight(25.5),
    letterSpacing: scaleLetterSpacing(-0.17),
    fontWeight: 600,
    color: White
  },
  unavailableTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    fontWeight: 600,
    textAlign: 'center',
    color: White
  }
});
const MediaViewerModal = ({
  visible,
  items,
  initialIndex,
  sessionKey,
  onClose,
}) => {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const { statusBarHeight } = useSafeAreaPadding();
  const { width: windowWidth } = useWindowDimensions();
  const [saving, setSaving] = useState(false);
  const [savedKeys, setSavedKeys] = useState({});
  const [currentIndex, setCurrentIndex] = useState(0);
  const [zoomActive, setZoomActive] = useState(false);
  const backdropOpacity = useRef(new Animated.Value(1)).current;

  useEffect(() => {
    setSaving(false);
    setSavedKeys({});
    setZoomActive(false);
    setCurrentIndex(Math.max(0, initialIndex || 0));
    backdropOpacity.setValue(1);
  }, [sessionKey, backdropOpacity]);

  if (!visible || !items || items.length === 0) return null;

  const boundedIndex = Math.min(items.length - 1, Math.max(0, currentIndex));
  const currentItem = items[boundedIndex];
  const currentSaved = !!savedKeys[currentItem.key];

  const handleSave = async () => {
    if (saving || currentSaved || !currentItem.mediaUrl) return;
    if (!CameraRoll || !BlobUtil) {
      showToast('Saving is unavailable on this build', 'info');
      return;
    }
    if (Platform.OS === 'android' && Platform.Version < 29) {
      const granted = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.WRITE_EXTERNAL_STORAGE);
      if (granted !== PermissionsAndroid.RESULTS.GRANTED) {
        showToast('Storage permission is needed to save', 'info');
        return;
      }
    }
    setSaving(true);
    const savedItemKey = currentItem.key;
    let downloadedPath = null;
    try {
      const extension = currentItem.kind === 3 ? 'mp4' : 'jpg';
      const response = await BlobUtil.config({ fileCache: true, appendExt: extension }).fetch('GET', baseService.getMediaUrl(currentItem.mediaUrl));
      downloadedPath = response.path();
      const fileUri = downloadedPath.startsWith('file://') ? downloadedPath : 'file://' + downloadedPath;
      await CameraRoll.save(fileUri, { type: currentItem.kind === 3 ? 'video' : 'photo' });
      setSavedKeys((current) => ({ ...current, [savedItemKey]: true }));
      showToast('Saved to Photos', 'info');
    } catch (error) {
      showToast("Couldn't save that", 'info');
    } finally {
      if (downloadedPath && BlobUtil.fs) {
        BlobUtil.fs.unlink(downloadedPath).catch(() => {});
      }
      setSaving(false);
    }
  };

  const renderPage = ({ item }) => (
    <View style={{ width: windowWidth, height: '100%' }}>
      <ZoomableMedia
        allowTapZoom={item.kind === 2}
        backdropOpacity={backdropOpacity}
        onDismiss={onClose}
        onZoomChange={setZoomActive}
      >
        {item.kind === 2 ? (
          item.localUri ? (
            <Image source={{ uri: item.localUri }} style={styles.media} resizeMode="contain" fadeDuration={0} />
          ) : (
            <RemoteImage uri={item.mediaUrl} style={styles.media} resizeMode="contain" />
          )
        ) : Video ? (
          <Video
            source={{ uri: item.localUri ? item.localUri : baseService.getMediaUrl(item.mediaUrl) }}
            style={styles.media}
            controls={item.key === currentItem.key}
            paused={item.key !== currentItem.key}
            resizeMode="contain"
            onError={() => {}}
          />
        ) : (
          <CustomText style={styles.unavailableTxt}>Video playback is unavailable on this build</CustomText>
        )}
      </ZoomableMedia>
    </View>
  );

  return (
    <Modal visible transparent animationType="fade" onRequestClose={onClose}>
      <View style={styles.root}>
        <Animated.View style={[styles.backdrop, { opacity: backdropOpacity }]} />
        <FlatList
          key={'viewer-' + String(sessionKey)}
          data={items}
          horizontal
          pagingEnabled
          showsHorizontalScrollIndicator={false}
          keyExtractor={(item) => item.key}
          renderItem={renderPage}
          initialScrollIndex={Math.min(items.length - 1, Math.max(0, initialIndex || 0))}
          getItemLayout={(data, index) => ({ length: windowWidth, offset: windowWidth * index, index })}
          scrollEnabled={!zoomActive}
          onMomentumScrollEnd={(event) => {
            const nextIndex = Math.round(event.nativeEvent.contentOffset.x / windowWidth);
            setCurrentIndex(Math.min(items.length - 1, Math.max(0, nextIndex)));
          }}
          onScrollToIndexFailed={() => {}}
        />
        <LinearGradient
          colors={['rgba(0,0,0,0.55)', 'rgba(0,0,0,0)']}
          style={[styles.chromeBar, { height: statusBarHeight + scaleHeight(64) }]}
          pointerEvents="box-none"
        >
          <View style={[styles.chromeRow, { marginTop: statusBarHeight + scaleHeight(8) }]} pointerEvents="box-none">
            <TouchableOpacity style={styles.closeBtn} onPress={onClose}>
              <CloseGlyph size={scaleWidth(16)} color={White} />
            </TouchableOpacity>
            {items.length > 1 && (
              <CustomText style={styles.counterTxt}>{(boundedIndex + 1) + ' of ' + items.length}</CustomText>
            )}
            <TouchableOpacity
              style={[styles.saveBtn, currentSaved && styles.savedBtn, !currentItem.mediaUrl && styles.saveBtnHidden]}
              onPress={handleSave}
              disabled={saving || currentSaved || !currentItem.mediaUrl}
            >
              <CustomText style={styles.saveTxt}>{saving ? 'Saving...' : currentSaved ? 'Saved' : 'Save'}</CustomText>
            </TouchableOpacity>
          </View>
        </LinearGradient>
      </View>
    </Modal>
  );
};

export default MediaViewerModal;