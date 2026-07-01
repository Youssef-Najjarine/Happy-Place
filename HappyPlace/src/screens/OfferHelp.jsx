import React, { useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import useHelperOffer from 'src/hooks/useHelperOffer';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';

const WarmIvory = '#F9F5EA';
const SoftRosePink = 'rgba(237, 83, 112, 0.20)';
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
  header: {
    paddingHorizontal: scaleWidth(20),
    paddingTop: scaleHeight(8),
    paddingBottom: scaleHeight(12),
    gap: scaleHeight(12)
  },
  backBtn: {
    alignSelf: 'flex-start',
    paddingVertical: scaleHeight(4),
    paddingRight: scaleWidth(8)
  },
  backTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 600
  },
  title: {
    fontSize: scaleFont(28),
    lineHeight: scaleLineHeight(33.6),
    letterSpacing: scaleLetterSpacing(-0.28),
    color: Black,
    fontWeight: 800
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
  sectionLabel: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(19.5),
    letterSpacing: scaleLetterSpacing(0.2),
    color: Black,
    fontWeight: 700,
    opacity: 0.45,
    marginBottom: scaleHeight(8)
  },
  sectionLabelSpaced: {
    marginTop: scaleHeight(20)
  },
  readySection: {
    gap: scaleHeight(12)
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
  header: {
    paddingHorizontal: scaleWidth(40),
    paddingTop: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    gap: scaleHeight(14)
  },
  backBtn: {
    alignSelf: 'flex-start',
    paddingVertical: scaleHeight(6),
    paddingRight: scaleWidth(10)
  },
  backTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: HappyColor,
    fontWeight: 600
  },
  title: {
    fontSize: scaleFont(36),
    lineHeight: scaleLineHeight(43.2),
    letterSpacing: scaleLetterSpacing(-0.36),
    color: Black,
    fontWeight: 800
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: scaleWidth(80)
  },
  centeredTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 500,
    opacity: 0.6,
    textAlign: 'center'
  },
  listContent: {
    paddingHorizontal: scaleWidth(40),
    paddingBottom: scaleHeight(56),
    gap: scaleHeight(16),
    flexGrow: 1
  },
  sectionLabel: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(0.2),
    color: Black,
    fontWeight: 700,
    opacity: 0.45,
    marginBottom: scaleHeight(10)
  },
  sectionLabelSpaced: {
    marginTop: scaleHeight(28)
  },
  readySection: {
    gap: scaleHeight(16)
  },
  joinCard: {
    borderRadius: scaleWidth(21.461),
    paddingVertical: scaleHeight(20),
    paddingHorizontal: scaleWidth(22),
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
    borderRadius: scaleWidth(21.461),
    paddingVertical: scaleHeight(20),
    paddingHorizontal: scaleWidth(22),
    width: '100%',
    gap: scaleHeight(14),
    backgroundColor: White
  },
  cardTextCol: {
    gap: scaleHeight(6)
  },
  cardTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
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
    height: scaleHeight(52),
    paddingHorizontal: scaleWidth(26),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  primaryTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: White,
    fontWeight: 600
  },
  secondaryBtn: {
    height: scaleHeight(52),
    paddingHorizontal: scaleWidth(26),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: PaleGray
  },
  secondaryTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: Black,
    fontWeight: 600,
    opacity: 0.6
  },
  offeredBadge: {
    height: scaleHeight(52),
    paddingHorizontal: scaleWidth(24),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: SoftRosePink
  },
  offeredBadgeTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    color: HappyColor,
    fontWeight: 700
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: scaleHeight(56),
    paddingHorizontal: scaleWidth(40),
    gap: scaleHeight(10)
  },
  emptyIconCircle: {
    width: scaleWidth(96),
    height: scaleWidth(96),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: scaleHeight(6),
    backgroundColor: SoftRosePink
  },
  emptyIcon: {
    width: scaleWidth(44),
    height: scaleHeight(44),
    resizeMode: 'contain'
  },
  emptyTitle: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
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

export default function OfferHelp() {
  const navigation = useNavigation();
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const { phase, startedGroups, openRequests, offer, withdraw, decline, join, declineInvite } = useHelperOffer();

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

  const renderJoinCard = useCallback((group) => (
    <View key={group.chatGroupId} style={styles.joinCard}>
      <View style={styles.cardTextCol}>
        <CustomText style={styles.cardTitle} numberOfLines={2} ellipsizeMode="tail">{group.chatGroupName}</CustomText>
        <CustomText style={styles.readyMeta}>Ready to join</CustomText>
      </View>
      <View style={styles.actionRow}>
        <TouchableOpacity style={styles.primaryBtn} onPress={() => join(group.chatGroupId)}>
          <CustomText style={styles.primaryTxt}>Join</CustomText>
        </TouchableOpacity>
        <TouchableOpacity style={styles.secondaryBtn} onPress={() => declineInvite(group.chatGroupId)}>
          <CustomText style={styles.secondaryTxt}>Decline</CustomText>
        </TouchableOpacity>
      </View>
    </View>
  ), [styles, join, declineInvite]);

  const renderHeader = useCallback(() => (
    <View>
      {startedGroups.length > 0 ? (
        <View style={styles.readySection}>
          <CustomText style={styles.sectionLabel}>Ready to join</CustomText>
          {startedGroups.map(renderJoinCard)}
        </View>
      ) : null}
      <CustomText style={[styles.sectionLabel, startedGroups.length > 0 ? styles.sectionLabelSpaced : null]}>People who need help</CustomText>
    </View>
  ), [styles, startedGroups, renderJoinCard]);

  const renderEmpty = useCallback(() => (
    <View style={styles.emptyState}>
      <View style={styles.emptyIconCircle}>
        <HappyEmoji {...styles.emptyIcon} />
      </View>
      <CustomText style={styles.emptyTitle}>All quiet right now</CustomText>
      <TouchableOpacity style={styles.emptyBtnView} onPress={() => navigation.navigate('ChatGroups')}>
        <CustomText style={styles.emptyBtnTxt}>Browse conversations</CustomText>
      </TouchableOpacity>
    </View>
  ), [styles, navigation]);

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomSafeHeight
  };

  return (
    <View style={rootStyle}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backBtn} onPress={() => navigation.goBack()}>
          <CustomText style={styles.backTxt}>Back</CustomText>
        </TouchableOpacity>
        <CustomText style={styles.title}>Help others</CustomText>
      </View>
      {phase === 'loading' ? (
        <View style={styles.centered}>
          <CustomText style={styles.centeredTxt}>Finding people who need help…</CustomText>
        </View>
      ) : (
        <FlatList
          style={{ flex: 1 }}
          data={openRequests}
          keyExtractor={(item) => item.chatGroupId}
          renderItem={renderRequest}
          ListHeaderComponent={renderHeader}
          ListEmptyComponent={renderEmpty}
          contentContainerStyle={styles.listContent}
          showsVerticalScrollIndicator={false}
        />
      )}
    </View>
  );
}