import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute, useFocusEffect } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import HelpHub from 'src/components/HelpHub';
import AccountBar from 'src/components/AccountBar';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  SoftGray, 
  VividBlueViolet, 
  WarmIvory, 
  SoftRosePink, 
  VeryLightLavenderTint,
  TranslucentBlack,
  Rosewater
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import EditChatNameModal from 'src/components/EditChatNameModal';
import DeleteChatGroupModal from 'src/components/DeleteChatGroupModal';
import LeaveChatGroupModal from 'src/components/LeaveChatGroupModal';
import LeaveGroupOwnerOptionsModal from 'src/components/LeaveGroupOwnerOptionsModal';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import LinkIcon from 'assets/images/chatGroups/share-chat-link-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import { SearchAndSortChatGroupsBar, SearchAndSortChatGroupsDropdown } from 'src/components/SearchAndSortChatGroups';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import PrivateIcon from 'assets/images/global/private-chat-icon.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';
import Avatar from 'src/components/Avatar';
import tokenStorage from 'src/services/tokenStorage';
import authenticationService from 'src/services/authenticationService';
import { showToast } from 'src/components/Toast';
import {
  useListChatGroupsQuery,
  useAvailableHelpersQuery,
  useRenameChatGroupMutation,
  useSetChatGroupVisibilityMutation,
  useDeleteChatGroupMutation,
  useLeaveChatGroupMutation,
  useRequestToJoinChatGroupMutation,
  useCancelJoinRequestMutation,
  useJoinPublicChatGroupMutation,
} from 'src/store/chatGroupsApi';
import { useDispatch } from 'react-redux';
import { showLoading, hideLoading } from 'src/store/loadingSlice';

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

const HelperStack = React.memo(
  function HelperStack({
    helpers,
    total,
    spacing,
    maxVisible = 5,
    imageBaseStyle,
    initialStyle,
    wrapperStyle,
    stackStyle,
    extraCircleStyle,
    extraTextStyle,
    TextComponent,
  }) {
    const capped = useMemo(() => helpers.slice(0, maxVisible), [helpers, maxVisible]);
    const overflow = (total || 0) - capped.length;

    return (
      <View style={wrapperStyle}>
        <View
          style={stackStyle}
          collapsable={false}
          shouldRasterizeIOS
          renderToHardwareTextureAndroid
        >
          {capped.map((helper, i) => (
            <Avatar
              key={i}
              uri={helper.profilePhotoUrl}
              color={helper.avatarColor}
              initial={helper.initial}
              style={[imageBaseStyle, { left: i * spacing }]}
              initialStyle={initialStyle}
            />
          ))}

          {capped.length > 0 && overflow > 0 && (
            <View style={[extraCircleStyle, { left: capped.length * spacing }]}>
              <TextComponent style={extraTextStyle}>+{overflow}</TextComponent>
            </View>
          )}
        </View>
      </View>
    );
  },
  (prev, next) =>
    prev.helpers === next.helpers &&
    prev.total === next.total &&
    prev.spacing === next.spacing &&
    prev.maxVisible === next.maxVisible
);

