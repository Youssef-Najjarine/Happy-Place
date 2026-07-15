import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList } from 'react-native';
import { useNavigation, useRoute, useFocusEffect, useIsFocused } from '@react-navigation/native';
import { useSelector } from 'react-redux';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import HelpHub from 'src/components/HelpHub';
import Avatar from 'src/components/Avatar';
import tokenStorage from 'src/services/tokenStorage';
import useHelperOffer from 'src/hooks/useHelperOffer';
import { useAvailableHelpersQuery } from 'src/store/chatGroupsApi';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import { HappyColor, White, Black, VeryLightGray, WarmIvory, Graphite, SoftRosePink, CharcoalNavy } from 'src/constants/colors';

const ReadyTint = 'rgba(237, 83, 112, 0.10)';
const PaleGray = '#EFEDE6';

function parseUtc(value) {
  if (!value) return 0;
  let text = String(value);
  if (!/[zZ]$/.test(text) && !/[+-]\d\d:?\d\d$/.test(text)) text = text + 'Z';
  const time = new Date(text).getTime();
  return Number.isFinite(time) ? time : 0;
}

function formatAgo(value) {
  const created = parseUtc(value);
  if (!created) return '';
  const seconds = Math.max(0, Math.floor((Date.now() - created) / 1000));
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return minutes + 'm ago';
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return hours + 'h ago';
  const days = Math.floor(hours / 24);
  return days + 'd ago';
}

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    height: '100%',
    width: '100%'
  },
  topNav: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    borderBottomLeftRadius: scaleWidth(24),
    borderBottomRightRadius: scaleWidth(24),
    marginBottom: scaleHeight(20),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'space-between'
  },
  greetingRow: {
    paddingHorizontal: scaleWidth(20)
  },
  greetingTitle: {
    fontSize: scaleFont(17),
    lineHeight: scaleLineHeight(25),
    letterSpacing: scaleLetterSpacing(-0.17),
    fontWeight: 700,
    color: Black
  },
  greetingSubtitle: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Graphite
  },
  loginRow: {
    height: scaleHeight(44),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  unlockAllFeaturesTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  loginView: {
    width: scaleWidth(62),
    height: scaleHeight(32)
  },
  loginBtn: {
    borderRadius: scaleWidth(99),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  loginBtnTxt: {
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontSize: scaleFont(16),
    fontWeight: 600,
    color: White
  },
  helpers: {
    paddingLeft: scaleWidth(20),
    height: scaleHeight(115),
    marginBottom: scaleHeight(16),
    width: '100%'
  },
  availableHelpersTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  helpersListContent: {
    paddingTop: scaleHeight(12),
    gap: scaleWidth(16)
  },
  helperCard: {
    width: scaleWidth(50)
  },
  helperCardBtn: {
    width: '100%',
    height: '100%',
    gap: scaleHeight(8),
    alignItems: 'center'
  },
  helperImage: {
    width: scaleWidth(50),
    height: scaleHeight(50),
    borderRadius: scaleWidth(50),
    resizeMode: 'cover'
  },
  helperName: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    height: scaleHeight(21),
    width: '100%',
    textAlign: 'center',
    color: Black
  },
  helpViewTypeWrap: {
    paddingHorizontal: scaleWidth(20),
    marginBottom: scaleHeight(12)
  },
  helpViewType: {
    paddingVertical: scaleHeight(2),
    paddingHorizontal: scaleWidth(2),
    borderRadius: scaleWidth(67.067),
    height: scaleHeight(39),
    width: '100%',
    backgroundColor: VeryLightGray,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  helpViewTypeSelectedBtn: {
    flex: 1,
    height: scaleHeight(34),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpViewTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  helpViewTypeNotSelectedBtn: {
    flex: 1,
    height: scaleHeight(35),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpViewTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: scaleWidth(20)
  },
  centeredTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 500,
    opacity: 0.6,
    textAlign: 'center'
  },
  listContent: {
    paddingHorizontal: scaleWidth(20),
    paddingBottom: scaleHeight(40),
    gap: scaleHeight(12),
    flexGrow: 1
  },
  joinCard: {
    borderRadius: scaleWidth(16),
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    width: '100%',
    gap: scaleHeight(12),
    backgroundColor: ReadyTint
  },
  readyMeta: {
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    color: HappyColor,
    fontWeight: 600
  },
  card: {
    borderRadius: scaleWidth(16),
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    width: '100%',
    gap: scaleHeight(12),
    backgroundColor: White
  },
  cardTextCol: {
    gap: scaleHeight(4)
  },
  cardTitle: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600
  },
  cardMeta: {
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    color: Black,
    fontWeight: 500,
    opacity: 0.5
  },
  actionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(10)
  },
  primaryBtn: {
    height: scaleHeight(41),
    paddingHorizontal: scaleWidth(20),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  primaryTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: White,
    fontWeight: 600
  },
  secondaryBtn: {
    height: scaleHeight(41),
    paddingHorizontal: scaleWidth(20),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: PaleGray
  },
  secondaryTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 600,
    opacity: 0.6
  },
  offeredBadge: {
    height: scaleHeight(41),
    paddingHorizontal: scaleWidth(18),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: SoftRosePink
  },
  offeredBadgeTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: HappyColor,
    fontWeight: 700
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: scaleHeight(40),
    paddingHorizontal: scaleWidth(20),
    gap: scaleHeight(8)
  },
  emptyIconCircle: {
    width: scaleWidth(72),
    height: scaleWidth(72),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: scaleHeight(4),
    backgroundColor: SoftRosePink
  },
  emptyIcon: {
    width: scaleWidth(34),
    height: scaleHeight(34),
    resizeMode: 'contain'
  },
  emptyTitle: {
    fontSize: scaleFont(17),
    lineHeight: scaleLineHeight(25.5),
    letterSpacing: scaleLetterSpacing(-0.17),
    color: Black,
    fontWeight: 700,
    textAlign: 'center'
  },
  emptyBtnView: {
    marginTop: scaleHeight(16),
    height: scaleHeight(48),
    paddingHorizontal: scaleWidth(28),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  emptyBtnTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    height: '100%',
    width: '100%'
  },
  topNav: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    borderBottomLeftRadius: scaleWidth(24),
    borderBottomRightRadius: scaleWidth(24),
    marginBottom: scaleHeight(20),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'space-between'
  },
  greetingRow: {
    paddingHorizontal: scaleWidth(24)
  },
  greetingTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 700,
    color: Black
  },
  greetingSubtitle: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: Graphite
  },
  loginRow: {
    height: 84,
    paddingHorizontal: scaleWidth(24),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  unlockAllFeaturesTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  loginView: {
    width: scaleWidth(79.192),
    height: scaleHeight(40.73067)
  },
  loginBtn: {
    borderRadius: scaleWidth(132.792),
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  loginBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  helpers: {
    paddingLeft: scaleWidth(24),
    height: scaleHeight(174),
    marginBottom: scaleHeight(20),
    width: '100%'
  },
  availableHelpersTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  helpersListContent: {
    paddingTop: scaleHeight(16.16),
    gap: scaleWidth(24)
  },
  helperCard: {
    width: scaleWidth(67.067)
  },
  helperCardBtn: {
    width: '100%',
    height: '100%',
    gap: scaleHeight(10.73),
    alignItems: 'center'
  },
  helperImage: {
    width: 93.03,
    height: 93.03,
    borderRadius: scaleWidth(67.067),
    resizeMode: 'cover'
  },
  helperName: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    height: scaleHeight(24),
    width: '100%',
    textAlign: 'center',
    color: Black
  },
  helpViewTypeWrap: {
    paddingHorizontal: scaleWidth(24),
    marginBottom: scaleHeight(12)
  },
  helpViewType: {
    paddingVertical: scaleHeight(4),
    paddingHorizontal: scaleWidth(4),
    borderRadius: scaleWidth(132.792),
    height: scaleHeight(56.34),
    width: '100%',
    backgroundColor: VeryLightGray,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  helpViewTypeSelectedBtn: {
    flex: 1,
    height: scaleHeight(48.34),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpViewTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  helpViewTypeNotSelectedBtn: {
    flex: 1,
    height: scaleHeight(48.34),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center'
  },
  helpViewTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: CharcoalNavy
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: scaleWidth(24)
  },
  centeredTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    fontWeight: 500,
    opacity: 0.6,
    textAlign: 'center'
  },
  listContent: {
    paddingHorizontal: scaleWidth(24),
    paddingBottom: scaleHeight(48),
    gap: scaleHeight(14),
    flexGrow: 1
  },
  joinCard: {
    borderRadius: scaleWidth(18),
    paddingVertical: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    gap: scaleHeight(14),
    backgroundColor: ReadyTint
  },
  readyMeta: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: HappyColor,
    fontWeight: 600
  },
  card: {
    borderRadius: scaleWidth(18),
    paddingVertical: scaleHeight(20),
    paddingHorizontal: scaleWidth(20),
    width: '100%',
    gap: scaleHeight(14),
    backgroundColor: White
  },
  cardTextCol: {
    gap: scaleHeight(5)
  },
  cardTitle: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    fontWeight: 600
  },
  cardMeta: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: Black,
    fontWeight: 500,
    opacity: 0.5
  },
  actionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(12)
  },
  primaryBtn: {
    height: scaleHeight(48),
    paddingHorizontal: scaleWidth(24),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  primaryTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: White,
    fontWeight: 600
  },
  secondaryBtn: {
    height: scaleHeight(48),
    paddingHorizontal: scaleWidth(24),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: PaleGray
  },
  secondaryTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 600,
    opacity: 0.6
  },
  offeredBadge: {
    height: scaleHeight(48),
    paddingHorizontal: scaleWidth(22),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: SoftRosePink
  },
  offeredBadgeTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 700
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: scaleHeight(48),
    paddingHorizontal: scaleWidth(24),
    gap: scaleHeight(10)
  },
  emptyIconCircle: {
    width: scaleWidth(88),
    height: scaleWidth(88),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: scaleHeight(6),
    backgroundColor: SoftRosePink
  },
  emptyIcon: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    resizeMode: 'contain'
  },
  emptyTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 700,
    textAlign: 'center'
  },
  emptyBtnView: {
    marginTop: scaleHeight(20),
    height: scaleHeight(56),
    paddingHorizontal: scaleWidth(36),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  emptyBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: White,
    fontWeight: 600
  }
});

