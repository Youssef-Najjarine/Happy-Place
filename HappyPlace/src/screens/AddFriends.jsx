import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useIsFocused, useFocusEffect } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black, VeryLightGray, SoftRosePink } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import Avatar from 'src/components/Avatar';
import { showToast } from 'src/components/Toast';
import tokenStorage from 'src/services/tokenStorage';
import {
  useSearchUsersQuery,
  useSendFriendRequestMutation,
  useCancelFriendRequestMutation,
  useAcceptFriendRequestMutation,
  useDeclineFriendRequestMutation,
} from 'store/friendsApi';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import DownArrowIcon from 'assets/images/global/arrow-down-icon.svg';
import UpArrowIcon from 'assets/images/addFriends/arrow-up-icon.svg';
import SearchIcon from 'assets/images/global/search-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';

const STATUS_LABELS = {
  self: 'You',
  friends: 'Friends',
};

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
  backArrowAndfriendsRow: {
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
  addFriendsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  search: {
    height: scaleHeight(39),
    width: '100%'
  },
  searchIcon: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    top: scaleHeight(9),
    left: scaleWidth(10),
    position: 'absolute'
  },
  searchInput: {
    borderRadius: scaleWidth(99),
    paddingLeft: scaleWidth(38),
    paddingVertical: scaleHeight(9),
    paddingRight: scaleWidth(16),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    width: '100%',
    height: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  suggestionsAndRequests: {
    marginBottom: scaleHeight(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  suggestionsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  sentRequests: {
    width: scaleWidth(138),
    height: scaleHeight(39),
    borderRadius: scaleWidth(99)
  },
  sentRequestsBtn: {
    borderRadius: scaleWidth(99),
    gap: scaleWidth(8),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  sentRequestsTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  sentRequestArrows: {
    width: scaleWidth(20),
    height: scaleHeight(20)
  },
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(12)
  },
  friendCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendImageAndName: {
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  friendImage: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(50)
  },
  friendPhoto: {
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
  friendFullName: {
    width: scaleWidth(145),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(145),
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  statusLabelTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  cancelRequestBtn: {
    width: scaleWidth(132),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  cancelRequestTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  },
  addFriendBtn: {
    width: scaleWidth(101),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  addFriendTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  requestOptions: {
    flexDirection: 'row',
    gap: scaleWidth(8)
  },
  acceptBtn: {
    width: scaleWidth(74),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  acceptTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  xBtn: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  xIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28)
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
  backArrowAndfriendsRow: {
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
  addFriendsTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  search: {
    height: scaleHeight(51),
    width: '100%'
  },
  searchIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    top: scaleHeight(12),
    left: scaleWidth(14),
    position: 'absolute'
  },
  searchInput: {
    borderRadius: scaleWidth(132.792),
    paddingLeft: scaleWidth(46),
    paddingVertical: scaleHeight(12),
    paddingRight: scaleWidth(14),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    width: '100%',
    height: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  suggestionsAndRequests: {
    marginBottom: scaleHeight(21.46),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  suggestionsTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  sentRequests: {
    width: scaleWidth(224.435),
    height: scaleHeight(56.336),
    borderRadius: scaleWidth(132.792)
  },
  sentRequestsBtn: {
    borderRadius: scaleWidth(132.792),
    gap: scaleWidth(8.05),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  plusIcon: {
    width: scaleWidth(26.83),
    height: scaleHeight(26.83)
  },  
  sentRequestsTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },  
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(16.1)
  },
  friendCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendImageAndName: {
    gap: scaleWidth(10.73),
    flexDirection: 'row',
    alignItems: 'center'
  },
  friendImage: {
      borderRadius: scaleWidth(67.067),
    width: 78.14,
    height: 78.14
  },
  friendPhoto: {
    borderRadius: scaleWidth(67.067),
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
  friendFullName: {
    width: scaleWidth(445.35733),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(445.35733),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  statusLabelTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  cancelRequestBtn: {
    width: scaleWidth(167.192),
    height: scaleHeight(54.144),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  cancelRequestTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: HappyColor
  },
  addFriendBtn: {
    width: scaleWidth(128.192),
    height: scaleHeight(54.144),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  addFriendTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  requestOptions: {
    flexDirection: 'row',
    gap: scaleWidth(10.73)
  },
  acceptBtn: {
    width: scaleWidth(99.28),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  acceptTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  xBtn: {
    width: scaleWidth(56.34),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  xIcon: {
    width: scaleWidth(37.56),
    height: scaleHeight(37.56)
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
export default function AddFriends() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const dispatch = useDispatch();
  const isFocused = useIsFocused();

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

  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);
  const trimmedQuery = debouncedSearch.trim();
  const isSearching = trimmedQuery.length > 0;

  const [sentRequestsTop, setSentRequestsTop] = useState(false);
  const friendsRef = useRef(null);
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always' }),
    []
  );

  const listPollingInterval = isFocused ? 5000 : 0;
  const {
    data: searchData,
    isSuccess: searchQuerySucceeded,
    isError: searchQueryErrored,
    refetch: refetchSearch
  } = useSearchUsersQuery(
    { authToken, query: trimmedQuery },
    { skip: !authToken, pollingInterval: listPollingInterval }
  );
  const [sendFriendRequest] = useSendFriendRequestMutation();
  const [cancelFriendRequest] = useCancelFriendRequestMutation();
  const [acceptFriendRequest] = useAcceptFriendRequestMutation();
  const [declineFriendRequest] = useDeclineFriendRequestMutation();

  useFocusEffect(
    useCallback(() => {
      if (!authToken) return;
      refetchSearch();
    }, [authToken, refetchSearch])
  );

  const [hasLoaded, setHasLoaded] = useState(false);
  const searchResolved = searchQuerySucceeded || searchQueryErrored;
  useEffect(() => {
    if (searchResolved && !hasLoaded) setHasLoaded(true);
  }, [searchResolved, hasLoaded]);
  useEffect(() => {
    if (hasLoaded || searchResolved) return;
    dispatch(showLoading());
    return () => dispatch(hideLoading());
  }, [hasLoaded, searchResolved, dispatch]);

  const userRows = searchData?.users || [];
  const connectionFailed = userRows.length === 0 && searchQueryErrored;

  const sortedRows = useMemo(() => {
    const indexed = userRows.map((row, index) => ({ row, index }));
    indexed.sort((a, b) => {
      const aSent = a.row.friendshipStatus === 'requestSent';
      const bSent = b.row.friendshipStatus === 'requestSent';
      if (aSent !== bSent) {
        return sentRequestsTop
          ? (aSent ? -1 : 1)
          : (aSent ? 1 : -1);
      }
      return a.index - b.index;
    });
    return indexed.map((entry) => entry.row);
  }, [userRows, sentRequestsTop]);

  const handleActionResult = useCallback((data) => {
    if (data?.status === 'accountRequired') {
      showToast('Create an account to use friends', 'error');
      navigation.navigate('FinishAccount');
      return false;
    }
    return true;
  }, [navigation]);
  const handleActionError = useCallback((error) => {
    if (error?.status === 429) {
      showToast('Too many friend requests. Please try again later.', 'error');
      return;
    }
    showToast('Something went wrong. Please try again.', 'error');
  }, []);

  const handleAddFriend = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await sendFriendRequest({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, sendFriendRequest, handleActionResult, handleActionError]);
  const handleCancelRequest = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await cancelFriendRequest({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, cancelFriendRequest, handleActionResult, handleActionError]);
  const handleAccept = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await acceptFriendRequest({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, acceptFriendRequest, handleActionResult, handleActionError]);
  const handleDecline = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await declineFriendRequest({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, declineFriendRequest, handleActionResult, handleActionError]);
  const handleOpenProfile = useCallback((username) => {
    navigation.push('Profile', { username });
  }, [navigation]);

  const friendsListContent = useMemo(() => ({
    ...styles.friendsListContent,
    paddingBottom: bottomSafeHeight
  }), [styles.friendsListContent, bottomSafeHeight]);

  const renderRightControl = useCallback((item) => {
    if (item.friendshipStatus === 'requestSent') {
      return (
        <TouchableOpacity
          style={styles.cancelRequestBtn}
          onPress={() => handleCancelRequest(item.username)}
        >
          <CustomText style={styles.cancelRequestTxt}>Cancel Request</CustomText>
        </TouchableOpacity>
      );
    }
    if (item.friendshipStatus === 'requestReceived') {
      return (
        <View style={styles.requestOptions}>
          <TouchableOpacity style={styles.acceptBtn} onPress={() => handleAccept(item.username)}>
            <CustomText style={styles.acceptTxt}>Accept</CustomText>
          </TouchableOpacity>
          <TouchableOpacity style={styles.xBtn} onPress={() => handleDecline(item.username)}>
            <XIcon {...styles.xIcon}/>
          </TouchableOpacity>
        </View>
      );
    }
    if (item.friendshipStatus === 'none') {
      return (
        <TouchableOpacity
          style={styles.addFriendBtn}
          onPress={() => handleAddFriend(item.username)}
        >
          <CustomText style={styles.addFriendTxt}>Add Friend</CustomText>
        </TouchableOpacity>
      );
    }
    return (
      <CustomText style={styles.statusLabelTxt}>{STATUS_LABELS[item.friendshipStatus] || ''}</CustomText>
    );
  }, [styles, handleCancelRequest, handleAccept, handleDecline, handleAddFriend]);

  const renderFriend = useCallback(({ item }) => {
    return (
      <View style={styles.friendCard}>
        <TouchableOpacity style={styles.friendImageAndName} onPress={() => handleOpenProfile(item.username)}>
          <View style={styles.friendImage}>
            <Avatar
              uri={item.profilePhotoUrl}
              color={item.avatarColor}
              initial={(item.displayName || item.username || '?')[0].toUpperCase()}
              style={styles.friendPhoto}
              initialStyle={styles.avatarInitialTxt}
            />
          </View>
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">
              {item.displayName}
            </CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">
              @{item.username}
            </CustomText>
          </View>
        </TouchableOpacity>

        <View>
          {renderRightControl(item)}
        </View>
      </View>
    );
  }, [styles, handleOpenProfile, renderRightControl]);

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
          <TouchableOpacity style={styles.emptyStateRetryBtn} onPress={refetchSearch}>
            <CustomText style={styles.emptyStateRetryTxt}>Retry</CustomText>
          </TouchableOpacity>
        </View>
      );
    }
    const title = isSearching ? 'No matches' : 'No suggestions yet';
    const subtitle = isSearching
      ? 'Try a different name or username.'
      : 'Join a chat group to meet people you can add.';
    return (
      <View style={styles.emptyState}>
        <View style={styles.emptyStateIconCircle}>
          <HappyEmoji {...styles.emptyStateIcon} />
        </View>
        <CustomText style={styles.emptyStateTitle}>{title}</CustomText>
        <CustomText style={styles.emptyStateSubtitle}>{subtitle}</CustomText>
      </View>
    );
  }, [hasLoaded, connectionFailed, styles, refetchSearch, isSearching]);

  const rootStyle = {
  ...styles.root,
  paddingTop: statusBarHeight + styles.root.paddingTop
  };
  return (
    <View style={rootStyle}>
        <View style={styles.topNav}>
          <View>
            <View style={styles.backArrowAndfriendsRow}>
              <View>
                <TouchableOpacity
                  style={styles.BackArrow}
                  onPress={() => navigation.goBack()}
                >
                  <BackArrow {...styles.backArrowIcon}/>
                </TouchableOpacity>
              </View>
              <View>
                <CustomText style={styles.addFriendsTxt}>Add Friends</CustomText>
              </View>
            </View>
          </View>
          <View style={styles.search}>
            <CustomTextInput
              style={styles.searchInput}
              keyboardType="default"
              autoCapitalize="none"
              autoCorrect={false}
              textContentType="none"
              autoComplete="off"
              importantForAutofill="no"
              returnKeyType="search"
              value={search}
              onChangeText={setSearch}
            />
            <SearchIcon {...styles.searchIcon} />
          </View>
        </View>
        <View style={styles.suggestionsAndRequests}>
          <View>
            <CustomText style={styles.suggestionsTxt}>{isSearching ? 'Results' : 'Suggestions'}</CustomText>
          </View>
          <View style={styles.sentRequests}>
            <TouchableOpacity
              style={styles.sentRequestsBtn}
              onPress={() => setSentRequestsTop(v => !v)}
            >
              <CustomText style={styles.sentRequestsTxt}>Sent Requests</CustomText>
              {!sentRequestsTop ? (
                <DownArrowIcon {...styles.sentRequestArrows} />
              ) : (
                <UpArrowIcon {...styles.sentRequestArrows} />
              )}
            </TouchableOpacity>
          </View>
        </View>
        <View style={styles.friendsBody}>
            <FlatList
            ref={friendsRef}
            data={sortedRows}
            contentContainerStyle={friendsListContent}
            showsVerticalScrollIndicator={false}
            keyExtractor={(item) => item.username}
            renderItem={renderFriend}
            ListEmptyComponent={renderEmpty}
            {...listCommonProps}
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