const phoneStyles = StyleSheet.create({
  root: { 
    backgroundColor: WarmIvory, 
    height: '100%', 
    width: '100%',
    position: 'relative'
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
  mainContent: { 
    flex: 1 
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
  ChatGroups: { 
    paddingHorizontal: scaleWidth(20), 
    width: '100%', 
    flex: 1 
  },
  chatGroupsListContent: { 
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(100),
    width: '100%',
    flexGrow: 1
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
  },
  chatGroupCard: {
    minHeight: scaleHeight(163),
    borderRadius: scaleWidth(16),
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    justifyContent: 'space-between',
    width: '100%',
    backgroundColor: White
  },
  chatGroupCardJoinedBorder: { 
    borderWidth: scaleWidth(1), 
    borderColor: HappyColor 
  },
  chatGroupHelpersWrapper: { 
    height: scaleHeight(36), 
    overflow: 'visible' 
  },
  chatGroupHelpersStack: { 
    height: scaleHeight(36), 
    position: 'relative', 
    overflow: 'visible' 
  },
  memberCountWrapper: {
    height: scaleHeight(36),
    justifyContent: 'center'
  },
  memberCountText: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: '600',
    opacity: 0.6,
    color: Black
  },
  chatGroupHelperImage: {
    width: scaleWidth(36),
    height: scaleHeight(36),
    borderWidth: scaleWidth(2),
    borderRadius: scaleWidth(50),
    position: 'absolute',
    top: 0,
    borderColor: White,
    backgroundColor: White,
    resizeMode: 'contain'
  },
  extraHelpersCircle: {
    width: scaleWidth(36),
    height: scaleWidth(36),
    borderRadius: scaleWidth(50),
    borderWidth: scaleWidth(2),
    marginLeft: -scaleWidth(2),
    borderColor: White,
    backgroundColor: Black,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 0
  },
  extraHelpersText: { 
    fontSize: scaleFont(12), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    color: White, 
    fontWeight: 600 
  },
  chatPhotosHeader: { 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    width: '100%' 
  },
  joinedEllipsisView: { 
    width: scaleWidth(108), 
    gap: scaleWidth(8), 
    flexDirection: 'row', 
    justifyContent: 'flex-end', 
    alignItems: 'center' 
  },
  publicCircle: { 
    width: scaleWidth(60), 
    height: scaleHeight(29), 
    borderRadius: scaleWidth(99), 
    justifyContent: 'center',
    alignItems: 'center', 
    backgroundColor: VeryLightGray 
  },
  publicAndPrivateLabel: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: Black 
  },
  privateCircle: { 
    width: scaleWidth(67), 
    height: scaleHeight(29), 
    borderRadius: scaleWidth(99), 
    justifyContent: 'center', 
    alignItems: 'center', 
    backgroundColor: SoftRosePink 
  },
  ellipsisBackground: { 
    width: scaleWidth(36), 
    height: scaleHeight(36), 
    borderRadius: scaleWidth(99), 
    backgroundColor: VeryLightGray, 
    justifyContent: 'center', 
    alignItems: 'center' 
  },
  ellipsis: { 
    width: scaleWidth(28), 
    height: scaleHeight(28) 
  },
  chatGroupDropdown: {
    top: scaleHeight(37),
    right: scaleWidth(30),
    width: scaleWidth(210),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowRadius: scaleWidth(15),
    shadowOffset: { 
      width: scaleWidth(8), 
      height: scaleHeight(8) 
    },
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    shadowColor: VeryLightLavenderTint,
    shadowOpacity: 1,
    elevation: 12,
    zIndex: 2000,
  },
  dropdownIcons: { 
    width: scaleWidth(24), 
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  chatGroupDropdownOptions: { 
    paddingHorizontal: scaleWidth(16), 
    paddingVertical: scaleHeight(10), 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    alignItems: 'center' 
  },
  chatGroupDropdownOptionsBorderBottom: { 
    borderBottomWidth: scaleHeight(0.5), 
    borderBottomColor: TranslucentBlack 
  },
  pendingMembersCircle: {
    top: scaleHeight(5), 
    right: scaleWidth(11), 
    width: scaleWidth(14), 
    height: scaleHeight(14), 
    position: 'absolute' 
  },
  deleteOption: { 
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
  chatGroupTitleView: { 
    width: '100%'
  },
  chatGroupTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  chatGroupOneBtnView: {
    height: scaleHeight(41),
    width: '100%'
  },
  chatGroupTwoBtnView: {
    height: scaleHeight(41),
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  groupChatLeaveChatBtn: {
    width: scaleWidth(128),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: Rosewater
  },
  groupChatLeaveChatTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: HappyColor, 
    fontWeight: 600 
  },
  groupChatViewChatBtn: {
    width: scaleWidth(167),
    borderRadius: scaleWidth(99),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: HappyColor
  },
  groupChatViewChatTxt: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: White 
  },
  groupChatRequestJoinBtn: { 
    borderRadius: scaleWidth(99), 
    width: '100%', 
    height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: Rosewater 
  },
  groupChatRequestJoinTxt: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: HappyColor 
  },
  groupChatCancelRequestBtn: {
    borderRadius: scaleWidth(99), 
    width: '100%', 
    height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: VeryLightGray 
  },
  groupChatCancelRequestTxt: {
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: Black
  },
  groupChatJoinNowBtn: { 
    borderRadius: scaleWidth(99), 
    width: '100%', height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: HappyColor 
  },
  groupChatJoinNowTxt: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: White 
  },
  activeListCell: {
    zIndex: 1000,
    elevation: 1000,
    overflow: 'visible'
  }
});

