import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList, Pressable, ActivityIndicator } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute, useIsFocused, useFocusEffect } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'store/loadingSlice';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import {
  HappyColor,
  White,
  Black,
  VeryLightGray,
  SoftGray,
  VeryLightLavenderTint,
  TranslucentBlack,
  CharcoalNavy,
  SoftRosePink
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import Avatar from 'src/components/Avatar';
import UnfriendModal from 'src/components/UnfriendModal';
import { showToast } from 'src/components/Toast';
import tokenStorage from 'src/services/tokenStorage';
import {
  useListFriendsPageQuery,
  useLazyListFriendsPageQuery,
  useListIncomingRequestsQuery,
  useListOutgoingRequestsQuery,
  useSendFriendRequestMutation,
  useCancelFriendRequestMutation,
  useAcceptFriendRequestMutation,
  useDeclineFriendRequestMutation,
  useUnfriendMutation,
} from 'store/friendsApi';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import SearchIcon from 'assets/images/global/search-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import ProfileIcon from 'assets/images/friends/profile-black-icon.svg';
import ChatIcon from 'assets/images/friends/chat-bubble-icon.svg';
import UnfriendIcon from 'assets/images/friends/unfriend-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import RedXIcon from 'assets/images/global/red-x-icon.svg';
import PlusIcon from 'assets/images/global/white-plus-icon.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';

const ActiveIndexContext = React.createContext(-1);
const stylesActive = StyleSheet.create({
  zLift: { zIndex: 1000, elevation: 1000, overflow: 'visible' },
});

function ActiveListCell({ children, index, style, ...props }) {
  const activeIndex = React.useContext(ActiveIndexContext);
  const isActive = index === activeIndex;
  return (
    <View {...props} style={[style, isActive ? stylesActive.zLift : null]}>
      {children}
    </View>
  );
}

const STATUS_LABELS = {
  self: 'You',
  friends: 'Friends',
  requestSent: 'Requested',
  requestReceived: 'Requested you',
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
  friendsHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
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
  friendsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  headerTitleTxt: {
    maxWidth: scaleWidth(170),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  addFriend: {
    width: scaleWidth(126),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99)
  },
  addFriendBtn: {
      borderRadius: scaleWidth(99),
      gap: scaleWidth(4),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  plusIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24)
  },
  addFriendTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  headerActions: {
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  headerEllipsisWrap: {
    position: 'relative'
  },
  headerDropdown: {
    top: scaleHeight(50),
    right: 0,
    width: scaleWidth(161),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowRadius: scaleWidth(15),
    shadowOffset: {
      width: scaleWidth(8),
      height: scaleHeight(8)
    },
    shadowOpacity: 1,
    shadowColor: VeryLightLavenderTint,
    elevation: 12,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 3000,
  },
  friendsViewType: {
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
  friendsViewTypeSelectedBtn: {
    flex: 1,
    height: scaleHeight(34),
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  friendsViewTypeSelectedtxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  friendsViewTypeNotSelectedBtn: {
    flex: 1,
    height: scaleHeight(35),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center'
  },
  friendsViewTypeNotSelectedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
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
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(16),
    paddingBottom: scaleHeight(110)
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
    width: scaleWidth(153),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(153),
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
  ellipsisBackground: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    backgroundColor: VeryLightGray,
    justifyContent: 'center',
    alignItems: 'center'
  },
  ellipsis: {
    width: scaleWidth(28),
    height: scaleHeight(28)
  },
  friendDropdown: {
    top: scaleHeight(21),
    right: scaleWidth(20),
    width: scaleWidth(161),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowRadius: scaleWidth(15),
    shadowOffset: {
      width: scaleWidth(8),
      height: scaleHeight(8)
    },
    shadowOpacity: 1,
    shadowColor: VeryLightLavenderTint,
    elevation: 12,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000,
  },
  dropdownIcons: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  friendDropdownOptions: {
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(10),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendDropdownOptionsBorderBottom: {
    borderBottomWidth: scaleHeight(0.5),
    borderBottomColor: TranslucentBlack
  },
  unfriendOption: {
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(10.5),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownBlackTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: Black
  },
  dropdownRedTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: HappyColor
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
  pageLoadingFooter: {
    paddingVertical: scaleHeight(16),
    alignItems: 'center'
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
  friendsHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
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
  friendsTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  headerTitleTxt: {
    maxWidth: scaleWidth(320),
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  addFriend: {
    width: scaleWidth(168.43533),
    height: scaleHeight(56.336),
    borderRadius: scaleWidth(132.792)
  },
  addFriendBtn: {
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
  addFriendTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  headerActions: {
    gap: scaleWidth(10.73),
    flexDirection: 'row',
    alignItems: 'center'
  },
  headerEllipsisWrap: {
    position: 'relative'
  },
  headerDropdown: {
    top: scaleHeight(86),
    right: 0,
    width: scaleWidth(215.977),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowRadius: scaleWidth(20.119),
    shadowOffset: {
      width: scaleWidth(10.73),
      height: scaleHeight(10.73)
    },
    shadowOpacity: 1,
    shadowColor: VeryLightLavenderTint,
    elevation: 12,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 3000,
  },
  friendsViewType: {
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
  friendsViewTypeSelectedBtn: {
    flex: 1,
    height: scaleHeight(48.34),
    borderRadius: scaleWidth(132.792),
    backgroundColor: HappyColor,
    justifyContent: 'center',
    alignItems: 'center'
  },
  friendsViewTypeSelectedtxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  friendsViewTypeNotSelectedBtn: {
    flex: 1,
    height: scaleHeight(48.34),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center'
  },
  friendsViewTypeNotSelectedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: CharcoalNavy
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
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(130)
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
    width: scaleWidth(56.34),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(67.07)
  },
  friendPhoto: {
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
  friendFullName: {
    width: scaleWidth(400),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(400),
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
  ellipsisBackground: {
    width: scaleWidth(56.34),
    height: scaleHeight(56.34),
    borderRadius: scaleWidth(132.792),
    backgroundColor: VeryLightGray,
    justifyContent: 'center',
    alignItems: 'center'
  },
  ellipsis: {
    width: scaleWidth(37.56),
    height: scaleHeight(37.56)
  },
  friendDropdown: {
    top: scaleHeight(28.17),
    right: scaleWidth(26.83),
    width: scaleWidth(215.977),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowRadius: scaleWidth(20.119),
    shadowOffset: {
      width: scaleWidth(10.73),
      height: scaleHeight(10.73)
    },
    shadowOpacity: 1,
    shadowColor: VeryLightLavenderTint,
    elevation: 12,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000,
  },
  dropdownIcons: {
    width: scaleWidth(32.19),
    height: scaleHeight(32.19),
    resizeMode: 'contain'
  },
  friendDropdownOptions: {
    paddingHorizontal: scaleWidth(21.46),
    paddingVertical: scaleHeight(13.41),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendDropdownOptionsBorderBottom: {
    borderBottomWidth: scaleHeight(0.67),
    borderBottomColor: TranslucentBlack
  },
  unfriendOption: {
    paddingHorizontal: scaleWidth(21.46),
    paddingVertical: scaleHeight(14.08),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownBlackTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 500,
    color: Black
  },
  dropdownRedTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 500,
    color: HappyColor
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
  pageLoadingFooter: {
    paddingVertical: scaleHeight(16),
    alignItems: 'center'
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
export default function Friends() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const dispatch = useDispatch();
  const isFocused = useIsFocused();

  const routeUsername = route.params?.username || null;
  const routeDisplayName = route.params?.displayName || null;
  const isOwnMode = !routeUsername;
  const isTabInstance = route.name === 'MyFriends';
  const bottomInsetHeight = isTabInstance ? 0 : bottomSafeHeight;

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

  const [friendsViewType, setSelectedFriendType] = useState('friends');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);
  const friendsSearchArg = friendsViewType === 'friends' ? debouncedSearch.trim() : '';

  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const [headerMenuOpen, setHeaderMenuOpen] = useState(false);
  const [unfriendTarget, setUnfriendTarget] = useState(null);
  const ellipsisRefs = useRef([]);
  const friendsRef = useRef(null);
  const friendsDropdownRef = useRef(null);
  const headerEllipsisRef = useRef(null);
  const headerDropdownRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    friendsDropdown: null,
    ellipsisBtn: null,
    headerDropdown: null,
    headerEllipsisBtn: null,
  });

  const listPollingInterval = isFocused ? 5000 : 0;
  const {
    data: friendsPage,
    isSuccess: friendsQuerySucceeded,
    isError: friendsQueryErrored,
    error: friendsQueryError,
    refetch: refetchFriends
  } = useListFriendsPageQuery(
    { authToken, username: isOwnMode ? null : routeUsername, search: friendsSearchArg, cursor: null },
    { skip: !authToken, pollingInterval: listPollingInterval }
  );
  const [fetchNextFriendsPage, { isFetching: isFetchingNextFriendsPage }] = useLazyListFriendsPageQuery();
  const {
    data: incomingData,
    refetch: refetchIncoming
  } = useListIncomingRequestsQuery(authToken, { skip: !authToken || !isOwnMode, pollingInterval: listPollingInterval });
  const {
    data: outgoingData,
    refetch: refetchOutgoing
  } = useListOutgoingRequestsQuery(authToken, { skip: !authToken || !isOwnMode, pollingInterval: listPollingInterval });

  const [sendFriendRequest] = useSendFriendRequestMutation();
  const [cancelFriendRequest] = useCancelFriendRequestMutation();
  const [acceptFriendRequest] = useAcceptFriendRequestMutation();
  const [declineFriendRequest] = useDeclineFriendRequestMutation();
  const [unfriend] = useUnfriendMutation();

  useFocusEffect(
    useCallback(() => {
      if (!authToken) return;
      refetchFriends();
      if (isOwnMode) {
        refetchIncoming();
        refetchOutgoing();
      }
    }, [authToken, isOwnMode, refetchFriends, refetchIncoming, refetchOutgoing])
  );

  const [hasLoadedFriends, setHasLoadedFriends] = useState(false);
  const friendsResolved = friendsQuerySucceeded || friendsQueryErrored;
  useEffect(() => {
    if (friendsResolved && !hasLoadedFriends) setHasLoadedFriends(true);
  }, [friendsResolved, hasLoadedFriends]);
  useEffect(() => {
    if (hasLoadedFriends || friendsResolved) return;
    dispatch(showLoading());
    return () => dispatch(hideLoading());
  }, [hasLoadedFriends, friendsResolved, dispatch]);

  const friendItems = friendsPage?.items || [];
  const friendsTotalCount = friendsPage?.totalCount ?? 0;
  const incomingRequests = incomingData?.requests || [];
  const outgoingRequests = outgoingData?.requests || [];
  const notFound = friendsQueryErrored && friendsQueryError?.status === 404;
  const connectionFailed = friendItems.length === 0 && friendsQueryErrored && !notFound;

  const clientFilter = useCallback((rows) => {
    const trimmedQuery = search.trim().toLowerCase();
    if (!trimmedQuery) return rows;
    return rows.filter((row) =>
      (row.displayName || '').toLowerCase().includes(trimmedQuery) ||
      (row.username || '').toLowerCase().includes(trimmedQuery)
    );
  }, [search]);
  const displayedRequests = useMemo(() => clientFilter(incomingRequests), [clientFilter, incomingRequests]);
  const displayedSent = useMemo(() => clientFilter(outgoingRequests), [clientFilter, outgoingRequests]);
  const displayedData = friendsViewType === 'friends' ? friendItems : friendsViewType === 'requests' ? displayedRequests : displayedSent;

  const closeAllMenus = useCallback(() => {
    setActiveDropdownIndex(null);
    setHeaderMenuOpen(false);
  }, []);
  const handleFriendsScroll = useCallback(() => {
    if (activeDropdownIndex !== null || headerMenuOpen) closeAllMenus();
  }, [activeDropdownIndex, headerMenuOpen, closeAllMenus]);
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always', onScrollBeginDrag: closeAllMenus }),
    [closeAllMenus]
  );
  const handleEllipsisPress = useCallback((index) => {
    swallowNextCloseRef.current = true;
    setHeaderMenuOpen(false);
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
  }, []);
  const handleHeaderEllipsisPress = useCallback(() => {
    swallowNextCloseRef.current = true;
    setActiveDropdownIndex(null);
    setHeaderMenuOpen((curr) => !curr);
  }, []);
  const handleSearchFocusOrTouch = useCallback(() => {
    closeAllMenus();
  }, [closeAllMenus]);
  const pointInRect = useCallback((x, y, r) => {
    return !!r && x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height;
  }, []);
  const handleRootTouchEndCapture = useCallback((e) => {
    if (swallowNextCloseRef.current) {
      swallowNextCloseRef.current = false;
      return;
    }
    if (activeDropdownIndex === null && !headerMenuOpen) return;
    const { pageX: x, pageY: y } = e.nativeEvent;
    const { friendsDropdown, ellipsisBtn, headerDropdown, headerEllipsisBtn } = rectsRef.current;
    if (
      pointInRect(x, y, friendsDropdown) ||
      pointInRect(x, y, ellipsisBtn) ||
      pointInRect(x, y, headerDropdown) ||
      pointInRect(x, y, headerEllipsisBtn)
    ) {
      return;
    }
    closeAllMenus();
  }, [activeDropdownIndex, headerMenuOpen, closeAllMenus, pointInRect]);
  const measureToRect = useCallback((ref, key) => {
    if (!ref?.current) {
      rectsRef.current[key] = null;
      return;
    }
    ref.current.measureInWindow((x, y, width, height) => {
      rectsRef.current[key] = { x, y, width, height };
    });
  }, []);

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
  const handleCancelSent = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await cancelFriendRequest({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, cancelFriendRequest, handleActionResult, handleActionError]);
  const handleAddOther = useCallback(async (username) => {
    if (!authToken) return;
    try {
      const data = await sendFriendRequest({ authToken, username }).unwrap();
      if (handleActionResult(data)) refetchFriends();
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, sendFriendRequest, handleActionResult, handleActionError, refetchFriends]);
  const handleConfirmUnfriend = useCallback(async () => {
    const username = unfriendTarget;
    setUnfriendTarget(null);
    if (!authToken || !username) return;
    try {
      const data = await unfriend({ authToken, username }).unwrap();
      handleActionResult(data);
    } catch (error) {
      handleActionError(error);
    }
  }, [authToken, unfriendTarget, unfriend, handleActionResult, handleActionError]);

  const handleViewProfile = useCallback((username) => {
    closeAllMenus();
    navigation.push('Profile', { username });
  }, [closeAllMenus, navigation]);
  const handleOpenBlockedUsers = useCallback(() => {
    closeAllMenus();
    navigation.navigate('BlockedUsers');
  }, [closeAllMenus, navigation]);
  const handleEndReached = useCallback(() => {
    if (friendsViewType !== 'friends') return;
    const nextCursor = friendsPage?.nextCursor;
    if (!authToken || !nextCursor || isFetchingNextFriendsPage) return;
    fetchNextFriendsPage({ authToken, username: isOwnMode ? null : routeUsername, search: friendsSearchArg, cursor: nextCursor });
  }, [friendsViewType, friendsPage?.nextCursor, authToken, isFetchingNextFriendsPage, fetchNextFriendsPage, isOwnMode, routeUsername, friendsSearchArg]);

  const friendsListContent = useMemo(() => ({
    ...styles.friendsListContent,
    paddingBottom: bottomInsetHeight + styles.friendsListContent.paddingBottom,
  }), [styles.friendsListContent, bottomInsetHeight]);
  useEffect(() => {
    ellipsisRefs.current = Array(displayedData.length)
      .fill(null)
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [displayedData.length]);
  useEffect(() => {
    if (activeDropdownIndex !== null) {
      const ellipsisRef = ellipsisRefs.current[activeDropdownIndex];
      requestAnimationFrame(() => {
        if (ellipsisRef?.current) {
          ellipsisRef.current.measureInWindow((x, y, width, height) => {
            rectsRef.current.ellipsisBtn = { x, y, width, height };
          });
        }
        if (friendsDropdownRef.current) {
          measureToRect(friendsDropdownRef, 'friendsDropdown');
        }
      });
    } else {
      rectsRef.current.ellipsisBtn = null;
      rectsRef.current.friendsDropdown = null;
    }
  }, [activeDropdownIndex, measureToRect]);
  useEffect(() => {
    if (headerMenuOpen) {
      requestAnimationFrame(() => {
        measureToRect(headerEllipsisRef, 'headerEllipsisBtn');
        measureToRect(headerDropdownRef, 'headerDropdown');
      });
    } else {
      rectsRef.current.headerEllipsisBtn = null;
      rectsRef.current.headerDropdown = null;
    }
  }, [headerMenuOpen, measureToRect]);
  useEffect(() => {
    closeAllMenus();
  }, [friendsViewType, closeAllMenus]);

  const renderAvatar = useCallback((item) => (
    <View style={styles.friendImage}>
      <Avatar
        uri={item.profilePhotoUrl}
        color={item.avatarColor}
        initial={(item.displayName || item.username || '?')[0].toUpperCase()}
        style={styles.friendPhoto}
        initialStyle={styles.avatarInitialTxt}
      />
    </View>
  ), [styles]);

  const renderFriend = useCallback(({ item, index }) => {
    const isActive = activeDropdownIndex === index;
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          {renderAvatar(item)}
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View>
          <TouchableOpacity
            ref={(ref) => (ellipsisRefs.current[index] = ref)}
            style={styles.ellipsisBackground}
            onPressIn={() => handleEllipsisPress(index)}
          >
            <EllipsisIcon {...styles.ellipsis} />
          </TouchableOpacity>
        </View>
        {isActive && (
          <Pressable
            ref={friendsDropdownRef}
            onLayout={() => measureToRect(friendsDropdownRef, 'friendsDropdown')}
            style={styles.friendDropdown}
          >
            <TouchableOpacity
              onPressIn={() => { swallowNextCloseRef.current = true; }}
              onPressOut={() => handleViewProfile(item.username)}
              style={[styles.friendDropdownOptions,styles.friendDropdownOptionsBorderBottom]}
            >
              <CustomText style={styles.dropdownBlackTxt}>View Profile</CustomText>
              <ProfileIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
            <TouchableOpacity
            onPressIn={() => { swallowNextCloseRef.current = true; }}
            onPressOut={closeAllMenus}
            style={[styles.friendDropdownOptions, styles.friendDropdownOptionsBorderBottom]}
            >
            <CustomText style={styles.dropdownBlackTxt}>Message</CustomText>
            <ChatIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
            <TouchableOpacity
            onPressIn={() => { swallowNextCloseRef.current = true; }}
            onPressOut={() => { closeAllMenus(); setUnfriendTarget(item.username); }}
            style={styles.unfriendOption}
            >
                <CustomText style={styles.dropdownRedTxt}>Unfriend</CustomText>
                <UnfriendIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
          </Pressable>
        )}
      </View>
    );
  }, [activeDropdownIndex, styles, closeAllMenus, renderAvatar, handleEllipsisPress, handleViewProfile, measureToRect]);

  const renderOtherFriend = useCallback(({ item }) => {
    return (
      <TouchableOpacity style={styles.friendCard} onPress={() => handleViewProfile(item.username)}>
        <View style={styles.friendImageAndName}>
          {renderAvatar(item)}
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View>
          {item.friendshipStatus === 'none' ? (
            <TouchableOpacity style={styles.acceptBtn} onPress={() => handleAddOther(item.username)}>
              <CustomText style={styles.acceptTxt}>Add</CustomText>
            </TouchableOpacity>
          ) : (
            <CustomText style={styles.statusLabelTxt}>{STATUS_LABELS[item.friendshipStatus] || ''}</CustomText>
          )}
        </View>
      </TouchableOpacity>
    );
  }, [styles, renderAvatar, handleViewProfile, handleAddOther]);

  const renderRequest = useCallback(({ item }) => {
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          {renderAvatar(item)}
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View style={styles.requestOptions}>
            <TouchableOpacity style={styles.acceptBtn} onPress={() => handleAccept(item.username)}>
                <CustomText style={styles.acceptTxt}>Accept</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.xBtn} onPress={() => handleDecline(item.username)}>
                <XIcon {...styles.xIcon}/>
            </TouchableOpacity>
        </View>
      </View>
    );
  }, [styles, renderAvatar, handleAccept, handleDecline]);

  const renderSent = useCallback(({ item }) => {
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          {renderAvatar(item)}
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View>
          <TouchableOpacity style={styles.cancelRequestBtn} onPress={() => handleCancelSent(item.username)}>
            <CustomText style={styles.cancelRequestTxt}>Cancel Request</CustomText>
          </TouchableOpacity>
        </View>
      </View>
    );
  }, [styles, renderAvatar, handleCancelSent]);

  const renderPageLoadingFooter = useCallback(() => {
    if (!isFetchingNextFriendsPage || friendsViewType !== 'friends') return null;
    return (
      <View style={styles.pageLoadingFooter}>
        <ActivityIndicator color={HappyColor} />
      </View>
    );
  }, [isFetchingNextFriendsPage, friendsViewType, styles]);

  const renderEmpty = useCallback(() => {
    if (!hasLoadedFriends) return null;
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
          <TouchableOpacity style={styles.emptyStateRetryBtn} onPress={refetchFriends}>
            <CustomText style={styles.emptyStateRetryTxt}>Retry</CustomText>
          </TouchableOpacity>
        </View>
      );
    }
    const isSearching = search.trim().length > 0;
    let title = 'No friends yet';
    let subtitle = 'Tap Add Friend to find people you know.';
    if (!isOwnMode) {
      title = 'No friends yet';
      subtitle = 'This user has not added any friends.';
    }
    if (friendsViewType === 'requests') {
      title = 'No pending requests';
      subtitle = 'Friend requests you receive will show up here.';
    }
    if (friendsViewType === 'sent') {
      title = 'No sent requests';
      subtitle = 'Requests you send will show up here until they are answered.';
    }
    if (isSearching) {
      title = 'No matches';
      subtitle = 'Try a different name or username.';
    }
    return (
      <View style={styles.emptyState}>
        <View style={styles.emptyStateIconCircle}>
          <HappyEmoji {...styles.emptyStateIcon} />
        </View>
        <CustomText style={styles.emptyStateTitle}>{title}</CustomText>
        <CustomText style={styles.emptyStateSubtitle}>{subtitle}</CustomText>
      </View>
    );
  }, [hasLoadedFriends, connectionFailed, styles, refetchFriends, search, friendsViewType, isOwnMode]);

  const rootStyle = {
  ...styles.root,
  paddingTop: statusBarHeight + styles.root.paddingTop
  };

  if (notFound) {
    return (
      <View style={rootStyle}>
        <View style={styles.topNav}>
          <View style={styles.friendsHeaderRow}>
            <View style={styles.backArrowAndfriendsRow}>
              <View>
                <TouchableOpacity
                  style={styles.BackArrow}
                  onPress={() => navigation.goBack()}
                >
                  <BackArrow {...styles.backArrowIcon}/>
                </TouchableOpacity>
              </View>
            </View>
          </View>
        </View>
        <View style={styles.emptyState}>
          <View style={styles.emptyStateIconCircle}>
            <SadEmoji {...styles.emptyStateIcon} />
          </View>
          <CustomText style={styles.emptyStateTitle}>Unable to load friends</CustomText>
          <CustomText style={styles.emptyStateSubtitle}>This list is not available.</CustomText>
        </View>
      </View>
    );
  }

  const headerTitle = isOwnMode ? `Friends (${friendsTotalCount})` : (routeDisplayName || `@${routeUsername}`);
  const renderItem = friendsViewType === 'friends'
    ? (isOwnMode ? renderFriend : renderOtherFriend)
    : friendsViewType === 'requests' ? renderRequest : renderSent;

  return (
    <View style={rootStyle} onTouchEndCapture={handleRootTouchEndCapture}>
        <View style={styles.topNav}>
            <View style={styles.friendsHeaderRow}>
                <View style={styles.backArrowAndfriendsRow}>
                    {!isTabInstance && (
                        <View>
                            <TouchableOpacity
                                style={styles.BackArrow}
                                onPress={() => navigation.goBack()}
                            >
                                <BackArrow {...styles.backArrowIcon}/>
                            </TouchableOpacity>
                        </View>
                    )}
                    <View>
                        <CustomText style={styles.headerTitleTxt} numberOfLines={1} ellipsizeMode="tail">{headerTitle}</CustomText>
                    </View>
                </View>
                {isOwnMode && (
                  <View style={styles.headerActions}>
                    <View style={styles.addFriend}>
                        <TouchableOpacity
                            style={styles.addFriendBtn}
                            onPress={() => navigation.navigate('AddFriends')}
                        >
                            <PlusIcon {...styles.plusIcon}/>
                            <CustomText style={styles.addFriendTxt}>Add Friend</CustomText>
                        </TouchableOpacity>
                    </View>
                    <View style={styles.headerEllipsisWrap}>
                        <TouchableOpacity
                            ref={headerEllipsisRef}
                            style={styles.ellipsisBackground}
                            onPressIn={handleHeaderEllipsisPress}
                        >
                            <EllipsisIcon {...styles.ellipsis} />
                        </TouchableOpacity>
                        {headerMenuOpen && (
                          <Pressable
                            ref={headerDropdownRef}
                            onLayout={() => measureToRect(headerDropdownRef, 'headerDropdown')}
                            style={styles.headerDropdown}
                          >
                            <TouchableOpacity
                              onPressIn={() => { swallowNextCloseRef.current = true; }}
                              onPressOut={handleOpenBlockedUsers}
                              style={styles.friendDropdownOptions}
                            >
                              <CustomText style={styles.dropdownBlackTxt}>Blocked Users</CustomText>
                              <RedXIcon {...styles.dropdownIcons} />
                            </TouchableOpacity>
                          </Pressable>
                        )}
                    </View>
                  </View>
                )}
            </View>
            {isOwnMode && (
              <View style={styles.friendsViewType}>
                <TouchableOpacity
                  style={friendsViewType === 'friends' ? styles.friendsViewTypeSelectedBtn : styles.friendsViewTypeNotSelectedBtn}
                  onPress={() => {
                      setSelectedFriendType('friends');
                      setSearch('');
                  }}
                >
                    <CustomText style={friendsViewType === 'friends' ? styles.friendsViewTypeSelectedtxt : styles.friendsViewTypeNotSelectedTxt}>Friends ({friendsTotalCount})</CustomText>
                </TouchableOpacity>
                <TouchableOpacity
                  style={friendsViewType === 'requests' ? styles.friendsViewTypeSelectedBtn : styles.friendsViewTypeNotSelectedBtn}
                  onPress={() => {
                      setSelectedFriendType('requests');
                      setSearch('');
                  }}
                >
                    <CustomText style={friendsViewType === 'requests' ? styles.friendsViewTypeSelectedtxt : styles.friendsViewTypeNotSelectedTxt}>Requests ({incomingRequests.length})</CustomText>
                </TouchableOpacity>
                <TouchableOpacity
                  style={friendsViewType === 'sent' ? styles.friendsViewTypeSelectedBtn : styles.friendsViewTypeNotSelectedBtn}
                  onPress={() => {
                      setSelectedFriendType('sent');
                      setSearch('');
                  }}
                >
                    <CustomText style={friendsViewType === 'sent' ? styles.friendsViewTypeSelectedtxt : styles.friendsViewTypeNotSelectedTxt}>Sent ({outgoingRequests.length})</CustomText>
                </TouchableOpacity>
              </View>
            )}
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
                onFocus={handleSearchFocusOrTouch}
                onTouchStart={handleSearchFocusOrTouch}
                />
                <SearchIcon {...styles.searchIcon} />
            </View>
        </View>
        <View style={styles.friendsBody}>
          <ActiveIndexContext.Provider value={activeDropdownIndex}>
            <FlatList
              ref={friendsRef}
              data={displayedData}
              contentContainerStyle={friendsListContent}
              showsVerticalScrollIndicator={false}
              keyExtractor={(item) => `${friendsViewType}-${item.username}`}
              onScroll={handleFriendsScroll}
              scrollEventThrottle={16}
              removeClippedSubviews={false}
              extraData={activeDropdownIndex}
              renderItem={renderItem}
              CellRendererComponent={ActiveListCell}
              onEndReached={handleEndReached}
              onEndReachedThreshold={0.5}
              ListFooterComponent={renderPageLoadingFooter}
              ListEmptyComponent={renderEmpty}
              {...listCommonProps}
            />
          </ActiveIndexContext.Provider>
        </View>
        <UnfriendModal
          visible={unfriendTarget !== null}
          username={unfriendTarget}
          onConfirm={handleConfirmUnfriend}
          onCancel={() => setUnfriendTarget(null)}
        />
        <LinearGradient
            pointerEvents="none"
            colors={['rgba(255, 255, 255, 0.2)', 'rgba(255, 255, 255, 0.7)']}
            style={{
                position: 'absolute',
                bottom: 0,
                left: 0,
                right: 0,
                height: bottomInsetHeight + scaleHeight(50),
            }}
        />
    </View>
  );
}