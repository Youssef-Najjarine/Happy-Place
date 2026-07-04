import React, { useState, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import HelpTopicModal from 'src/components/HelpTopicModal';
import StopHelpingModal from 'src/components/StopHelpingModal';
import useSeekerSearch from 'src/hooks/useSeekerSearch';
import useHelperListen from 'src/hooks/useHelperListen';
import { HappyColor, White, Black, VeryLightGray, SoftRosePink } from 'src/constants/colors';

const phoneStyles = StyleSheet.create({
  helpView: {
    height: scaleHeight(41),
    paddingHorizontal: scaleWidth(20),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  topNavIcons: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    resizeMode: 'contain'
  },
  helpMeBtn: {
    width: scaleWidth(160),
    height: scaleHeight(41),
    borderWidth: scaleWidth(1.5),
    borderRadius: scaleWidth(99),
    gap: scaleWidth(6),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: Black,
    backgroundColor: White
  },
  helpMeTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600
  },
  iCanHelpBtn: {
    width: scaleWidth(167),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    gap: scaleWidth(6),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    backgroundColor: HappyColor
  },
  iCanHelpTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: White,
    fontWeight: 600
  },
  searchingView: {
    height: scaleHeight(42),
    paddingHorizontal: scaleWidth(20),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  searching: {
    width: scaleWidth(87),
    height: scaleHeight(29),
    borderRadius: scaleWidth(99),
    backgroundColor: SoftRosePink,
    justifyContent: 'center',
    alignItems: 'center'
  },
  searchingTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    minWidth: scaleWidth(71),
    fontWeight: 600,
    color: Black
  },
  helperStatus: {
    flex: 1,
    marginRight: scaleWidth(8),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600
  },
  waitingActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(8)
  },
  connectView: {
    width: scaleWidth(118),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor
  },
  connectBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  connectTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  },
  helpBtnView: {
    width: scaleWidth(72),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor
  },
  helpBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpBtnTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  },
  cancelView: {
    width: scaleWidth(81),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    backgroundColor: VeryLightGray
  },
  cancelBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  cancelTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  }
});

const tabletStyles = StyleSheet.create({
  helpView: {
    height: scaleHeight(50.82),
    paddingHorizontal: scaleWidth(24),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  topNavIcons: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  helpMeBtn: {
    width: scaleWidth(344),
    height: scaleHeight(50.82),
    borderWidth: scaleWidth(1),
    borderRadius: scaleWidth(99),
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderColor: Black,
    backgroundColor: White
  },
  helpMeTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  iCanHelpBtn: {
    width: scaleWidth(344),
    height: scaleHeight(50.82),
    borderRadius: scaleWidth(99),
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 0,
    backgroundColor: HappyColor
  },
  iCanHelpTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  },
  searchingView: {
    height: scaleHeight(46),
    paddingHorizontal: scaleWidth(24),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  searching: {
    width: scaleWidth(102.461),
    height: scaleHeight(34.73067),
    borderRadius: scaleWidth(132.792),
    backgroundColor: SoftRosePink,
    justifyContent: 'center',
    alignItems: 'center'
  },
  searchingTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    minWidth: scaleWidth(81),
    fontWeight: 600,
    color: Black
  },
  helperStatus: {
    flex: 1,
    marginRight: scaleWidth(12),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  waitingActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(12)
  },
  connectView: {
    width: scaleWidth(150),
    height: scaleHeight(46),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor
  },
  connectBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  connectTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: White,
    fontWeight: 600
  },
  helpBtnView: {
    width: scaleWidth(96),
    height: scaleHeight(46),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor
  },
  helpBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: White,
    fontWeight: 600
  },
  cancelView: {
    width: scaleWidth(109),
    height: scaleHeight(46),
    borderRadius: scaleWidth(132.792),
    backgroundColor: VeryLightGray
  },
  cancelBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center'
  },
  cancelTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  }
});