const tabletStyles = StyleSheet.create({
  root: { 
    backgroundColor: WarmIvory, 
    height: '100%', 
    width: '100%',
    position: 'relative'
  },
  topNav: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(20),
    borderBottomLeftRadius: scaleWidth(32.192),
    borderBottomRightRadius: scaleWidth(32.192),
    marginBottom: scaleHeight(26.83),
    width: '100%',
    backgroundColor: White,
    justifyContent: 'space-between'
  },
  mainContent: { 
    flex: 1 
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
  ChatGroups: { 
    paddingHorizontal: scaleWidth(24), 
    width: '100%', 
    flex: 1 
  },
  chatGroupsListContent: { 
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(140),
    width: '100%',
    flexGrow: 1
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
  },
  chatGroupCard: {
    minHeight: scaleHeight(212.11467),
    borderRadius: scaleWidth(21.461),
    paddingVertical: scaleHeight(24),
    paddingHorizontal: scaleWidth(24),
    justifyContent: 'space-between',
    width: '100%',
    backgroundColor: White
  },
  chatGroupCardJoinedBorder: { 
    borderWidth: scaleWidth(1.341), 
    borderColor: HappyColor 
  },
  chatGroupHelpersWrapper: { 
    height: scaleHeight(48.28799), 
    overflow: 'visible' 
  },
  chatGroupHelpersStack: { 
    height: 66.98, 
    position: 'relative', 
    overflow: 'visible' 
  },
  memberCountWrapper: {
    height: scaleHeight(48.28799),
    justifyContent: 'center'
  },
  memberCountText: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: '600',
    opacity: 0.6,
    color: Black
  },
  chatGroupHelperImage: {
    width: 66.98,
    height: 66.98,
    borderWidth: scaleWidth(2.683),
    borderRadius: scaleWidth(67.067),
    position: 'absolute',
    top: 0,
    borderColor: White,
    backgroundColor: White,
    resizeMode: 'contain'
  },
  extraHelpersCircle: {
    width: 66.98,
    height: 66.98,
    borderRadius: scaleWidth(67.067),
    borderWidth: scaleWidth(2.683),
    marginLeft: -scaleWidth(3),
    borderColor: White,
    backgroundColor: Black,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 0
  },
  extraHelpersText: { 
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(24), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    color: White, 
    fontWeight: 600 
  },
  chatPhotosHeader: { 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    width: '100%' 
  },
  joinedEllipsisView: { 
    width: scaleWidth(133.48), 
    gap: scaleWidth(12), 
    flexDirection: 'row', 
    justifyContent: 'flex-end', 
    alignItems: 'center' 
  },
  publicCircle: { 
    width: scaleWidth(73.192), 
    height: scaleHeight(34.73067), 
    borderRadius: scaleWidth(132.792), 
    justifyContent: 'center',
    alignItems: 'center', 
    backgroundColor: VeryLightGray 
  },
  publicAndPrivateLabel: { 
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(24), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    fontWeight: 600, 
    color: Black 
  },
  privateCircle: { 
    width: scaleWidth(81.192), 
    height: scaleHeight(34.73067), 
    borderRadius: scaleWidth(132.792), 
    justifyContent: 'center', 
    alignItems: 'center', 
    backgroundColor: SoftRosePink 
  },
  ellipsisBackground: { 
    width: 66.98, 
    height: 66.98, 
    borderRadius: scaleWidth(132.792), 
    backgroundColor: VeryLightGray, 
    justifyContent: 'center', 
    alignItems: 'center' 
  },
  ellipsis: { 
    width: scaleWidth(37.55733), 
    height: scaleHeight(37.55733) 
  },
  chatGroupDropdown: {
    top: scaleHeight(51.07),
    right: scaleWidth(42.83),
    width: scaleWidth(241.44),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: VividBlueViolet, 
    shadowOpacity: 0.10,
    shadowOffset: {
      width: scaleWidth(10.731),
      height: scaleHeight(10.731),
    },
    shadowRadius: scaleWidth(40.24),
    elevation: 16,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000,
  },
  dropdownIcons: { 
    width: scaleWidth(32.192), 
    height: scaleHeight(32.192),
    resizeMode: 'contain'
  },
  chatGroupDropdownOptions: { 
    paddingHorizontal: scaleWidth(21.46), 
    paddingVertical: scaleHeight(13.41), 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    alignItems: 'center' 
  },
  chatGroupDropdownOptionsBorderBottom: { 
    borderBottomWidth: scaleHeight(0.671), 
    borderBottomColor: TranslucentBlack 
  },
  pendingMembersCircle: {
    top: scaleHeight(6.71), 
    right: scaleWidth(14.76), 
    width: scaleWidth(18.779), 
    height: scaleHeight(18.779), 
    position: 'absolute' 
  },
  deleteOption: { 
    paddingHorizontal: scaleWidth(21.46), 
    paddingVertical: scaleHeight(14.08), 
    flexDirection: 'row', 
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownBlackTxt: {
    fontSize: scaleFont(18), 
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black
  },
  dropdownRedTxt: { 
    fontSize: scaleFont(18), 
    lineHeight: scaleLineHeight(27), 
    letterSpacing: scaleLetterSpacing(-0.18), 
    fontWeight: 500,
    color: HappyColor
  },
  chatGroupTitleView: { 
    width: '100%'
  },
  chatGroupTitle: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    color: Black,
    fontWeight: 600
  },
  chatGroupOneBtnView: {
    height: scaleHeight(50.82667),
    width: '100%'
  },
  chatGroupTwoBtnView: {
    height: scaleHeight(50.82667),
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  groupChatLeaveChatBtn: {
    width: scaleWidth(316),
    borderRadius: scaleWidth(132.792),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: Rosewater
  },
  groupChatLeaveChatTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor, 
    fontWeight: 600 
  },
  groupChatViewChatBtn: {
    width: scaleWidth(316),
    borderRadius: scaleWidth(132.792),
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: HappyColor
  },
  groupChatViewChatTxt: { 
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(24), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    fontWeight: 600, 
    color: White 
  },
  groupChatRequestJoinBtn: { 
    borderRadius: scaleWidth(132.792), 
    width: '100%', height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: Rosewater 
  },
  groupChatRequestJoinTxt: { 
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(24), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    fontWeight: 600, 
    color: HappyColor 
  },
  groupChatCancelRequestBtn: {
    borderRadius: scaleWidth(132.792), 
    width: '100%', 
    height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: VeryLightGray 
  },
  groupChatCancelRequestTxt: {
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(24), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    fontWeight: 600, 
    color: Black
  }, 
  groupChatJoinNowBtn: { 
    borderRadius: scaleWidth(132.792), 
    width: '100%', height: '100%', 
    justifyContent: 'center', 
    alignItems: 'center', 
    borderColor: 'none', 
    backgroundColor: HappyColor 
  },
  groupChatJoinNowTxt: { 
    fontSize: scaleFont(16), 
    lineHeight: scaleLineHeight(22), 
    letterSpacing: scaleLetterSpacing(-0.16), 
    fontWeight: 600, 
    color: White 
  },
  activeListCell: {
    zIndex: 1000,
    elevation: 1000,
    overflow: 'visible'
  }
});

