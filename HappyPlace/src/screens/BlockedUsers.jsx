import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import {
  HappyColor,
  White,
  Black,
  VeryLightGray,
  SoftRosePink
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import Avatar from 'src/components/Avatar';
import { showToast } from 'src/components/Toast';
import tokenStorage from 'src/services/tokenStorage';
import { useListBlockedQuery, useUnblockUserMutation } from 'store/friendsApi';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';

const phoneStyles = StyleSheet.create({
  root: {
    paddingTop: scaleHeight(12),
    paddingHorizontal: scaleWidth(20),
    backgroundColor: White,
    height: '100%',
    width: '100%',
  },
  topNav: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    marginBottom: scaleHeight(20)
  },
  backArrowAndTitleRow: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center'
  },
  BackArrow: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  titleTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  listBody: {
    flex: 1
  },
  listContent: {
    gap: scaleHeight(16),
    paddingBottom: scaleHeight(110)
  },
  blockedCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  blockedImageAndName: {
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  blockedImage: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(50)
  },
  blockedPhoto: {
    borderRadius: scaleWidth(50),
    width: '100%',
    height: '100%',
    resizeMode: 'contain'
  },
  avatarInitialTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 700,
    color: White
  },
  blockedFullName: {
    width: scaleWidth(170),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  blockedUsername: {
    width: scaleWidth(170),
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  unblockBtn: {
    width: scaleWidth(94),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  unblockTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  emptyState: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: scaleWidth(40),
    paddingBottom: scaleHeight(80),
    gap: scaleHeight(16)
  },
  emptyStateIconCircle: {
    width: scaleWidth(96),
    height: scaleWidth(96),
    borderRadius: scaleWidth(99),
    backgroundColor: SoftRosePink,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: scaleHeight(4)
  },
  emptyStateIcon: {
    width: scaleWidth(44),
    height: scaleHeight(44),
    resizeMode: 'contain'
  },
  emptyStateTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 700,
    color: Black,
    textAlign: 'center'
  },
  emptyStateSubtitle: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black,
    textAlign: 'center',
    opacity: 0.6
  },
  emptyStateRetryBtn: {
    width: scaleWidth(140),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    marginTop: scaleHeight(4),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  emptyStateRetryTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  }
});
const tabletStyles = StyleSheet.create({
  root: {
    paddingTop: scaleHeight(16.1),
    paddingHorizontal: scaleWidth(26.83),
    backgroundColor: White,
    height: '100%',
    width: '100%',
  },
  topNav: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(21.46),
    marginBottom: scaleHeight(26.83)
  },
  backArrowAndTitleRow: {
    gap: scaleWidth(16.1),
    flexDirection: 'row',
    alignItems: 'center'
  },
  BackArrow: {
    borderRadius: scaleWidth(132.792),
    width: 78.14,
    height: 78.14,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  backArrowIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557),
  },
  titleTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  listBody: {
    flex: 1
  },
  listContent: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(130)
  },
  blockedCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  blockedImageAndName: {
    gap: scaleWidth(10.73),
    flexDirection: 'row',
    alignItems: 'center'
  },
  blockedImage: {
    width: scaleWidth(56.34),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(67.07)
  },
  blockedPhoto: {
    borderRadius: scaleWidth(67.07),
    width: '100%',
    height: '100%',
    resizeMode: 'contain'
  },
  avatarInitialTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 700,
    color: White
  },
  blockedFullName: {
    width: scaleWidth(430),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  blockedUsername: {
    width: scaleWidth(430),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  unblockBtn: {
    width: scaleWidth(126.13),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  unblockTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  emptyState: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: scaleWidth(80),
    paddingBottom: scaleHeight(100),
    gap: scaleHeight(20)
  },
  emptyStateIconCircle: {
    width: scaleWidth(140),
    height: scaleWidth(140),
    borderRadius: scaleWidth(132.792),
    backgroundColor: SoftRosePink,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: scaleHeight(6)
  },
  emptyStateIcon: {
    width: scaleWidth(64),
    height: scaleHeight(64),
    resizeMode: 'contain'
  },
  emptyStateTitle: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    letterSpacing: scaleLetterSpacing(-0.26),
    fontWeight: 700,
    color: Black,
    textAlign: 'center'
  },
  emptyStateSubtitle: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black,
    textAlign: 'center',
    opacity: 0.6
  },
  emptyStateRetryBtn: {
    width: scaleWidth(200),
    height: scaleHeight(50.82),
    borderRadius: scaleWidth(132.792),
    marginTop: scaleHeight(6),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  emptyStateRetryTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  }
});
export default function BlockedUsers() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const dispatch = useDispatch();

  const [authToken, setAuthToken] = useState(tokenStorage.peekToken());
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const token = await tokenStorage.getToken();
      if (!cancelled && token) setAuthToken(token);
    })();
    const unsubscribe = tokenStorage.subscribe((token) => {
      if (!cancelled) setAuthToken(token);
    });
    return () => { cancelled = true; unsubscribe(); };
  }, []);

  const {
    data: blockedData,
    isSuccess: blockedQuerySucceeded,
    isError: blockedQueryErrored,
    refetch: refetchBlocked
  } = useListBlockedQuery(authToken, { skip: !authToken });
  const [unblockUser] = useUnblockUserMutation();

  useFocusEffect(
    useCallback(() => {
      if (!authToken) return;
      refetchBlocked();
    }, [authToken, refetchBlocked])
  );

  const [hasLoaded, setHasLoaded] = useState(false);
  const blockedResolved = blockedQuerySucceeded || blockedQueryErrored;
  useEffect(() => {
    if (blockedResolved && !hasLoaded) setHasLoaded(true);
  }, [blockedResolved, hasLoaded]);
  useEffect(() => {
    if (hasLoaded || blockedResolved) return;
    dispatch(showLoading());
    return () => dispatch(hideLoading());
  }, [hasLoaded, blockedResolved, dispatch]);

  const blockedUsers = blockedData?.blockedUsers || [];
  const connectionFailed = blockedUsers.length === 0 && blockedQueryErrored;

  const handleUnblock = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await unblockUser({ authToken, username }).unwrap();
      if (data?.status === 'accountRequired') {
        showToast('Create an account to manage blocked users', 'error');
        navigation.navigate('FinishAccount');
      }
    } catch {
      showToast('Something went wrong. Please try again.', 'error');
    }
  }, [authToken, unblockUser, navigation]);

  const listContent = useMemo(() => ({
    ...styles.listContent,
    paddingBottom: bottomSafeHeight + styles.listContent.paddingBottom,
  }), [styles.listContent, bottomSafeHeight]);

  const renderBlocked = useCallback(({ item }) => {
    return (
      <View style={styles.blockedCard}>
        <View style={styles.blockedImageAndName}>
          <View style={styles.blockedImage}>
            <Avatar
              uri={item.profilePhotoUrl}
              color={item.avatarColor}
              initial={(item.displayName || item.username || '?')[0].toUpperCase()}
              style={styles.blockedPhoto}
              initialStyle={styles.avatarInitialTxt}
            />
          </View>
          <View>
            <CustomText style={styles.blockedFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.blockedUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View>
          <TouchableOpacity style={styles.unblockBtn} onPress={() => handleUnblock(item.username)}>
            <CustomText style={styles.unblockTxt}>Unblock</CustomText>
          </TouchableOpacity>
        </View>
      </View>
    );
  }, [styles, handleUnblock]);

  const renderEmpty = useCallback(() => {
    if (!hasLoaded) return null;
    if (connectionFailed) {
      return (
        <View style={styles.emptyState}>
          <View style={styles.emptyStateIconCircle}>
            <SadEmoji {...styles.emptyStateIcon} />
          </View>
          <CustomText style={styles.emptyStateTitle}>Can't connect right now</CustomText>
          <CustomText style={styles.emptyStateSubtitle}>
            Check your internet connection and try again.
          </CustomText>
          <TouchableOpacity style={styles.emptyStateRetryBtn} onPress={refetchBlocked}>
            <CustomText style={styles.emptyStateRetryTxt}>Retry</CustomText>
          </TouchableOpacity>
        </View>
      );
    }
    return (
      <View style={styles.emptyState}>
        <View style={styles.emptyStateIconCircle}>
          <HappyEmoji {...styles.emptyStateIcon} />
        </View>
        <CustomText style={styles.emptyStateTitle}>No blocked users</CustomText>
        <CustomText style={styles.emptyStateSubtitle}>
          People you block will show up here.
        </CustomText>
      </View>
    );
  }, [hasLoaded, connectionFailed, styles, refetchBlocked]);

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight + styles.root.paddingTop
  };

  return (
    <View style={rootStyle}>
        <View style={styles.topNav}>
            <View style={styles.backArrowAndTitleRow}>
                <View>
                    <TouchableOpacity
                        style={styles.BackArrow}
                        onPress={() => navigation.goBack()}
                    >
                        <BackArrow {...styles.backArrowIcon}/>
                    </TouchableOpacity>
                </View>
                <View>
                    <CustomText style={styles.titleTxt}>Blocked Users ({blockedUsers.length})</CustomText>
                </View>
            </View>
        </View>
        <View style={styles.listBody}>
            <FlatList
              data={blockedUsers}
              contentContainerStyle={listContent}
              showsVerticalScrollIndicator={false}
              keyExtractor={(item) => item.username}
              renderItem={renderBlocked}
              ListEmptyComponent={renderEmpty}
              keyboardShouldPersistTaps="always"
            />
        </View>
        <LinearGradient
            pointerEvents="none"
            colors={['rgba(255, 255, 255, 0.2)', 'rgba(255, 255, 255, 0.7)']}
            style={{
                position: 'absolute',
                bottom: 0,
                left: 0,
                right: 0,
                height: bottomSafeHeight + scaleHeight(50),
            }}
        />
    </View>
  );
}