export default function HelpHub() {
  const navigation = useNavigation();
  const route = useRoute();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const { phase, readyHelperCount, beginSearch, connect, cancelSearch } = useSeekerSearch();
  const { listening, pendingCount, readyCount, offeredCount, startListening, stopListening, openPicker } = useHelperListen();
  const [showHelpTopic, setShowHelpTopic] = useState(false);
  const [showStopHelping, setShowStopHelping] = useState(false);
  const [dotCount, setDotCount] = useState(0);

  const showDots = ((phase === 'waiting' || showHelpTopic) && readyHelperCount === 0) || (listening && pendingCount === 0 && readyCount === 0);

  useEffect(() => {
    if (!showDots) return;
    const interval = setInterval(() => setDotCount((p) => (p + 1) % 4), 500);
    return () => clearInterval(interval);
  }, [showDots]);

  useEffect(() => {
    if (route.params?.startSearching && route.params?.searchRole === 'Seeker') {
      navigation.setParams({ startSearching: false, searchRole: null });
      setShowHelpTopic(true);
    } else if (route.params?.startHelping) {
      navigation.setParams({ startHelping: false });
      startListening();
    }
  }, [route.params, startListening]);

  const handleHelpMe = useCallback(() => {
    setShowHelpTopic(true);
  }, []);

  const handleICanHelp = useCallback(() => {
    startListening();
  }, [startListening]);

  const handleConfirmTopic = useCallback((topicText) => {
    setShowHelpTopic(false);
    beginSearch(topicText);
  }, [beginSearch]);

  const handleCancelTopic = useCallback(() => {
    setShowHelpTopic(false);
  }, []);

  const handleSeekerCancel = useCallback(() => {
    setShowHelpTopic(false);
    cancelSearch();
  }, [cancelSearch]);

  const handleHelperCancel = useCallback(() => {
    if (offeredCount > 0) {
      setShowStopHelping(true);
    } else {
      stopListening();
    }
  }, [offeredCount, stopListening]);

  const handleConfirmStopHelping = useCallback(() => {
    setShowStopHelping(false);
    stopListening();
  }, [stopListening]);

  const handleCancelStopHelping = useCallback(() => {
    setShowStopHelping(false);
  }, []);

  return (
    <>
      {(phase !== 'idle' || showHelpTopic) ? (
        <View style={styles.searchingView}>
          <View style={styles.searching}>
            <CustomText style={styles.searchingTxt}>{readyHelperCount > 0 ? `${readyHelperCount} ready` : `Searching${'.'.repeat(dotCount)}`}</CustomText>
          </View>
          <View style={styles.waitingActions}>
            {readyHelperCount > 0 ? (
              <View style={styles.connectView}>
                <TouchableOpacity style={styles.connectBtn} onPressIn={connect} disabled={phase === 'connecting'}>
                  <CustomText style={styles.connectTxt}>{phase === 'connecting' ? 'Connecting…' : 'Connect'}</CustomText>
                </TouchableOpacity>
              </View>
            ) : null}
            <View style={styles.cancelView}>
              <TouchableOpacity style={styles.cancelBtn} onPressIn={handleSeekerCancel}>
                <CustomText style={styles.cancelTxt}>Cancel</CustomText>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      ) : readyCount > 0 ? (
        <View style={styles.searchingView}>
          <CustomText style={styles.helperStatus} numberOfLines={1}>{readyCount === 1 ? '1 ready to join' : `${readyCount} ready to join`}</CustomText>
          <View style={styles.waitingActions}>
            <View style={styles.helpBtnView}>
              <TouchableOpacity style={styles.helpBtn} onPressIn={openPicker}>
                <CustomText style={styles.helpBtnTxt}>View</CustomText>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      ) : listening ? (
        <View style={styles.searchingView}>
          {pendingCount > 0 ? (
            <CustomText style={styles.helperStatus} numberOfLines={1}>{pendingCount === 1 ? '1 person needs help' : `${pendingCount} people need help`}</CustomText>
          ) : (
            <View style={styles.searching}>
              <CustomText style={styles.searchingTxt}>{`Searching${'.'.repeat(dotCount)}`}</CustomText>
            </View>
          )}
          <View style={styles.waitingActions}>
            {pendingCount > 0 ? (
              <View style={styles.helpBtnView}>
                <TouchableOpacity style={styles.helpBtn} onPressIn={openPicker}>
                  <CustomText style={styles.helpBtnTxt}>Help</CustomText>
                </TouchableOpacity>
              </View>
            ) : null}
            <View style={styles.cancelView}>
              <TouchableOpacity style={styles.cancelBtn} onPressIn={handleHelperCancel}>
                <CustomText style={styles.cancelTxt}>Cancel</CustomText>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      ) : (
        <View style={styles.helpView}>
          <TouchableOpacity style={styles.helpMeBtn} onPressIn={handleHelpMe}>
            <SadEmoji {...styles.topNavIcons} />
            <CustomText style={styles.helpMeTxt}>HELP ME</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.iCanHelpBtn} onPressIn={handleICanHelp}>
            <HappyEmoji {...styles.topNavIcons} />
            <CustomText style={styles.iCanHelpTxt}>I CAN HELP</CustomText>
          </TouchableOpacity>
        </View>
      )}
      <HelpTopicModal
        visible={showHelpTopic}
        maxLen={100}
        onConfirm={handleConfirmTopic}
        onCancel={handleCancelTopic}
      />
      <StopHelpingModal
        visible={showStopHelping}
        offeredCount={offeredCount}
        onConfirm={handleConfirmStopHelping}
        onCancel={handleCancelStopHelping}
      />
    </>
  );
}