export default function ChatGroups() {
  const route = useRoute();
  const navigation = useNavigation();
  const dispatch = useDispatch();
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const { width } = useWindowDimensions();
  const isTablet = width >= tabletBreakpoint;
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const [authToken, setAuthToken] = useState(tokenStorage.peekToken());
  const [bootstrapFailed, setBootstrapFailed] = useState(false);
  const [bootstrapAttempt, setBootstrapAttempt] = useState(0);
  useEffect(() => {
    let cancelled = false;
    (async () => {
      let token = await tokenStorage.getToken();
      if (token && !cancelled) {
        try {
          const validation = await authenticationService.validateToken(token);
          if (validation.status === 401) {
            await tokenStorage.clearToken();
            token = null;
          }
        } catch {}
      }
      while (!cancelled && !token) {
        try {
          token = await tokenStorage.ensureGuestToken();
        } catch {}
        if (!token && !cancelled) {
          setBootstrapFailed(true);
          await new Promise((resolve) => setTimeout(resolve, 5000));
          token = await tokenStorage.getToken();
        }
      }
      if (!cancelled && token) setAuthToken(token);
    })();
    const unsubscribe = tokenStorage.subscribe((token) => {
      if (!cancelled) setAuthToken(token);
    });
    return () => { cancelled = true; unsubscribe(); };
  }, [bootstrapAttempt]);
  const [sortBy, setSortBy] = useState('Latest');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);
  const { data: chatGroupsData, isSuccess: chatGroupsQuerySucceeded, isError: chatGroupsQueryErrored, refetch: refetchChatGroups } = useListChatGroupsQuery({ authToken, sortBy, search: debouncedSearch }, { skip: !authToken, pollingInterval: 5000 });
  const { data: availableHelpersData, refetch: refetchAvailableHelpers } = useAvailableHelpersQuery(authToken, { skip: !authToken, pollingInterval: 5000 });
  const [displayedGroups, setDisplayedGroups] = useState([]);
  const [hasLoadedChatGroups, setHasLoadedChatGroups] = useState(false);
  const chatGroupsResolved = chatGroupsQuerySucceeded || chatGroupsQueryErrored || bootstrapFailed;
  useEffect(() => {
    if (chatGroupsData !== undefined) setDisplayedGroups(chatGroupsData);
  }, [chatGroupsData]);
  useEffect(() => {
    if (chatGroupsResolved && !hasLoadedChatGroups) setHasLoadedChatGroups(true);
  }, [chatGroupsResolved, hasLoadedChatGroups]);
  const isChatGroupsInitialLoading = !hasLoadedChatGroups;
  useEffect(() => {
    if (!isChatGroupsInitialLoading || chatGroupsResolved) return;
    dispatch(showLoading());
    return () => dispatch(hideLoading());
  }, [isChatGroupsInitialLoading, chatGroupsResolved, dispatch]);
  const connectionFailed = displayedGroups.length === 0 && (chatGroupsQueryErrored || (bootstrapFailed && !authToken));
  useFocusEffect(
    useCallback(() => {
      if (!authToken) return;
      refetchChatGroups();
      refetchAvailableHelpers();
    }, [authToken, refetchChatGroups, refetchAvailableHelpers])
  );
  const handleRetryConnection = useCallback(() => {
    if (authToken) {
      refetchChatGroups();
      refetchAvailableHelpers();
      return;
    }
    setBootstrapAttempt((attempt) => attempt + 1);
  }, [authToken, refetchChatGroups, refetchAvailableHelpers]);
  const hasShownConnectionToastRef = useRef(false);
  useEffect(() => {
    if (connectionFailed && !hasShownConnectionToastRef.current) {
      hasShownConnectionToastRef.current = true;
      showToast('Couldn\u2019t reach the server \u2014 tap to try again', 'info', { label: 'Retry', onPress: handleRetryConnection }, 'chat-groups-connection');
    }
    if (!connectionFailed) {
      hasShownConnectionToastRef.current = false;
    }
  }, [connectionFailed, handleRetryConnection]);
  const helpersTop = availableHelpersData || [];
  const [renameChatGroup] = useRenameChatGroupMutation();
  const [setChatGroupVisibility] = useSetChatGroupVisibilityMutation();
  const [deleteChatGroup] = useDeleteChatGroupMutation();
  const [leaveChatGroup] = useLeaveChatGroupMutation();
  const [requestToJoinChatGroup] = useRequestToJoinChatGroupMutation();
  const [cancelJoinRequest] = useCancelJoinRequestMutation();
  const [joinPublicChatGroup] = useJoinPublicChatGroupMutation();
  const sortOptions = ['Popular', 'Latest', 'Most Active', 'Public', 'Private'];
  const [isSortOpen, setIsSortOpen] = useState(false);
  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const [selectedChatGroupId, setSelectedChatGroupId] = useState(null);
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const [showOwnerLeaveOptionsModal, setShowOwnerLeaveOptionsModal] = useState(false);
  const ellipsisRefs = useRef([]);
  const chatGroupsRef = useRef(null);
  const sortBtnRef = useRef(null);
  const sortDropdownRef = useRef(null);
  const chatDropdownRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const activeDropdownGroupIdRef = useRef(null);
  const rectsRef = useRef({
    sortBtn: null,
    sortDropdown: null,
    chatDropdown: null,
    ellipsisBtn: null,
  });
  const closeAllMenus = useCallback(() => {
    setIsSortOpen(false);
    setActiveDropdownIndex(null);
    activeDropdownGroupIdRef.current = null;
  }, []);
  const measureToRect = useCallback((ref, key) => {
    if (!ref?.current) {
      rectsRef.current[key] = null;
      return;
    }
    ref.current.measureInWindow((x, y, width, height) => {
      rectsRef.current[key] = { x, y, width, height };
    });
  }, []);
  const pointInRect = useCallback((x, y, r) => {
    return !!r && x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height;
  }, []);
  const handleRootTouchEndCapture = useCallback((e) => {
    if (swallowNextCloseRef.current) {
      swallowNextCloseRef.current = false;
      return;
    }
    if (!isSortOpen && activeDropdownIndex === null) return;

    const { pageX: x, pageY: y } = e.nativeEvent;
    const { sortBtn, sortDropdown, chatDropdown, ellipsisBtn } = rectsRef.current;

    if (
      pointInRect(x, y, sortBtn) ||
      pointInRect(x, y, sortDropdown) ||
      pointInRect(x, y, chatDropdown) ||
      pointInRect(x, y, ellipsisBtn)
    ) {
      return;
    }
    closeAllMenus();
  }, [isSortOpen, activeDropdownIndex, closeAllMenus, pointInRect]);
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always', onScrollBeginDrag: closeAllMenus }),
    [closeAllMenus]
  );
  const selectedChat = useMemo(
   () => displayedGroups.find(g => g.id === selectedChatGroupId) ?? null,
   [displayedGroups, selectedChatGroupId]
  );
  const handleHelpersScroll = useCallback(() => {
    if (isSortOpen) setIsSortOpen(false);
  }, [isSortOpen]);
  const handleChatGroupsScroll = useCallback(() => {
    if (activeDropdownIndex !== null) setActiveDropdownIndex(null);
    if (isSortOpen) setIsSortOpen(false);
  }, [activeDropdownIndex, isSortOpen]);
  const handleEllipsisPress = useCallback((index, groupId) => {
    swallowNextCloseRef.current = true;
    setIsSortOpen(false);
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
    activeDropdownGroupIdRef.current = groupId;
  }, []);
  useEffect(() => {
    if (activeDropdownIndex === null) return;
    const openGroup = displayedGroups[activeDropdownIndex];
    if (!openGroup || openGroup.id !== activeDropdownGroupIdRef.current) {
      closeAllMenus();
    }
  }, [displayedGroups, activeDropdownIndex, closeAllMenus]);
  const handleSortPressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    setActiveDropdownIndex(null);
    setIsSortOpen((o) => !o);
  }, []);
  const handleSortOptionPressIn = useCallback((opt) => {
    swallowNextCloseRef.current = true;
    setSortBy(opt);
  }, []);
  const handleSearchFocusOrTouch = useCallback(() => {
    closeAllMenus();
  }, [closeAllMenus]);
  const handleEditNamePressIn = useCallback((id) => {
    swallowNextCloseRef.current = true;
    setSelectedChatGroupId(id);
    setShowEditChatNameModal(true);
  }, []);
  const handleConfirmEditName = useCallback((newName) => {
    if (!selectedChatGroupId) return;
    if (authToken) renameChatGroup({ authToken, chatGroupId: selectedChatGroupId, name: newName });
    setShowEditChatNameModal(false);
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId, authToken, renameChatGroup]);
  const handleMembersPressIn = useCallback((id, isOwner) => {
    swallowNextCloseRef.current = true;
    navigation.navigate('Members', { chatGroupId: id, isOwner });
  }, [navigation]);
  const handleMakeChatPrivatePressIn = useCallback((id) => {
    closeAllMenus();
    swallowNextCloseRef.current = true;
    if (authToken) setChatGroupVisibility({ authToken, chatGroupId: id, isPublic: false });
  }, [closeAllMenus, authToken, setChatGroupVisibility]);
  const handleMakeChatPublicPressIn = useCallback((id) => {
    closeAllMenus();
    swallowNextCloseRef.current = true;
    if (authToken) setChatGroupVisibility({ authToken, chatGroupId: id, isPublic: true });
  }, [closeAllMenus, authToken, setChatGroupVisibility]);
  const handleShareChatPressIn = useCallback((id) => {
    swallowNextCloseRef.current = true;
  }, []);  
  const handleDeleteChatPressIn = useCallback((id) => {
    swallowNextCloseRef.current = true;      
    setSelectedChatGroupId(id);                
    setShowDeleteChatGroupModal(true);       
  }, []);
  const handleConfirmDeleteChatGroup = useCallback(() => {
    if (!selectedChatGroupId) return;
    if (authToken) deleteChatGroup({ authToken, chatGroupId: selectedChatGroupId });
    setShowDeleteChatGroupModal(false);
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId, authToken, deleteChatGroup]);
  const handleConfirmLeaveChatGroup = useCallback(async () => {
    if (!selectedChatGroupId) return;
    setShowLeaveChatGroupModal(false);
    if (!authToken) { setSelectedChatGroupId(null); return; }
    try {
      const response = await leaveChatGroup({ authToken, chatGroupId: selectedChatGroupId }).unwrap();
      if (response && response.status === 'lastOwner') {
        setShowOwnerLeaveOptionsModal(true);
        return;
      }
    } catch (error) {
    }
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId, authToken, leaveChatGroup]);
  const handleConfirmMakePublicAndLeave = useCallback(async () => {
    if (!selectedChatGroupId) return;
    setShowOwnerLeaveOptionsModal(false);
    if (authToken) {
      try {
        await leaveChatGroup({ authToken, chatGroupId: selectedChatGroupId, disposition: 'makePublic' }).unwrap();
      } catch (error) {
      }
    }
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId, authToken, leaveChatGroup]);
  const handleConfirmDeleteAndLeave = useCallback(async () => {
    if (!selectedChatGroupId) return;
    setShowOwnerLeaveOptionsModal(false);
    if (authToken) {
      try {
        await leaveChatGroup({ authToken, chatGroupId: selectedChatGroupId, disposition: 'delete' }).unwrap();
      } catch (error) {
      }
    }
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId, authToken, leaveChatGroup]);
  const handleLeaveChatGroupPress = useCallback((item) => {
    closeAllMenus();
    setSelectedChatGroupId(item.id);
    if (item.owner && (item.memberCount || 0) <= 1) {
      setShowOwnerLeaveOptionsModal(true);
    } else {
      setShowLeaveChatGroupModal(true);
    }
  }, [closeAllMenus]);
  const handleViewChatGroupPress = useCallback((id) => {
    closeAllMenus();
    navigation.navigate('ChatGroup', { chatGroupId: id });
  }, [closeAllMenus, navigation]);
  const handleRequestJoinChatGroupPress = useCallback(async (id) => {
    closeAllMenus();
    if (!authToken) return;
    await requestToJoinChatGroup({ authToken, chatGroupId: id });
  }, [closeAllMenus, authToken, requestToJoinChatGroup]);
  const handleCancelJoinRequestPress = useCallback((id) => {
    closeAllMenus();
    if (authToken) cancelJoinRequest({ authToken, chatGroupId: id });
  }, [closeAllMenus, authToken, cancelJoinRequest]);
  const handleJoinChatGroupPress = useCallback((id) => {
    closeAllMenus();
    if (authToken) joinPublicChatGroup({ authToken, chatGroupId: id });
  }, [closeAllMenus, authToken, joinPublicChatGroup]);
  const renderHelper = useCallback(({ item }) => (
    <View style={styles.helperCard}>
      <TouchableOpacity style={styles.helperCardBtn}>
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
  ), [styles]);
  const renderChatGroup = useCallback(({ item, index }) => {
      const isActive = activeDropdownIndex === index;
      const maxVisible = 5;
      const overlapSpacing = isTablet ? 32.19 : 24;
      const spacing = scaleWidth(overlapSpacing);

      return (
        <View style={[styles.chatGroupCard, item.joined && styles.chatGroupCardJoinedBorder, { overflow: 'visible' }]} needsOffscreenAlphaCompositing>
          <View style={styles.chatPhotosHeader}>
            {item.public || item.joined ? (
              <HelperStack
                helpers={item.helpers}
                total={item.memberCount}
                spacing={spacing}
                maxVisible={maxVisible}
                imageBaseStyle={styles.chatGroupHelperImage}
                initialStyle={{ color: White, fontSize: scaleFont(14), fontWeight: '600' }}
                wrapperStyle={styles.chatGroupHelpersWrapper}
                stackStyle={styles.chatGroupHelpersStack}
                extraCircleStyle={styles.extraHelpersCircle}
                extraTextStyle={styles.extraHelpersText}
                TextComponent={CustomText}
              />
            ) : (
              <View style={styles.memberCountWrapper}>
                <CustomText style={styles.memberCountText}>
                  {item.memberCount} {item.memberCount === 1 ? 'member' : 'members'}
                </CustomText>
              </View>
            )}
            <View style={styles.joinedEllipsisView}>
              {item.public ? (
                <View style={styles.publicCircle}>
                  <CustomText style={styles.publicAndPrivateLabel}>Public</CustomText>
                </View>
              ) : (
                <View style={styles.privateCircle}>
                  <CustomText style={styles.publicAndPrivateLabel}>Private</CustomText>
                </View>
              )}
              <TouchableOpacity
                ref={(ref) => (ellipsisRefs.current[index] = ref)}
                style={styles.ellipsisBackground}
                onPressIn={() => handleEllipsisPress(index, item.id)}
              >
                <EllipsisIcon {...styles.ellipsis} />
              </TouchableOpacity>
            </View>
          </View>

          <View style={styles.chatGroupTitleView}>
            <CustomText style={styles.chatGroupTitle}>
              {item.title}
            </CustomText>
          </View>

          {item.joined ? (
            <View style={styles.chatGroupTwoBtnView}>
              <TouchableOpacity 
                style={styles.groupChatLeaveChatBtn} 
                onPress={() => handleLeaveChatGroupPress(item)}
              >
                <CustomText style={styles.groupChatLeaveChatTxt}>Leave Chat</CustomText>
              </TouchableOpacity>
              <TouchableOpacity 
                style={styles.groupChatViewChatBtn} 
                onPress={() => handleViewChatGroupPress(item.id)}
              >
                <CustomText style={styles.groupChatViewChatTxt}>View Chat</CustomText>
              </TouchableOpacity>
            </View>
          ) : !item.public && !item.joined && !item.joinRequest ? (
            <View style={styles.chatGroupOneBtnView}>
              <TouchableOpacity 
                style={styles.groupChatRequestJoinBtn} 
                onPress={() => handleRequestJoinChatGroupPress(item.id)}
              >
                <CustomText style={styles.groupChatRequestJoinTxt}>Request to Join</CustomText>
              </TouchableOpacity>
            </View>
          ) : !item.public && !item.joined && item.joinRequest ? (
            <View style={styles.chatGroupOneBtnView}>
              <TouchableOpacity 
                style={styles.groupChatCancelRequestBtn} 
                onPress={() => handleCancelJoinRequestPress(item.id)}
              >
                <CustomText style={styles.groupChatCancelRequestTxt}>Cancel Request</CustomText>
              </TouchableOpacity>
            </View>
          ) : item.public && !item.joined ? (
            <View style={styles.chatGroupOneBtnView}>
              <TouchableOpacity 
                style={styles.groupChatJoinNowBtn} 
                onPress={() => handleJoinChatGroupPress(item.id)}
              >
                <CustomText style={styles.groupChatJoinNowTxt}>Join Now</CustomText>
              </TouchableOpacity>
            </View>
          ) : null}

          {isActive && (
            <Pressable
              ref={chatDropdownRef}
              onLayout={() => measureToRect(chatDropdownRef, 'chatDropdown')}
              style={styles.chatGroupDropdown}
            >
              {item.owner && (
                <TouchableOpacity
                  onPressIn={() => handleEditNamePressIn(item.id)}
                  onPressOut={closeAllMenus}
                  style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                >
                  <CustomText style={styles.dropdownBlackTxt}>Edit name</CustomText>
                  <EditIcon {...styles.dropdownIcons} />
                </TouchableOpacity>
              )}

              {(item.public || item.joined) && (
                <TouchableOpacity
                  onPressIn={() => handleMembersPressIn(item.id, item.owner)}
                  onPressOut={closeAllMenus}
                  style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                >
                  <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                  <MembersIcon {...styles.dropdownIcons} />
                  {item.pendingMembers && item.owner && <PendingMembersCircle {...styles.pendingMembersCircle} />}
                </TouchableOpacity>
              )}

              {item.owner && item.public && (
                <TouchableOpacity
                  onPressIn={() => handleMakeChatPrivatePressIn(item.id)}
                  onPressOut={closeAllMenus}
                  style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                >
                  <CustomText style={styles.dropdownBlackTxt}>Make Chat Private</CustomText>
                  <PrivateIcon {...styles.dropdownIcons} />
                </TouchableOpacity>
              )}
              {item.owner && !item.public && (
                <TouchableOpacity
                  onPressIn={() => handleMakeChatPublicPressIn(item.id)}
                  onPressOut={closeAllMenus}
                  style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                >
                  <CustomText style={styles.dropdownBlackTxt}>Make Chat Public</CustomText>
                  <PrivateIcon {...styles.dropdownIcons} />
                </TouchableOpacity>
              )}              
                <TouchableOpacity
                  onPressIn={() => handleShareChatPressIn(item.id)}
                  onPressOut={closeAllMenus}
                  style={[styles.chatGroupDropdownOptions, item.owner ? styles.chatGroupDropdownOptionsBorderBottom : null]}
                >
                  <CustomText style={styles.dropdownBlackTxt}>Share Chat</CustomText>
                  <LinkIcon {...styles.dropdownIcons} />
                </TouchableOpacity>
              {item.owner && (
                <TouchableOpacity
                  onPressIn={() => handleDeleteChatPressIn(item.id)}
                  onPressOut={closeAllMenus}
                  style={styles.deleteOption}
                >
                  <CustomText style={styles.dropdownRedTxt}>Delete group</CustomText>
                  <TrashIcon {...styles.dropdownIcons} />
                </TouchableOpacity>
              )}
            </Pressable>
          )}
        </View>
      );
    }, [activeDropdownIndex, isTablet, styles, authToken]
  );
  const renderEmptyChatGroups = useCallback(() => {
    if (isChatGroupsInitialLoading) return null;
    if (connectionFailed) {
      return (
        <View style={styles.emptyState}>
          <View style={styles.emptyStateIconCircle}>
            <SadEmoji {...styles.emptyStateIcon} />
          </View>
          <CustomText style={styles.emptyStateTitle}>Can't connect right now</CustomText>
          <CustomText style={styles.emptyStateSubtitle}>
            Check your internet connection. We'll keep retrying automatically.
          </CustomText>
          <TouchableOpacity style={styles.emptyStateRetryBtn} onPress={handleRetryConnection}>
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
        <CustomText style={styles.emptyStateTitle}>No chats yet</CustomText>
        <CustomText style={styles.emptyStateSubtitle}>
          Tap HELP ME to talk to someone, or I CAN HELP to support someone.
        </CustomText>
      </View>
    );
  }, [styles, isChatGroupsInitialLoading, connectionFailed, handleRetryConnection]);
  useEffect(() => {
    ellipsisRefs.current = Array(displayedGroups.length)
      .fill(null)
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [displayedGroups.length]);
  useEffect(() => {
    if (isSortOpen) {
      requestAnimationFrame(() => {
        measureToRect(sortBtnRef, 'sortBtn');
        measureToRect(sortDropdownRef, 'sortDropdown');
      });
    } else {
      rectsRef.current.sortDropdown = null;
    }
  }, [isSortOpen, measureToRect]);
  useEffect(() => {
    if (activeDropdownIndex !== null) {
      const ellipsisRef = ellipsisRefs.current[activeDropdownIndex];
      requestAnimationFrame(() => {
        if (ellipsisRef?.current) {
          ellipsisRef.current.measureInWindow((x, y, width, height) => {
            rectsRef.current.ellipsisBtn = { x, y, width, height };
          });
        }
        if (chatDropdownRef.current) {
          measureToRect(chatDropdownRef, 'chatDropdown');
        }
      });
    } else {
      rectsRef.current.ellipsisBtn = null;
      rectsRef.current.chatDropdown = null;
    }
  }, [activeDropdownIndex, measureToRect]);
  const topNavStyle = useMemo(
    () => ({ ...styles.topNav, paddingTop: statusBarHeight }),
    [styles.topNav, statusBarHeight]
  );
  const chatGroupsContentContainer = useMemo(() => ({
    ...styles.chatGroupsListContent,
    paddingBottom: bottomSafeHeight + styles.chatGroupsListContent.paddingBottom,
  }), [styles.chatGroupsListContent, bottomSafeHeight]);
  return (
    <>
      <View style={styles.root} onTouchEndCapture={handleRootTouchEndCapture}>
        <View style={topNavStyle}>
          <AccountBar closeMenus={closeAllMenus} />

          <HelpHub />
          <SearchAndSortChatGroupsBar
            search={search}
            onChangeSearch={setSearch}
            onSearchFocus={handleSearchFocusOrTouch}
            sortBy={sortBy}
            onSortPress={handleSortPressIn}
            sortBtnRef={sortBtnRef}
          />
        </View>
        <View style={styles.mainContent}>
          {helpersTop.length > 0 && (
            <View style={styles.helpers}>
              <CustomText style={styles.availableHelpersTxt}>Available Helpers</CustomText>
              <FlatList
                data={helpersTop}
                {...listCommonProps}
                onScroll={handleHelpersScroll}
                showsHorizontalScrollIndicator={false}
                contentContainerStyle={styles.helpersListContent}
                keyExtractor={(item) => item.id}
                renderItem={renderHelper}
                horizontal
              />
            </View>
          )}
          <View style={styles.ChatGroups}>
            <ActiveIndexContext.Provider value={activeDropdownIndex}>
              <FlatList
                ref={chatGroupsRef}
                data={displayedGroups}
                {...listCommonProps}
                contentContainerStyle={chatGroupsContentContainer}
                showsVerticalScrollIndicator={false}
                keyExtractor={(item) => item.id}
                onScroll={handleChatGroupsScroll}
                scrollEventThrottle={16}
                removeClippedSubviews={false}
                extraData={{ activeDropdownIndex, displayedGroups }}
                renderItem={renderChatGroup}
                ListEmptyComponent={renderEmptyChatGroups}
                CellRendererComponent={ActiveListCell}
              />
            </ActiveIndexContext.Provider>
          </View>
          {isSortOpen && (
            <SearchAndSortChatGroupsDropdown
              sortOptions={sortOptions}
              sortBy={sortBy}
              onSelectOption={handleSortOptionPressIn}
              onClose={closeAllMenus}
              dropdownRef={sortDropdownRef}
              onLayout={() => measureToRect(sortDropdownRef, 'sortDropdown')}
            />
          )}
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
      <EditChatNameModal
        visible={showEditChatNameModal}
        initialName={selectedChat?.title ?? ''}
        maxLen={100}
        onConfirm={handleConfirmEditName}
        onCancel={() => { setShowEditChatNameModal(false); setSelectedChatGroupId(null); }}
      />
      <DeleteChatGroupModal
        visible={showDeleteChatGroupModal}
        onConfirm={handleConfirmDeleteChatGroup}
        onCancel={() => { setShowDeleteChatGroupModal(false); setSelectedChatGroupId(null); }}
      /> 
      <LeaveChatGroupModal
        visible={showLeaveChatGroupModal}
        onConfirm={handleConfirmLeaveChatGroup}
        onCancel={() => { setShowLeaveChatGroupModal(false); setSelectedChatGroupId(null); }}
      />
      <LeaveGroupOwnerOptionsModal
        visible={showOwnerLeaveOptionsModal}
        onMakePublic={handleConfirmMakePublicAndLeave}
        onDelete={handleConfirmDeleteAndLeave}
        onCancel={() => { setShowOwnerLeaveOptionsModal(false); setSelectedChatGroupId(null); }}
      />
    </>
  );
}