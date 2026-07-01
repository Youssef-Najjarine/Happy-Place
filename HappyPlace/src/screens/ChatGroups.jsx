import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute } from '@react-navigation/native';
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
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import SearchIcon from 'assets/images/global/search-icon.svg';
import SortIcon from 'assets/images/chatGroups/sort-icon.svg';
import LinkIcon from 'assets/images/chatGroups/share-chat-link-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import DownArrowIcon from 'assets/images/global/arrow-down-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import PrivateIcon from 'assets/images/global/private-chat-icon.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';

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
    spacing,
    maxVisible = 5,
    imageBaseStyle,
    wrapperStyle,
    stackStyle,
    extraCircleStyle,
    extraTextStyle,
    TextComponent,
  }) {
    const capped = useMemo(() => helpers.slice(0, maxVisible), [helpers, maxVisible]);

    return (
      <View style={wrapperStyle}>
        <View
          style={stackStyle}
          collapsable={false}
          shouldRasterizeIOS
          renderToHardwareTextureAndroid
        >
          {capped.map((h, i) => (
            <Image
              key={i}
              source={h.image}
              defaultSource={h.image}
              fadeDuration={0}
              progressiveRenderingEnabled={false}
              style={[imageBaseStyle, { left: i * spacing }]}
            />
          ))}

          {helpers.length > maxVisible && (
            <View style={[extraCircleStyle, { left: maxVisible * spacing }]}>
              <TextComponent style={extraTextStyle}>+{helpers.length - maxVisible}</TextComponent>
            </View>
          )}
        </View>
      </View>
    );
  },
  (prev, next) =>
    prev.helpers === next.helpers &&
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
  topNavIcons: { 
    width: scaleWidth(20), 
    height: scaleHeight(20), 
    resizeMode: 'contain' 
  },
  searchAndSortRow: {
    paddingHorizontal: scaleWidth(20),
    height: scaleHeight(39),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  search: { 
    width: scaleWidth(200),
    height: '100%' 
  },
  searchIcon: { 
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
  sort: { 
    width: scaleWidth(127), 
    height: '100%' 
  },
  sortBtn: {
    borderRadius: scaleWidth(99),
    paddingVertical: scaleHeight(9),
    paddingHorizontal: scaleWidth(10),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  sortTxt: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 600, 
    color: Black 
  },
  sortByDropdown: {
    top: scaleHeight(-32),
    right: scaleWidth(20),
    width: scaleWidth(127),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1.341),
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
    zIndex: 2000
  },
  sortByOptions: { 
    paddingHorizontal: scaleWidth(16), 
    paddingVertical: scaleHeight(8), 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    alignItems: 'center' 
  },
  sortByDropdownTxt: { 
    fontSize: scaleFont(14), 
    lineHeight: scaleLineHeight(21), 
    letterSpacing: scaleLetterSpacing(-0.14), 
    fontWeight: 500 
  },
  sortByNotSelectedTxt: { 
    color: Black 
  },
  sortBySelectedTxt: { 
    color: HappyColor 
  },
  sortByOptionsBorderBottom: { 
    borderBottomWidth: scaleHeight(0.671), 
    borderBottomColor: TranslucentBlack 
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
  chatGroupCard: {
    height: scaleHeight(163),
    maxHeight: scaleHeight(163),
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
  topNavIcons: { 
    width: scaleWidth(24), 
    height: scaleHeight(24), 
    resizeMode: 'contain' 
  },
  searchAndSortRow: {
    paddingHorizontal: scaleWidth(24),
    height: scaleHeight(47),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  search: { 
    width: scaleWidth(532),
    height: '100%' 
  },
  searchIcon: { 
    top: scaleHeight(10), 
    left: scaleWidth(12), 
    position: 'absolute' 
  },
  searchInput: {
    borderRadius: scaleWidth(132.792),
    paddingLeft: scaleWidth(44),
    paddingVertical: scaleHeight(10),
    paddingRight: scaleWidth(12),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    width: '100%',
    height: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  sort: { 
    width: scaleWidth(152), 
    height: '100%' 
  },
  sortBtn: {
    borderRadius: scaleWidth(132.792),
    paddingVertical: scaleHeight(11.5),
    paddingHorizontal: scaleWidth(12),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  sortTxt: { 
    fontSize: scaleFont(18), 
    lineHeight: scaleLineHeight(27), 
    letterSpacing: scaleLetterSpacing(-0.18), 
    fontWeight: 600, 
    color: Black 
  },
  sortByDropdown: {
    top: scaleHeight(-42.57),
    right: scaleWidth(24),
    width: scaleWidth(152),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: VividBlueViolet,
    shadowOpacity: 0.10,             
    shadowOffset: {
      width:  scaleWidth(10.731),
      height: scaleHeight(10.731),
    },
    shadowRadius: scaleWidth(40.24),
    elevation: 16,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000
  },
  sortByOptions: { 
    paddingHorizontal: scaleWidth(21.461), 
    paddingVertical: scaleHeight(8), 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    alignItems: 'center' 
  },
  sortByDropdownTxt: { 
    fontSize: scaleFont(18), 
    lineHeight: scaleLineHeight(27), 
    letterSpacing: scaleLetterSpacing(-0.18), 
    fontWeight: 500 
  },
  sortByNotSelectedTxt: { 
    color: Black 
  },
  sortBySelectedTxt: { 
    color: HappyColor 
  },
  sortByOptionsBorderBottom: { 
    borderBottomWidth: scaleHeight(0.671), 
    borderBottomColor: TranslucentBlack 
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
  chatGroupCard: {
    height: scaleHeight(212.11467),
    maxHeight: scaleHeight(212.11467),
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
  const helpersTop = [];
  const route = useRoute();
  const sortOptions = ['Popular', 'Latest', 'A - Z', 'Z - A'];
  const [isSortOpen, setIsSortOpen] = useState(false);
  const [sortBy, setSortBy] = useState('Popular');
  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const [search, setSearch] = useState('');
  const [chatGroups, setChatGroups] = useState([]);
  const [selectedChatGroupId, setSelectedChatGroupId] = useState(null);
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const ellipsisRefs = useRef([]);
  const chatGroupsRef = useRef(null);
  const sortBtnRef = useRef(null);
  const sortDropdownRef = useRef(null);
  const chatDropdownRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    sortBtn: null,
    sortDropdown: null,
    chatDropdown: null,
    ellipsisBtn: null,
  });
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always', onScrollBeginDrag: closeAllMenus }),
    [closeAllMenus]
  );
  const sortedChatGroups = useMemo(() => {
    const arr = [...chatGroups];
    switch (sortBy) {
      case 'Popular':
        return arr.sort((a, b) => (b.helpers?.length || 0) - (a.helpers?.length || 0));
      case 'A - Z':
        return arr.sort((a, b) => a.title.localeCompare(b.title));
      case 'Z - A':
        return arr.sort((a, b) => b.title.localeCompare(a.title));
      case 'Latest':
      default:
        return arr;
    }
  }, [chatGroups, sortBy]);
  const selectedChat = useMemo(
   () => chatGroups.find(g => g.id === selectedChatGroupId) ?? null,
   [chatGroups, selectedChatGroupId]
  );
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
  const closeAllMenus = useCallback(() => {
    setIsSortOpen(false);
    setActiveDropdownIndex(null);
  }, []);
  const handleHelpersScroll = useCallback(() => {
    if (isSortOpen) setIsSortOpen(false);
  }, [isSortOpen]);
  const handleChatGroupsScroll = useCallback(() => {
    if (activeDropdownIndex !== null) setActiveDropdownIndex(null);
    if (isSortOpen) setIsSortOpen(false);
  }, [activeDropdownIndex, isSortOpen]);
  const handleEllipsisPress = useCallback((index) => {
    swallowNextCloseRef.current = true;
    setIsSortOpen(false);
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
  }, []);
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
    setChatGroups(prev =>
      prev.map(g => g.id === selectedChatGroupId ? { ...g, title: newName } : g)
    );
    setShowEditChatNameModal(false);
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId]);
  const handleMembersPressIn = useCallback((id) => {
    swallowNextCloseRef.current = true;
    navigation.navigate('Members');
  }, []);
  const handleMakeChatPrivatePressIn = useCallback((id) => {
    closeAllMenus();
    swallowNextCloseRef.current = true;
    setChatGroups(prev =>
      prev.map(g => g.id === id ? { ...g, public: false } : g)
    );
  }, []);
  const handleMakeChatPublicPressIn = useCallback((id) => {
    closeAllMenus();
    swallowNextCloseRef.current = true;
    setChatGroups(prev =>
      prev.map(g => g.id === id ? { ...g, public: true } : g)
    );
  }, []);
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
    setChatGroups(prev => prev.filter(g => g.id !== selectedChatGroupId));
    setShowDeleteChatGroupModal(false);
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId]);
 const handleConfirmLeaveChatGroup = useCallback(() => {
    if (!selectedChatGroupId) return;
    setChatGroups(prev =>
      prev.map(g => g.id === selectedChatGroupId ? { ...g, joined: false } : g)
    );
    setShowLeaveChatGroupModal(false);
    setSelectedChatGroupId(null);
  }, [selectedChatGroupId]);
  const handleLeaveChatGroupPress = useCallback((id) => {
    closeAllMenus();
    setSelectedChatGroupId(id);
    setShowLeaveChatGroupModal(true);
  }, [closeAllMenus]);
  function handleViewChatGroupPress() {
    closeAllMenus();
    navigation.navigate('ChatGroup')
  }
  const handleRequestJoinChatGroupPress = useCallback((id) => {
    closeAllMenus();
    setChatGroups(prev =>
      prev.map(g => g.id === id ? { ...g, joinRequest: true } : g)
    );
  }, [closeAllMenus]);
  const handleCancelJoinRequestPress = useCallback((id) => {
    closeAllMenus();
    setChatGroups(prev =>
      prev.map(g => g.id === id ? { ...g, joinRequest: false } : g)
    );
  }, [closeAllMenus]);
  const handleJoinChatGroupPress = useCallback((id) => {
    closeAllMenus();
    setChatGroups(prev =>
      prev.map(g => g.id === id ? { ...g, joined: true } : g)
    );
  }, [closeAllMenus]);
  const renderHelper = useCallback(({ item }) => (
    <View style={styles.helperCard}>
      <TouchableOpacity style={styles.helperCardBtn}>
        <Image source={item.image} style={styles.helperImage} fadeDuration={0} />
        <CustomText style={styles.helperName}>{item.name}</CustomText>
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
            <HelperStack
              helpers={item.helpers}
              spacing={spacing}
              maxVisible={maxVisible}
              imageBaseStyle={styles.chatGroupHelperImage}
              wrapperStyle={styles.chatGroupHelpersWrapper}
              stackStyle={styles.chatGroupHelpersStack}
              extraCircleStyle={styles.extraHelpersCircle}
              extraTextStyle={styles.extraHelpersText}
              TextComponent={CustomText}
            />
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
                onPressIn={() => handleEllipsisPress(index)}
              >
                <EllipsisIcon {...styles.ellipsis} />
              </TouchableOpacity>
            </View>
          </View>

          <View style={styles.chatGroupTitleView}>
            <CustomText style={styles.chatGroupTitle} numberOfLines={1} ellipsizeMode="tail">
              {item.title}
            </CustomText>
          </View>

          {item.joined ? (
            <View style={styles.chatGroupTwoBtnView}>
              <TouchableOpacity 
                style={styles.groupChatLeaveChatBtn} 
                onPress={() => handleLeaveChatGroupPress(item.id)}
              >
                <CustomText style={styles.groupChatLeaveChatTxt}>Leave Chat</CustomText>
              </TouchableOpacity>
              <TouchableOpacity 
                style={styles.groupChatViewChatBtn} 
                onPress={() => { handleViewChatGroupPress();}}
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

              <TouchableOpacity
                onPressIn={() => handleMembersPressIn(item.id)}
                onPressOut={closeAllMenus}
                style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
              >
                <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                <MembersIcon {...styles.dropdownIcons} />
                {item.pendingMembers && item.owner && <PendingMembersCircle {...styles.pendingMembersCircle} />}
              </TouchableOpacity>

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
    }, [activeDropdownIndex, isTablet, styles]
  );
  const renderEmptyChatGroups = useCallback(() => (
    <View style={styles.emptyState}>
      <View style={styles.emptyStateIconCircle}>
        <HappyEmoji {...styles.emptyStateIcon} />
      </View>
      <CustomText style={styles.emptyStateTitle}>No chats yet</CustomText>
      <CustomText style={styles.emptyStateSubtitle}>
        Tap HELP ME to talk to someone, or I CAN HELP to support someone.
      </CustomText>
    </View>
  ), [styles]);
  useEffect(() => {
    ellipsisRefs.current = Array(chatGroups.length)
      .fill(null)
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [chatGroups.length]);
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
  const navigation = useNavigation();
  const cameFromLogin = route.params?.from === 'login';
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const { width } = useWindowDimensions();
  const isTablet = width >= tabletBreakpoint;
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
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
          <View style={styles.searchAndSortRow}>
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
              <SearchIcon {...StyleSheet.flatten([styles.topNavIcons, styles.searchIcon])} />
            </View>
            <View style={styles.sort}>
              <TouchableOpacity
                style={styles.sortBtn}
                ref={sortBtnRef}
                onPressIn={handleSortPressIn}
              >
                <SortIcon {...styles.topNavIcons} />
                <CustomText style={styles.sortTxt}>{sortBy}</CustomText>
                <DownArrowIcon {...styles.topNavIcons} />
              </TouchableOpacity>
            </View>
          </View>
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
                data={sortedChatGroups}
                {...listCommonProps}
                contentContainerStyle={chatGroupsContentContainer}
                showsVerticalScrollIndicator={false}
                keyExtractor={(item) => item.id}
                onScroll={handleChatGroupsScroll}
                scrollEventThrottle={16}
                removeClippedSubviews={false}
                extraData={{ activeDropdownIndex, chatGroups }}
                renderItem={renderChatGroup}
                ListEmptyComponent={renderEmptyChatGroups}
                CellRendererComponent={ActiveListCell}
              />
            </ActiveIndexContext.Provider>
          </View>
          {isSortOpen && (
            <View
              ref={sortDropdownRef}
              onLayout={() => measureToRect(sortDropdownRef, 'sortDropdown')}
              style={styles.sortByDropdown}
            >
              {sortOptions.map((opt, idx) => (
                <TouchableOpacity
                  key={opt}
                  onPressIn={() => handleSortOptionPressIn(opt)}
                  onPressOut={closeAllMenus}
                  style={[styles.sortByOptions, idx < sortOptions.length - 1 && styles.sortByOptionsBorderBottom]}
                >
                  <CustomText
                    style={[
                      styles.sortByDropdownTxt,
                      sortBy === opt ? styles.sortBySelectedTxt : styles.sortByNotSelectedTxt,
                    ]}
                  >
                    {opt}
                  </CustomText>
                </TouchableOpacity>
              ))}
            </View>
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
    </>
  );
}