export default function Help() {
  const navigation = useNavigation();
  const route = useRoute();
  const user = useSelector((state) => state.user);
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const { statusBarHeight } = useSafeAreaPadding();
  const [helpViewType, setSelectedHelpView] = useState('needsHelp');
  const [helperListening, setHelperListening] = useState(false);
  const [helpViewRequested, setHelpViewRequested] = useState(false);
  const [authToken, setAuthToken] = useState(tokenStorage.peekToken());
  useEffect(() => {
    const unsubscribe = tokenStorage.subscribe((token) => setAuthToken(token));
    return unsubscribe;
  }, []);
  useEffect(() => {
    if (route.params?.helpView) {
      setSelectedHelpView(route.params.helpView);
      setHelpViewRequested(true);
      navigation.setParams({ helpView: null });
    }
  }, [route.params?.helpView, navigation]);
  const isFocused = useIsFocused();
  const helpersPollingInterval = isFocused ? 5000 : 0;
  const { data: availableHelpersData, refetch: refetchAvailableHelpers } = useAvailableHelpersQuery(authToken, { skip: !authToken, pollingInterval: helpersPollingInterval });
  useFocusEffect(
    useCallback(() => {
      if (!authToken) return;
      refetchAvailableHelpers();
    }, [authToken, refetchAvailableHelpers])
  );
  const availableHelpers = availableHelpersData || [];
  const { phase, startedGroups, openRequests, offer, withdraw, decline, join, declineInvite } = useHelperOffer();
  const offeredRequests = openRequests.filter((request) => request.offerStatus === 'offered');
  const showHelperViews = helperListening || helpViewRequested || offeredRequests.length > 0 || startedGroups.length > 0;
  const handleLoginPressIn = useCallback(() => {
    navigation.navigate('LoginOptions');
  }, [navigation]);
  const handleOpenHelperProfile = useCallback((username) => {
    if (!username) return;
    navigation.push('Profile', { username });
  }, [navigation]);
  const renderHelper = useCallback(({ item }) => (
    <View style={styles.helperCard}>
      <TouchableOpacity
        style={styles.helperCardBtn}
        disabled={!item.username || item.isAnonymous}
        onPress={() => handleOpenHelperProfile(item.username)}
      >
        <Avatar
          uri={item.profilePhotoUrl}
          color={item.avatarColor}
          initial={(item.name || '?').charAt(0).toUpperCase()}
          style={styles.helperImage}
          initialStyle={{ color: White, fontSize: scaleFont(18), fontWeight: '600' }}
        />
        <CustomText style={styles.helperName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
      </TouchableOpacity>
    </View>
  ), [styles, handleOpenHelperProfile]);
  const renderRequest = useCallback(({ item }) => {
    const offered = item.offerStatus === 'offered';
    return (
      <View style={styles.card}>
        <View style={styles.cardTextCol}>
          <CustomText style={styles.cardTitle} numberOfLines={2} ellipsizeMode="tail">{item.chatGroupName}</CustomText>
          <CustomText style={styles.cardMeta}>{formatAgo(item.createdAtUtc)}</CustomText>
        </View>
        <View style={styles.actionRow}>
          {offered ? (
            <>
              <View style={styles.offeredBadge}>
                <CustomText style={styles.offeredBadgeTxt}>Offered</CustomText>
              </View>
              <TouchableOpacity style={styles.secondaryBtn} onPress={() => withdraw(item.chatGroupId)}>
                <CustomText style={styles.secondaryTxt}>Withdraw</CustomText>
              </TouchableOpacity>
            </>
          ) : (
            <>
              <TouchableOpacity style={styles.primaryBtn} onPress={() => offer(item.chatGroupId, item.chatGroupName)}>
                <CustomText style={styles.primaryTxt}>Offer to help</CustomText>
              </TouchableOpacity>
              <TouchableOpacity style={styles.secondaryBtn} onPress={() => decline(item.chatGroupId)}>
                <CustomText style={styles.secondaryTxt}>Not interested</CustomText>
              </TouchableOpacity>
            </>
          )}
        </View>
      </View>
    );
  }, [styles, offer, withdraw, decline]);
  const renderJoinCard = useCallback(({ item }) => (
    <View style={styles.joinCard}>
      <View style={styles.cardTextCol}>
        <CustomText style={styles.cardTitle} numberOfLines={2} ellipsizeMode="tail">{item.chatGroupName}</CustomText>
        <CustomText style={styles.readyMeta}>Ready to join</CustomText>
      </View>
      <View style={styles.actionRow}>
        <TouchableOpacity style={styles.primaryBtn} onPress={() => join(item.chatGroupId)}>
          <CustomText style={styles.primaryTxt}>Join</CustomText>
        </TouchableOpacity>
        <TouchableOpacity style={styles.secondaryBtn} onPress={() => declineInvite(item.chatGroupId)}>
          <CustomText style={styles.secondaryTxt}>Decline</CustomText>
        </TouchableOpacity>
      </View>
    </View>
  ), [styles, join, declineInvite]);
  const renderEmpty = useCallback(() => {
    const emptyTitle = helpViewType === 'needsHelp'
      ? 'No one needs help right now'
      : helpViewType === 'offered' ? 'You haven\u2019t offered to help anyone yet' : 'Nothing is ready to join yet';
    return (
      <View style={styles.emptyState}>
        <View style={styles.emptyIconCircle}>
          <HappyEmoji {...styles.emptyIcon} />
        </View>
        <CustomText style={styles.emptyTitle}>{emptyTitle}</CustomText>
        <TouchableOpacity style={styles.emptyBtnView} onPress={() => navigation.navigate('ChatGroups')}>
          <CustomText style={styles.emptyBtnTxt}>Browse conversations</CustomText>
        </TouchableOpacity>
      </View>
    );
  }, [styles, helpViewType, navigation]);
  const displayedData = helpViewType === 'needsHelp' ? openRequests : helpViewType === 'offered' ? offeredRequests : startedGroups;
  const renderItem = helpViewType === 'ready' ? renderJoinCard : renderRequest;
  const topNavStyle = useMemo(
    () => ({ ...styles.topNav, paddingTop: statusBarHeight }),
    [styles.topNav, statusBarHeight]
  );

  return (
    <View style={styles.root}>
      <View style={topNavStyle}>
        {user.isLoggedIn ? (
          <View style={styles.greetingRow}>
            <CustomText style={styles.greetingTitle} numberOfLines={1}>
              {user.displayName ? `Hi ${user.displayName}!` : 'Hi there!'}
              <CustomText style={styles.greetingSubtitle}> What do you need today?</CustomText>
            </CustomText>
          </View>
        ) : (
          <View style={styles.loginRow}>
            <View>
              <CustomText style={styles.unlockAllFeaturesTxt}>Login to unlock all features!</CustomText>
            </View>
            <View style={styles.loginView}>
              <TouchableOpacity
                style={styles.loginBtn}
                onPressIn={handleLoginPressIn}
              >
                <CustomText style={styles.loginBtnTxt}>Login</CustomText>
              </TouchableOpacity>
            </View>
          </View>
        )}
        <HelpHub onListeningChange={setHelperListening} />
      </View>
      {availableHelpers.length > 0 && (
        <View style={styles.helpers}>
          <CustomText style={styles.availableHelpersTxt}>Available Helpers</CustomText>
          <FlatList
            data={availableHelpers}
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={styles.helpersListContent}
            keyExtractor={(item) => item.id}
            renderItem={renderHelper}
            horizontal
          />
        </View>
      )}
      {showHelperViews && (
        <>
        <View style={styles.helpViewTypeWrap}>
          <View style={styles.helpViewType}>
            <TouchableOpacity
              style={helpViewType === 'needsHelp' ? styles.helpViewTypeSelectedBtn : styles.helpViewTypeNotSelectedBtn}
              onPress={() => setSelectedHelpView('needsHelp')}
            >
                <CustomText style={helpViewType === 'needsHelp' ? styles.helpViewTypeSelectedtxt : styles.helpViewTypeNotSelectedTxt}>Needs Help ({openRequests.length})</CustomText>
            </TouchableOpacity>
            <TouchableOpacity
              style={helpViewType === 'offered' ? styles.helpViewTypeSelectedBtn : styles.helpViewTypeNotSelectedBtn}
              onPress={() => setSelectedHelpView('offered')}
            >
                <CustomText style={helpViewType === 'offered' ? styles.helpViewTypeSelectedtxt : styles.helpViewTypeNotSelectedTxt}>Offered ({offeredRequests.length})</CustomText>
            </TouchableOpacity>
            <TouchableOpacity
              style={helpViewType === 'ready' ? styles.helpViewTypeSelectedBtn : styles.helpViewTypeNotSelectedBtn}
              onPress={() => setSelectedHelpView('ready')}
            >
                <CustomText style={helpViewType === 'ready' ? styles.helpViewTypeSelectedtxt : styles.helpViewTypeNotSelectedTxt}>Ready ({startedGroups.length})</CustomText>
            </TouchableOpacity>
          </View>
        </View>
        {phase === 'loading' ? (
          <View style={styles.centered}>
            <CustomText style={styles.centeredTxt}>{'Finding people who need help\u2026'}</CustomText>
          </View>
        ) : (
          <FlatList
            style={{ flex: 1 }}
            data={displayedData}
            keyExtractor={(item) => item.chatGroupId}
            renderItem={renderItem}
            ListEmptyComponent={renderEmpty}
            contentContainerStyle={styles.listContent}
            showsVerticalScrollIndicator={false}
          />
        )}
        </>
      )}
    </View>
  );
}