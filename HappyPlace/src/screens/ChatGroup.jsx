import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, SectionList, useWindowDimensions, Platform, Pressable, ScrollView, Keyboard, PanResponder, Modal, PermissionsAndroid, AppState, Animated, Easing } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute, useIsFocused } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
    HappyColor, 
    White, 
    Black, 
    VeryLightGray, 
    SoftGray, 
    Charcoal, 
    VividBlueViolet, 
    WarmIvory, 
    SoftRosePink, 
    VeryLightLavenderTint, 
    TranslucentBlack,
    WhiteScrim
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import EditChatNameModal from 'src/components/EditChatNameModal';
import ViewChatNameModal from 'src/components/ViewChatNameModal';
import MediaViewerModal from 'src/components/MediaViewerModal';
import DeleteChatGroupModal from 'src/components/DeleteChatGroupModal';
import LeaveChatGroupModal from 'src/components/LeaveChatGroupModal';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import { useSelector } from 'react-redux';
import tokenStorage from 'src/services/tokenStorage';
import { showToast } from 'src/components/Toast';
import { Svg, Path, Rect } from 'react-native-svg';
import Avatar from 'src/components/Avatar';
import RemoteImage from 'src/components/RemoteImage';
import ReportMessageModal from 'src/components/ReportMessageModal';
import ImagePicker from 'react-native-image-crop-picker';
import baseService from 'src/services/baseService';
import { buildReplyContext, resolveReplyDisplay } from 'src/utils/chatMessageStore';

function loadOptionalModule(loader) {
    try {
        const loadedModule = loader();
        return loadedModule && loadedModule.default ? loadedModule.default : loadedModule;
    } catch (error) {
        return null;
    }
}

const Video = loadOptionalModule(() => require('react-native-video'));
const Sound = loadOptionalModule(() => require('react-native-nitro-sound'));
const EmojiPicker = loadOptionalModule(() => require('rn-emoji-keyboard'));

function PlayGlyph({ size, color }) {
    return (
        <Svg width={size} height={size} viewBox="0 0 24 24">
            <Path d="M8 5.14v13.72c0 .96 1.05 1.55 1.87 1.05l10.98-6.86c.77-.48.77-1.62 0-2.1L9.87 4.09C9.05 3.59 8 4.18 8 5.14z" fill={color} />
        </Svg>
    );
}

function PauseGlyph({ size, color }) {
    return (
        <Svg width={size} height={size} viewBox="0 0 24 24">
            <Rect x="6" y="4.5" width="4.4" height="15" rx="2.2" fill={color} />
            <Rect x="13.6" y="4.5" width="4.4" height="15" rx="2.2" fill={color} />
        </Svg>
    );
}
import useChatMessages from 'src/hooks/useChatMessages';
import { selectCachedChatGroup } from 'src/store/chatMessagesApi';
import { useListMembersQuery, useRenameChatGroupMutation, useSetChatGroupVisibilityMutation, useDeleteChatGroupMutation, useLeaveChatGroupMutation, useApproveMemberMutation, useRejectMemberMutation } from 'src/store/chatGroupsApi';
import { formatMessageTime, localDateKey, localDateKeyForDaysAgo, formatDayHeader } from 'src/utils/chatTime';
import MicrophoneIcon from 'assets/images/chatGroup/microphone-icon.svg';
import PlusIcon from 'assets/images/chatGroup/plus-black-icon.svg';
import SendMessageIcon from 'assets/images/chatGroup/send-message-icon.svg';
import SentMessageIcon from 'assets/images/chatGroup/sent-message-icon.svg';
import ClockIcon from 'assets/images/chatGroup/clock-icon.svg';
import ViewedMessageIcon from 'assets/images/chatGroup/viewed-message-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import PrivateIcon from 'assets/images/global/private-chat-icon.svg';
import LeaveGroupIcon from 'assets/images/global/leave-and-remove-chat-icon.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';

const phoneStyles = StyleSheet.create({
    root: { 
        backgroundColor: WarmIvory, 
        height: '100%', 
        width: '100%',
        position: 'relative'
    },
    topNav: {
        gap: scaleHeight(12),
        paddingTop: scaleHeight(12),
        paddingBottom: scaleHeight(16),
        borderBottomLeftRadius: scaleWidth(24),
        borderBottomRightRadius: scaleWidth(24),
        width: '100%',
        backgroundColor: White
    },
    chatHeaderRow: {
        paddingHorizontal: scaleWidth(20),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between'
    },
    backArrowAndChatNameRow: {
        gap: scaleWidth(12),
        flex: 1,
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
    chatNameAndMembers: {
        gap: scaleHeight(2),
        flex: 1
    },
    directHeaderRow: {
        gap: scaleWidth(10),
        flexDirection: 'row',
        alignItems: 'center'
    },
    headerTextColumn: {
        gap: scaleHeight(2)
    },
    directHeaderTextColumn: {
        gap: scaleHeight(2),
        flex: 1
    },
    directHeaderAvatar: {
        width: scaleWidth(36),
        height: scaleWidth(36),
        borderRadius: scaleWidth(18)
    },
    directHeaderAvatarInitial: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        color: White
    },
    largeIcons: {
        width: scaleWidth(28),
        height: scaleHeight(28),
        resizeMode: 'contain'
    },
    chatNameTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    membersRow: {
        flexDirection: 'row',
        alignItems: 'center'
    },
    membersTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 500
    },
    lightMembersColor: {
        color: Charcoal
    },
    blackMembersColor: {
        color: Black
    },
    memberNameShrink: {
        flexShrink: 1
    },
    privacyLabelAndEllipsisRow: {
        gap: scaleWidth(12),
        flexShrink: 0,
        flexDirection: 'row',
        alignItems: 'center'
    },
    privacyLabel: {
        width: scaleWidth(60),
        height: scaleHeight(29),
        borderRadius: scaleWidth(99),
        justifyContent: 'center',
        alignItems: 'center'
    },
    publicBackgroundColor: {
        backgroundColor: VeryLightGray
    },
    privateBackgroundColor: {
        backgroundColor: SoftRosePink
    },
    privacyLabelTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
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
    chatGroupDropdown: {
        top: scaleHeight(-60),
        right: scaleWidth(35),
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
        zIndex: 2000
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
    requestToJoin: {
        width: scaleWidth(335),
        top: scaleHeight(-34),
        right: scaleWidth(20),
        borderWidth: scaleWidth(1),
        shadowRadius: scaleWidth(15),
        shadowOffset: { 
            width: scaleWidth(8), 
            height: scaleHeight(8) 
        },
        shadowColor: VeryLightLavenderTint,
        shadowOpacity: 1,
        elevation: 12,
        position: 'absolute',
        borderColor: SoftGray,
        zIndex: 2000,
    },
    requestToJoinRow: {
        borderRadius: scaleWidth(99),
        paddingVertical: scaleHeight(8),
        paddingHorizontal: scaleWidth(8),
        width:'100%',
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        backgroundColor: White
    },
    requestToJoinImageAndNames: {
        gap: scaleWidth(8),
        flexDirection: 'row',
        alignItems: 'center'

    },
    requestToJoinProfilePicture: {
        width: scaleWidth(42),
        height: scaleHeight(42),
        borderRadius: scaleWidth(50),
        resizeMode: 'contain'
    },
    requestToJoinNameTxt: {
        width: scaleWidth(137),
        fontSize: scaleFont(16), 
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    requestToJoinusernameTxt: {
        width: scaleWidth(137),
        fontSize: scaleFont(12), 
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        opacity: 0.6,
        fontStyle: 'italic',
        color: Black
    },
    requestToJoinOptions: {
        height: scaleHeight(42),
        gap: scaleWidth(8),
        flexDirection: 'row',
        alignItems: 'center'
    },
    requestToJoinAcceptBtn: {
        width: scaleWidth(74),
        borderRadius: scaleWidth(99),
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    requestToJoinAcceptTxt: {
        fontSize: scaleFont(16), 
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: White
    },
    requestToJoinXBtn: {
        width: scaleWidth(42),
        borderRadius: scaleWidth(99),
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: VeryLightGray
    },
    xIcon: {
        width: scaleWidth(42),
        height: scaleHeight(42)
    },
    chatGroup: {
        paddingHorizontal: scaleWidth(20),
        flex: 1,
        minHeight: 0,
        position: 'relative',
        zIndex: 1000,
        elevation: 1000,
        overflow: 'visible'
    },
    ChatMessageSeparator: {
        height: scaleHeight(16)
    },
    dayHeader: {
        paddingHorizontal: scaleWidth(20),
        marginVertical: scaleHeight(16),
        alignItems: 'center'
    },
    dayHeaderTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: Black
    },
    sentIndicatorAndClockIcon: {
        width: scaleWidth(12),
        height: scaleHeight(12)
    },
    failedIndicator: {
        width: scaleWidth(14),
        height: scaleHeight(14),
        borderRadius: scaleWidth(99),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    failedIndicatorTxt: {
        fontSize: scaleFont(10),
        lineHeight: scaleLineHeight(12),
        fontWeight: 700,
        color: White
    },
    notDeliveredTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        marginTop: scaleHeight(2),
        alignSelf: 'flex-end',
        color: HappyColor
    },
    myChatTextBox: {
        gap: scaleHeight(6),
        minWidth: scaleWidth(82),
        maxWidth: scaleWidth(279),
        minHeight: scaleHeight(66),
        borderRadius: scaleWidth(16),
        paddingTop: scaleHeight(10),
        paddingBottom: scaleHeight(8),
        paddingLeft: scaleWidth(14),
        paddingRight: scaleWidth(12),
        alignSelf: 'flex-end',
        backgroundColor: HappyColor
    },
    myChatText: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: White,
        flexShrink: 1
    },
    myTimeStamp: {
        gap: scaleWidth(4),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'flex-end'
    },
    myTimeStampTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        color: WhiteScrim
    },
    helperChatMessageView: {
        gap: scaleWidth(6),
        alignSelf: 'flex-start',
        flexDirection: 'row'
    },
    helperChatView: {
        gap: scaleHeight(8)
    },
    helperProfilePictureContainer: {
        width: scaleWidth(32),
        justifyContent: 'flex-end'
    },
    helperProfilePicture: {
        width: '100%',
        height: scaleHeight(32),
        borderRadius: scaleWidth(50),
        resizeMode: 'contain'
    },
    helperChatTextBox: {
        gap: scaleHeight(6),
        minWidth: scaleWidth(82),
        maxWidth: scaleWidth(257),
        minHeight: scaleHeight(66),
        borderRadius: scaleWidth(16),
        paddingTop: scaleHeight(12),
        paddingBottom: scaleHeight(8),
        paddingHorizontal: scaleWidth(12),
        alignSelf: 'flex-start',
        backgroundColor: White
    },
    helperChatText: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        flexShrink: 1,
        alignSelf: 'flex-start',
        color: Black
    },
    helperTimeStamp: {
        alignItems: 'flex-end'
    },
    helperTimeStampTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        color: Charcoal 
    },
    helperReadMoreTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: HappyColor
    },
    myReadMoreTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: Black
    },
    myChatMessageView: {
        gap: scaleHeight(8),
        alignSelf: 'flex-end',
        alignItems: 'flex-end',
    },
    helperChatMessageImage: {
        borderBottomRightRadius: scaleWidth(16),
        borderBottomLeftRadius: scaleWidth(4)
    },
    myChatMessageImage: {
        borderBottomRightRadius: scaleWidth(4),
        borderBottomLeftRadius: scaleWidth(16)
    },
    chatMessageImages: {
        gap: scaleHeight(8),
        width: scaleWidth(279),
        alignSelf: 'flex-end', 
    },
    chatMessageImageView: {
        height: scaleHeight(180),
        width: '100%',
        position: 'relative'
    },
    chatMessageImage: {
        borderTopLeftRadius: scaleWidth(16),
        borderTopRightRadius: scaleWidth(16),
        width: '100%',
        height: '100%',
        resizeMode: 'cover'
    },
    chatMessageImageTimeStamp: {
        bottom: scaleHeight(8),
        right: scaleWidth(8),
        width: scaleWidth(67),
        height: scaleHeight(26),
        borderRadius: scaleWidth(20),
        gap: scaleWidth(4),
        position: 'absolute',
        flexDirection: 'row',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: Charcoal

    },
    chatMessageImageTimeStampTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        color: White
    },
    peopleTyping: {
        marginTop: scaleHeight(16)
    },
    peopleTypingTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 500,
        color: Black
    },
    textBoxContainer: {
        width: '100%'
    },
    inputRow: {
        paddingVertical: scaleHeight(6),
        paddingHorizontal: scaleWidth(6),
        borderRadius: scaleWidth(50),
        width: '100%',
        position: 'relative',
        backgroundColor: White
    },
    inputView: {
        width: '100%'
    },
    input: {
        paddingLeft: scaleWidth(56),
        paddingRight: scaleWidth(56),
        paddingTop: scaleHeight(10.5),
        paddingBottom: scaleHeight(10.5),
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.16),
        width: '100%',
        minHeight: scaleHeight(42),
        maxHeight: scaleHeight(126),
        textAlignVertical: 'center',
        fontWeight: 500,
        color: Black
    },
    textBoxBtnViews: {
        width: scaleWidth(42),
        height: scaleHeight(42),
        borderRadius: scaleWidth(50),
        position: 'absolute'
    },
    textBoxBtns: {
        width: '100%',
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center'
    },
    plusView: {
        bottom: scaleHeight(6),
        left: scaleWidth(6),
        backgroundColor: "#F9F5EA"
    },  
    microphoneAndSendView: {
        bottom: scaleHeight(6),
        right: scaleWidth(6),
        backgroundColor: HappyColor
    },
    scrollToBottomPill: {
        position: 'absolute',
        bottom: scaleHeight(8),
        alignSelf: 'center',
        paddingHorizontal: scaleWidth(14),
        height: scaleHeight(34),
        borderRadius: scaleWidth(99),
        justifyContent: 'center',
        alignItems: 'center',
        shadowColor: Black,
        shadowOffset: { width: 0, height: scaleHeight(2) },
        shadowOpacity: 0.2,
        shadowRadius: scaleWidth(6),
        elevation: 4,
        backgroundColor: HappyColor
    },
    scrollToBottomPillTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: White
    },
    helperSenderNameTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 600,
        marginBottom: scaleHeight(2),
        color: VividBlueViolet
    },
    tombstoneTxt: {
        fontStyle: 'italic',
        opacity: 0.55
    },
    chatSingleImageView: {
        borderRadius: scaleWidth(12),
        overflow: 'hidden'
    },
    chatSingleImage: {
        width: scaleWidth(220),
        borderRadius: scaleWidth(12)
    },
    mediaPlaceholderTxt: {
        fontStyle: 'italic',
        opacity: 0.7
    },
    reactionsRow: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        gap: scaleWidth(6),
        marginTop: scaleHeight(4)
    },
    reactionsRowMine: {
        justifyContent: 'flex-end'
    },
    reactionChip: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(4),
        paddingHorizontal: scaleWidth(8),
        paddingVertical: scaleHeight(2),
        borderRadius: scaleWidth(99),
        borderWidth: scaleWidth(1),
        borderColor: SoftGray,
        backgroundColor: White
    },
    reactionChipMine: {
        borderColor: HappyColor,
        backgroundColor: SoftRosePink
    },
    reactionChipTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600,
        color: Black
    },
    avatarInitialTxt: {
        fontSize: scaleFont(14),
        fontWeight: 700,
        color: White
    },
    loadingOlderTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        textAlign: 'center',
        opacity: 0.6,
        paddingVertical: scaleHeight(6),
        color: Black
    },
    emptyStateView: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(12),
        paddingHorizontal: scaleWidth(24)
    },
    emptyStateTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 500,
        textAlign: 'center',
        opacity: 0.7,
        color: Black
    },
    retryBtn: {
        borderRadius: scaleWidth(99),
        paddingHorizontal: scaleWidth(24),
        height: scaleHeight(40),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    retryBtnTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        color: White
    },
    actionSheetOverlay: {
        flex: 1,
        justifyContent: 'flex-end',
        backgroundColor: TranslucentBlack
    },
    actionSheetCard: {
        borderTopLeftRadius: scaleWidth(24),
        borderTopRightRadius: scaleWidth(24),
        paddingTop: scaleHeight(16),
        paddingBottom: scaleHeight(28),
        paddingHorizontal: scaleWidth(16),
        gap: scaleHeight(12),
        backgroundColor: White
    },
    reactionBarRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        paddingHorizontal: scaleWidth(4)
    },
    reactionBarBtn: {
        width: scaleWidth(48),
        height: scaleWidth(48),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: VeryLightGray
    },
    reactionBarBtnActive: {
        backgroundColor: SoftRosePink,
        borderWidth: scaleWidth(1),
        borderColor: HappyColor
    },
    reactionBarEmojiTxt: {
        fontSize: scaleFont(22)
    },
    actionSheetOption: {
        height: scaleHeight(48),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center'
    },
    actionSheetOptionBorderTop: {
        borderTopWidth: scaleWidth(1),
        borderTopColor: VeryLightGray
    },
    actionSheetBlackTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: Black
    },
    actionSheetRedTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: HappyColor
    },
    recordingBar: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        height: scaleHeight(48),
        paddingHorizontal: scaleWidth(8)
    },
    recordingTimerView: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(8)
    },
    recordingDot: {
        width: scaleWidth(10),
        height: scaleWidth(10),
        borderRadius: scaleWidth(99),
        backgroundColor: HappyColor
    },
    recordingTimerTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: Black
    },
    recordingCancelBtn: {
        paddingHorizontal: scaleWidth(12),
        height: '100%',
        justifyContent: 'center'
    },
    recordingCancelTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        opacity: 0.7,
        color: Black
    },
    recordingSendBtn: {
        borderRadius: scaleWidth(99),
        paddingHorizontal: scaleWidth(20),
        height: scaleHeight(36),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    recordingSendTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        color: White
    },
    videoBubbleBox: {
        width: scaleWidth(220),
        height: scaleHeight(140),
        borderRadius: scaleWidth(12),
        overflow: 'hidden',
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(6),
        backgroundColor: Charcoal
    },
    videoPreviewFill: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
    },
    videoPreviewScrim: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0,0,0,0.18)'
    },
    videoPreviewPlayCircle: {
        width: scaleWidth(52),
        height: scaleWidth(52),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0,0,0,0.45)'
    },
    videoPreviewDurationTxt: {
        position: 'absolute',
        right: scaleWidth(8),
        bottom: scaleHeight(8),
        fontSize: scaleFont(12),
        fontWeight: 600,
        color: White
    },
    voiceRow: {
        width: scaleWidth(216),
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(10),
        paddingVertical: scaleHeight(2)
    },
    voicePlayBtn: {
        width: scaleWidth(36),
        height: scaleWidth(36),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: VeryLightGray
    },
    voiceTrackTouch: {
        flex: 1,
        height: scaleHeight(28),
        justifyContent: 'center'
    },
    voiceTrackBase: {
        height: scaleHeight(4),
        borderRadius: scaleWidth(99),
        width: '100%'
    },
    voiceTrackFill: {
        position: 'absolute',
        height: scaleHeight(4),
        borderRadius: scaleWidth(99),
        left: 0
    },
    voiceTrackThumb: {
        position: 'absolute',
        width: scaleWidth(12),
        height: scaleWidth(12),
        borderRadius: scaleWidth(99),
        marginLeft: scaleWidth(-6)
    },
    voiceTrackBaseMine: {
        backgroundColor: 'rgba(255,255,255,0.4)'
    },
    voiceTrackBaseHelper: {
        backgroundColor: VeryLightGray
    },
    voiceTrackFillMine: {
        backgroundColor: White
    },
    voiceTrackFillHelper: {
        backgroundColor: HappyColor
    },
    voiceThumbMine: {
        backgroundColor: White
    },
    voiceThumbHelper: {
        backgroundColor: HappyColor
    },
    voiceTimeTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600
    },
    voiceTimeMine: {
        color: White
    },
    voiceTimeHelper: {
        color: Charcoal
    },
    stagedAttachmentBar: {
        marginBottom: scaleHeight(8)
    },
    stagedAttachmentBarContent: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(10),
        paddingTop: scaleHeight(8),
        paddingHorizontal: scaleWidth(12)
    },
    stagedItem: {
        position: 'relative'
    },
    composerOverlay: {
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 1001,
        elevation: 1001
    },
    composerBackdrop: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        opacity: 0.88,
        backgroundColor: WarmIvory
    },
    stagedThumb: {
        width: scaleWidth(64),
        height: scaleWidth(64),
        borderRadius: scaleWidth(10),
        overflow: 'hidden',
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: Charcoal
    },
    stagedThumbFill: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
    },
    stagedThumbScrim: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0,0,0,0.18)'
    },
    stagedVoiceChip: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(8),
        height: scaleHeight(40),
        paddingHorizontal: scaleWidth(14),
        borderRadius: scaleWidth(99),
        borderWidth: scaleWidth(1),
        borderColor: SoftGray,
        backgroundColor: White
    },
    stagedVoiceTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        fontWeight: 600,
        color: Black
    },
    stagedRemoveBtn: {
        position: 'absolute',
        top: scaleHeight(-7),
        right: scaleWidth(-7),
        width: scaleWidth(22),
        height: scaleWidth(22),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 2,
        backgroundColor: TranslucentBlack
    },
    stagedRemoveTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(15),
        fontWeight: 600,
        color: White
    },
    attachSheetOverlay: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        justifyContent: 'flex-end',
        zIndex: 3000,
        elevation: 3000,
        backgroundColor: TranslucentBlack
    },
    replyChip: {
        borderLeftWidth: scaleWidth(3),
        borderRadius: scaleWidth(8),
        paddingVertical: scaleHeight(5),
        paddingHorizontal: scaleWidth(8),
        gap: scaleHeight(1)
    },
    replyChipMine: {
        borderLeftColor: White,
        backgroundColor: WhiteScrim
    },
    replyChipHelper: {
        borderLeftColor: HappyColor,
        backgroundColor: VeryLightGray
    },
    replyChipSenderTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 700
    },
    replyChipSenderMine: {
        color: White
    },
    replyChipSenderHelper: {
        color: HappyColor
    },
    replyChipBodyTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(17),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 500
    },
    replyChipBodyMine: {
        color: WhiteScrim
    },
    replyChipBodyHelper: {
        color: Charcoal
    },
    replyChipDeletedTxt: {
        fontStyle: 'italic'
    },
    replyBar: {
        flexDirection: 'row',
        alignItems: 'center',
        marginTop: scaleHeight(8),
        borderRadius: scaleWidth(12),
        borderLeftWidth: scaleWidth(3),
        borderLeftColor: HappyColor,
        paddingVertical: scaleHeight(8),
        paddingLeft: scaleWidth(10),
        paddingRight: scaleWidth(8),
        backgroundColor: White
    },
    replyBarTextColumn: {
        flex: 1,
        gap: scaleHeight(1)
    },
    replyBarSenderTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 700,
        color: HappyColor
    },
    replyBarBodyTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 500,
        color: Charcoal
    },
    replyBarCloseBtn: {
        width: scaleWidth(26),
        height: scaleWidth(26),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        marginLeft: scaleWidth(8),
        backgroundColor: VeryLightGray
    },
    replyBarCloseTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600,
        color: Black
    },
    guestCountdownPill: {
        alignSelf: 'center',
        marginTop: scaleHeight(8),
        borderRadius: scaleWidth(99),
        paddingVertical: scaleHeight(6),
        paddingHorizontal: scaleWidth(14),
        backgroundColor: White
    },
    guestCountdownTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 600,
        color: HappyColor,
        textAlign: 'center'
    },
    guestWall: {
        width: '100%',
        borderRadius: scaleWidth(16),
        paddingVertical: scaleHeight(14),
        paddingHorizontal: scaleWidth(16),
        alignItems: 'center',
        gap: scaleHeight(10),
        backgroundColor: White
    },
    guestWallTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(20),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: Black,
        textAlign: 'center'
    },
    guestWallBtn: {
        borderRadius: scaleWidth(99),
        paddingVertical: scaleHeight(9),
        paddingHorizontal: scaleWidth(22),
        backgroundColor: HappyColor
    },
    guestWallBtnTxt: {
        fontSize: scaleFont(15),
        lineHeight: scaleLineHeight(20),
        letterSpacing: scaleLetterSpacing(-0.15),
        fontWeight: 800,
        color: White
    },
    highlightedRow: {
        borderRadius: scaleWidth(12),
        backgroundColor: VeryLightLavenderTint
    },
    swipeReplyIndicatorWrap: {
        position: 'absolute',
        left: scaleWidth(2),
        top: 0,
        bottom: 0,
        justifyContent: 'center'
    },
    swipeReplyIndicatorCircle: {
        width: scaleWidth(28),
        height: scaleWidth(28),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: White
    },
    swipeReplyIndicatorGlyph: {
        fontSize: scaleFont(15),
        lineHeight: scaleLineHeight(18),
        fontWeight: 800,
        color: HappyColor
    },
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
        paddingTop: scaleHeight(16.1),
        paddingBottom: scaleHeight(20),
        borderBottomLeftRadius: scaleWidth(32.192),
        borderBottomRightRadius: scaleWidth(32.192),
        width: '100%',
        backgroundColor: White
    },
    chatHeaderRow: {
        paddingHorizontal: scaleWidth(24),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between'
    },
    backArrowAndChatNameRow: {
        gap: scaleWidth(16.1),
        flex: 1,
        flexDirection: 'row',
        alignItems: 'center'
    },
    BackArrow: {
        width: 68.42,
        height: 68.42,
        borderRadius: scaleWidth(132.792),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: VeryLightGray
    },
    chatNameAndMembers: {
        gap: scaleHeight(2.68),
        flex: 1
    },
    directHeaderRow: {
        gap: scaleWidth(12),
        flexDirection: 'row',
        alignItems: 'center'
    },
    headerTextColumn: {
        gap: scaleHeight(2.68)
    },
    directHeaderTextColumn: {
        gap: scaleHeight(2.68),
        flex: 1
    },
    directHeaderAvatar: {
        width: scaleWidth(44),
        height: scaleWidth(44),
        borderRadius: scaleWidth(22)
    },
    directHeaderAvatarInitial: {
        fontSize: scaleFont(18),
        fontWeight: 600,
        color: White
    },
    largeIcons: {
        width: scaleWidth(37.557),
        height: scaleHeight(37.557),
        resizeMode: 'contain'
    },
    chatNameTxt: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 600,
        color: Black
    },
    membersRow: {
        flexDirection: 'row',
        alignItems: 'center'
    },
    membersTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500
    },
    lightMembersColor: {
        color: Charcoal
    },
    blackMembersColor: {
        color: Black
    },
    memberNameShrink: {
        flexShrink: 1
    },
    privacyLabelAndEllipsisRow: {
        gap: scaleWidth(16.1),
        flexShrink: 0,
        flexDirection: 'row',
        alignItems: 'center'
    },
    privacyLabel: {
        width: scaleWidth(73.192),
        height: scaleHeight(34.73),
        borderRadius: scaleWidth(132.792),
        justifyContent: 'center',
        alignItems: 'center'
    },
    publicBackgroundColor: {
        backgroundColor: VeryLightGray
    },
    privateBackgroundColor: {
        backgroundColor: SoftRosePink
    },
    privacyLabelTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    ellipsisBackground: { 
        width: 68.42,
        height: 68.42,
        borderRadius: scaleWidth(132.792), 
        backgroundColor: VeryLightGray, 
        justifyContent: 'center', 
        alignItems: 'center' 
    },
    chatGroupDropdown: {
        top: scaleHeight(-73),
        right: scaleWidth(45),
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
        paddingHorizontal: scaleWidth(21.461), 
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
    requestToJoin: {
        width: scaleWidth(696),
        top: scaleHeight(-40),
        right: scaleWidth(24),
        borderWidth: scaleWidth(1.341),
        shadowRadius: scaleWidth(15),
        shadowOffset: { 
            width: scaleWidth(8), 
            height: scaleHeight(8) 
        },
        shadowColor: VeryLightLavenderTint,
        shadowOpacity: 1,
        elevation: 12,
        position: 'absolute',
        borderColor: SoftGray,
        zIndex: 2000,
    },
    requestToJoinRow: {
        borderRadius: scaleWidth(132.792),
        paddingVertical: scaleHeight(12),
        paddingHorizontal: scaleWidth(12),
        width:'100%',
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        backgroundColor: White
    },
    requestToJoinImageAndNames: {
        gap: scaleWidth(12),
        flexDirection: 'row',
        alignItems: 'center'

    },
    requestToJoinProfilePicture: {
        width: 68.42,
        height: 68.42,
        borderRadius: scaleWidth(67.067),
        resizeMode: 'contain'
    },
    requestToJoinNameTxt: {
        width: scaleWidth(428.136),
        fontSize: scaleFont(20), 
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 600,
        color: Black
    },
    requestToJoinusernameTxt: {
        width: scaleWidth(428.136),
        fontSize: scaleFont(16), 
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        opacity: 0.6,
        fontStyle: 'italic',
        color: Black
    },
    requestToJoinOptions: {
        height: scaleHeight(54.144),
        gap: scaleWidth(12),
        flexDirection: 'row',
        alignItems: 'center'
    },
    requestToJoinAcceptBtn: {
        width: scaleWidth(95.192),
        borderRadius: scaleWidth(132.792),
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    requestToJoinAcceptTxt: {
        fontSize: scaleFont(20), 
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 600,
        color: White
    },
    requestToJoinXBtn: {
        borderRadius: scaleWidth(132.792),
        width: 68.42,
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: VeryLightGray
    },
    xIcon: {
        width: scaleWidth(50),
        height: scaleHeight(50)
    },
    chatGroup: {
        paddingHorizontal: scaleWidth(24),
        flex: 1,
        minHeight: 0,
        position: 'relative',
        zIndex: 1000,
        elevation: 1000,
        overflow: 'visible'
    },
    ChatMessageSeparator: {
        height: scaleHeight(20)
    },
    dayHeader: {
        paddingHorizontal: scaleWidth(24),
        marginVertical: scaleHeight(20),
        alignItems: 'center'
    },
    dayHeaderTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    sentIndicatorAndClockIcon: {
        width: scaleWidth(16.096),
        height: scaleHeight(16.096)
    },
    failedIndicator: {
        width: scaleWidth(18),
        height: scaleHeight(18),
        borderRadius: scaleWidth(132.792),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    failedIndicatorTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(14),
        fontWeight: 700,
        color: White
    },
    notDeliveredTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 500,
        marginTop: scaleHeight(2.68),
        alignSelf: 'flex-end',
        color: HappyColor
    },
    myChatTextBox: {
        gap: scaleHeight(8.05),
        minWidth: scaleWidth(109.336),
        maxWidth: scaleWidth(496),
        minHeight: scaleHeight(83.192),
        borderRadius: scaleWidth(21.461),
        paddingTop: scaleHeight(12),
        paddingBottom: scaleHeight(8),
        paddingLeft: scaleWidth(16),
        paddingRight: scaleWidth(16),
        alignSelf: 'flex-end',
        backgroundColor: HappyColor
    },
    myChatText: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 500,
        color: White,
        flexShrink: 1
    },
    myTimeStamp: {
        gap: scaleWidth(5.37),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'flex-end'
    },
    myTimeStampTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: WhiteScrim 
    },
    helperChatMessageView: {
        gap: scaleWidth(8),
        alignSelf: 'flex-start',
        flexDirection: 'row'
    },
    helperChatView: {
        gap: scaleHeight(12)
    },
    helperProfilePictureContainer: {
        width: 59.54,
        justifyContent: 'flex-end'
    },
    helperProfilePicture: {
        width: '100%',
        height: 59.54,
        borderRadius: scaleWidth(67.067),
        resizeMode: 'contain'
    },
    helperChatTextBox: {
        gap: scaleHeight(8.05),
        minWidth: scaleWidth(109.336),
        maxWidth: scaleWidth(349),
        minHeight: scaleHeight(83.192),
        borderRadius: scaleWidth(21.461),
        paddingTop: scaleHeight(16),
        paddingBottom: scaleHeight(12),
        paddingHorizontal: scaleWidth(16),
        alignSelf: 'flex-start',
        backgroundColor: White
    },
    helperChatText: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 500,
        flexShrink: 1,
        alignSelf: 'flex-start',
        color: Black
    },
    helperTimeStamp: {
        alignItems: 'flex-end'
    },
    helperTimeStampTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: Charcoal 
    },
    helperReadMoreTxt: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 500,
        color: HappyColor
    },
    myReadMoreTxt: {
        fontSize: scaleFont(18),
        lineHeight: scaleLineHeight(27),
        letterSpacing: scaleLetterSpacing(-0.18),
        fontWeight: 500,
        color: Black
    },
    myChatMessageView: {
        gap: scaleHeight(12),
        alignSelf: 'flex-end',
        alignItems: 'flex-end',
    },
    helperChatMessageImage: {
        borderBottomRightRadius: scaleWidth(21.461),
        borderBottomLeftRadius: scaleWidth(5.365)
    },
    myChatMessageImage: {
        borderBottomRightRadius: scaleWidth(5.365),
        borderBottomLeftRadius: scaleWidth(21.461)
    },
    chatMessageImages: {
        gap: scaleHeight(12),
        width: scaleWidth(496),
        alignSelf: 'flex-end', 
    },
    chatMessageImageView: {
        height: scaleHeight(241.44),
        width: '100%',
        position: 'relative'
    },
    chatMessageImage: {
        borderTopLeftRadius: scaleWidth(21.461),
        borderTopRightRadius: scaleWidth(21.461),
        width: '100%',
        height: '100%',
        resizeMode: 'cover'
    },
    chatMessageImageTimeStamp: {
        bottom: scaleHeight(10.73),
        right: scaleWidth(12),
        width: scaleWidth(89.55733),
        height: scaleHeight(34.73067),
        borderRadius: scaleWidth(26.827),
        gap: scaleWidth(5.37),
        position: 'absolute',
        flexDirection: 'row',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: Charcoal

    },
    chatMessageImageTimeStampTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: White
    },
    peopleTyping: {
        marginTop: scaleHeight(20)
    },
    peopleTypingTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 500,
        color: Black
    },
    textBoxContainer: {
        width: '100%'
    },
    inputRow: {
        paddingVertical: scaleHeight(8.05),
        paddingHorizontal: scaleWidth(8.05),
        borderRadius: scaleWidth(67.067),
        width: '100%',
        position: 'relative',
        backgroundColor: White
    },
    inputView: {
        width: '100%'
    },
    input: {
        paddingLeft: scaleWidth(60),
        paddingRight: scaleWidth(60),
        paddingTop: scaleHeight(13.166),
        paddingBottom: scaleHeight(13.166),
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        width: '100%',
        minHeight: scaleHeight(56.332),
        maxHeight: scaleHeight(176.332),
        textAlignVertical: 'center',
        fontWeight: 500,
        color: Black
    },
    textBoxBtnViews: {
        width: 68.42,
        height: 68.42,
        borderRadius: scaleWidth(67.067),
        position: 'absolute'
    },
    textBoxBtns: {
        width: '100%',
        height: '100%',
        justifyContent: 'center',
        alignItems: 'center'
    },
    plusView: {
        bottom: scaleHeight(8.05),
        left: scaleWidth(8.05),
        backgroundColor: "#F9F5EA"
    },  
    microphoneAndSendView: {
        bottom: scaleHeight(8.05),
        right: scaleWidth(8.05),
        backgroundColor: HappyColor
    },
    scrollToBottomPill: {
        position: 'absolute',
        bottom: scaleHeight(10),
        alignSelf: 'center',
        paddingHorizontal: scaleWidth(18),
        height: scaleHeight(42),
        borderRadius: scaleWidth(132.792),
        justifyContent: 'center',
        alignItems: 'center',
        shadowColor: Black,
        shadowOffset: { width: 0, height: scaleHeight(2) },
        shadowOpacity: 0.2,
        shadowRadius: scaleWidth(6),
        elevation: 4,
        backgroundColor: HappyColor
    },
    scrollToBottomPillTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: White
    },
    helperSenderNameTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 600,
        marginBottom: scaleHeight(2),
        color: VividBlueViolet
    },
    tombstoneTxt: {
        fontStyle: 'italic',
        opacity: 0.55
    },
    chatSingleImageView: {
        borderRadius: scaleWidth(12),
        overflow: 'hidden'
    },
    chatSingleImage: {
        width: scaleWidth(220),
        borderRadius: scaleWidth(12)
    },
    mediaPlaceholderTxt: {
        fontStyle: 'italic',
        opacity: 0.7
    },
    reactionsRow: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        gap: scaleWidth(6),
        marginTop: scaleHeight(4)
    },
    reactionsRowMine: {
        justifyContent: 'flex-end'
    },
    reactionChip: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(4),
        paddingHorizontal: scaleWidth(8),
        paddingVertical: scaleHeight(2),
        borderRadius: scaleWidth(99),
        borderWidth: scaleWidth(1),
        borderColor: SoftGray,
        backgroundColor: White
    },
    reactionChipMine: {
        borderColor: HappyColor,
        backgroundColor: SoftRosePink
    },
    reactionChipTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600,
        color: Black
    },
    avatarInitialTxt: {
        fontSize: scaleFont(14),
        fontWeight: 700,
        color: White
    },
    loadingOlderTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        textAlign: 'center',
        opacity: 0.6,
        paddingVertical: scaleHeight(6),
        color: Black
    },
    emptyStateView: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(12),
        paddingHorizontal: scaleWidth(24)
    },
    emptyStateTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 500,
        textAlign: 'center',
        opacity: 0.7,
        color: Black
    },
    retryBtn: {
        borderRadius: scaleWidth(99),
        paddingHorizontal: scaleWidth(24),
        height: scaleHeight(40),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    retryBtnTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        color: White
    },
    actionSheetOverlay: {
        flex: 1,
        justifyContent: 'flex-end',
        backgroundColor: TranslucentBlack
    },
    actionSheetCard: {
        borderTopLeftRadius: scaleWidth(24),
        borderTopRightRadius: scaleWidth(24),
        paddingTop: scaleHeight(16),
        paddingBottom: scaleHeight(28),
        paddingHorizontal: scaleWidth(16),
        gap: scaleHeight(12),
        backgroundColor: White
    },
    reactionBarRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        paddingHorizontal: scaleWidth(4)
    },
    reactionBarBtn: {
        width: scaleWidth(48),
        height: scaleWidth(48),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: VeryLightGray
    },
    reactionBarBtnActive: {
        backgroundColor: SoftRosePink,
        borderWidth: scaleWidth(1),
        borderColor: HappyColor
    },
    reactionBarEmojiTxt: {
        fontSize: scaleFont(22)
    },
    actionSheetOption: {
        height: scaleHeight(48),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center'
    },
    actionSheetOptionBorderTop: {
        borderTopWidth: scaleWidth(1),
        borderTopColor: VeryLightGray
    },
    actionSheetBlackTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: Black
    },
    actionSheetRedTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: HappyColor
    },
    recordingBar: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        height: scaleHeight(48),
        paddingHorizontal: scaleWidth(8)
    },
    recordingTimerView: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(8)
    },
    recordingDot: {
        width: scaleWidth(10),
        height: scaleWidth(10),
        borderRadius: scaleWidth(99),
        backgroundColor: HappyColor
    },
    recordingTimerTxt: {
        fontSize: scaleFont(16),
        fontWeight: 600,
        color: Black
    },
    recordingCancelBtn: {
        paddingHorizontal: scaleWidth(12),
        height: '100%',
        justifyContent: 'center'
    },
    recordingCancelTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        opacity: 0.7,
        color: Black
    },
    recordingSendBtn: {
        borderRadius: scaleWidth(99),
        paddingHorizontal: scaleWidth(20),
        height: scaleHeight(36),
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: HappyColor
    },
    recordingSendTxt: {
        fontSize: scaleFont(14),
        fontWeight: 600,
        color: White
    },
    videoBubbleBox: {
        width: scaleWidth(220),
        height: scaleHeight(140),
        borderRadius: scaleWidth(12),
        overflow: 'hidden',
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(6),
        backgroundColor: Charcoal
    },
    videoPreviewFill: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
    },
    videoPreviewScrim: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0,0,0,0.18)'
    },
    videoPreviewPlayCircle: {
        width: scaleWidth(52),
        height: scaleWidth(52),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0,0,0,0.45)'
    },
    videoPreviewDurationTxt: {
        position: 'absolute',
        right: scaleWidth(8),
        bottom: scaleHeight(8),
        fontSize: scaleFont(12),
        fontWeight: 600,
        color: White
    },
    voiceRow: {
        width: scaleWidth(216),
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(10),
        paddingVertical: scaleHeight(2)
    },
    voicePlayBtn: {
        width: scaleWidth(36),
        height: scaleWidth(36),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: VeryLightGray
    },
    voiceTrackTouch: {
        flex: 1,
        height: scaleHeight(28),
        justifyContent: 'center'
    },
    voiceTrackBase: {
        height: scaleHeight(4),
        borderRadius: scaleWidth(99),
        width: '100%'
    },
    voiceTrackFill: {
        position: 'absolute',
        height: scaleHeight(4),
        borderRadius: scaleWidth(99),
        left: 0
    },
    voiceTrackThumb: {
        position: 'absolute',
        width: scaleWidth(12),
        height: scaleWidth(12),
        borderRadius: scaleWidth(99),
        marginLeft: scaleWidth(-6)
    },
    voiceTrackBaseMine: {
        backgroundColor: 'rgba(255,255,255,0.4)'
    },
    voiceTrackBaseHelper: {
        backgroundColor: VeryLightGray
    },
    voiceTrackFillMine: {
        backgroundColor: White
    },
    voiceTrackFillHelper: {
        backgroundColor: HappyColor
    },
    voiceThumbMine: {
        backgroundColor: White
    },
    voiceThumbHelper: {
        backgroundColor: HappyColor
    },
    voiceTimeTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600
    },
    voiceTimeMine: {
        color: White
    },
    voiceTimeHelper: {
        color: Charcoal
    },
    stagedAttachmentBar: {
        marginBottom: scaleHeight(8)
    },
    stagedAttachmentBarContent: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(10),
        paddingTop: scaleHeight(8),
        paddingHorizontal: scaleWidth(12)
    },
    stagedItem: {
        position: 'relative'
    },
    composerOverlay: {
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 1001,
        elevation: 1001
    },
    composerBackdrop: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        opacity: 0.88,
        backgroundColor: WarmIvory
    },
    stagedThumb: {
        width: scaleWidth(64),
        height: scaleWidth(64),
        borderRadius: scaleWidth(10),
        overflow: 'hidden',
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: Charcoal
    },
    stagedThumbFill: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
    },
    stagedThumbScrim: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0,0,0,0.18)'
    },
    stagedVoiceChip: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: scaleWidth(8),
        height: scaleHeight(40),
        paddingHorizontal: scaleWidth(14),
        borderRadius: scaleWidth(99),
        borderWidth: scaleWidth(1),
        borderColor: SoftGray,
        backgroundColor: White
    },
    stagedVoiceTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        fontWeight: 600,
        color: Black
    },
    stagedRemoveBtn: {
        position: 'absolute',
        top: scaleHeight(-7),
        right: scaleWidth(-7),
        width: scaleWidth(22),
        height: scaleWidth(22),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 2,
        backgroundColor: TranslucentBlack
    },
    stagedRemoveTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(15),
        fontWeight: 600,
        color: White
    },
    attachSheetOverlay: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        justifyContent: 'flex-end',
        zIndex: 3000,
        elevation: 3000,
        backgroundColor: TranslucentBlack
    },
    replyChip: {
        borderLeftWidth: scaleWidth(3),
        borderRadius: scaleWidth(8),
        paddingVertical: scaleHeight(5),
        paddingHorizontal: scaleWidth(8),
        gap: scaleHeight(1)
    },
    replyChipMine: {
        borderLeftColor: White,
        backgroundColor: WhiteScrim
    },
    replyChipHelper: {
        borderLeftColor: HappyColor,
        backgroundColor: VeryLightGray
    },
    replyChipSenderTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 700
    },
    replyChipSenderMine: {
        color: White
    },
    replyChipSenderHelper: {
        color: HappyColor
    },
    replyChipBodyTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(17),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 500
    },
    replyChipBodyMine: {
        color: WhiteScrim
    },
    replyChipBodyHelper: {
        color: Charcoal
    },
    replyChipDeletedTxt: {
        fontStyle: 'italic'
    },
    replyBar: {
        flexDirection: 'row',
        alignItems: 'center',
        marginTop: scaleHeight(8),
        borderRadius: scaleWidth(12),
        borderLeftWidth: scaleWidth(3),
        borderLeftColor: HappyColor,
        paddingVertical: scaleHeight(8),
        paddingLeft: scaleWidth(10),
        paddingRight: scaleWidth(8),
        backgroundColor: White
    },
    replyBarTextColumn: {
        flex: 1,
        gap: scaleHeight(1)
    },
    replyBarSenderTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 700,
        color: HappyColor
    },
    replyBarBodyTxt: {
        fontSize: scaleFont(13),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.13),
        fontWeight: 500,
        color: Charcoal
    },
    replyBarCloseBtn: {
        width: scaleWidth(26),
        height: scaleWidth(26),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        marginLeft: scaleWidth(8),
        backgroundColor: VeryLightGray
    },
    replyBarCloseTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(16),
        fontWeight: 600,
        color: Black
    },
    guestCountdownPill: {
        alignSelf: 'center',
        marginTop: scaleHeight(8),
        borderRadius: scaleWidth(99),
        paddingVertical: scaleHeight(6),
        paddingHorizontal: scaleWidth(14),
        backgroundColor: White
    },
    guestCountdownTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(16),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 600,
        color: HappyColor,
        textAlign: 'center'
    },
    guestWall: {
        width: '100%',
        borderRadius: scaleWidth(16),
        paddingVertical: scaleHeight(14),
        paddingHorizontal: scaleWidth(16),
        alignItems: 'center',
        gap: scaleHeight(10),
        backgroundColor: White
    },
    guestWallTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(20),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: Black,
        textAlign: 'center'
    },
    guestWallBtn: {
        borderRadius: scaleWidth(99),
        paddingVertical: scaleHeight(9),
        paddingHorizontal: scaleWidth(22),
        backgroundColor: HappyColor
    },
    guestWallBtnTxt: {
        fontSize: scaleFont(15),
        lineHeight: scaleLineHeight(20),
        letterSpacing: scaleLetterSpacing(-0.15),
        fontWeight: 800,
        color: White
    },
    highlightedRow: {
        borderRadius: scaleWidth(12),
        backgroundColor: VeryLightLavenderTint
    },
    swipeReplyIndicatorWrap: {
        position: 'absolute',
        left: scaleWidth(2),
        top: 0,
        bottom: 0,
        justifyContent: 'center'
    },
    swipeReplyIndicatorCircle: {
        width: scaleWidth(28),
        height: scaleWidth(28),
        borderRadius: scaleWidth(99),
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: White
    },
    swipeReplyIndicatorGlyph: {
        fontSize: scaleFont(15),
        lineHeight: scaleLineHeight(18),
        fontWeight: 800,
        color: HappyColor
    },
});

const QUICK_REACTIONS = ['\u2764\uFE0F', '\uD83D\uDC4D', '\uD83D\uDE0A', '\uD83D\uDE22', '\u203C\uFE0F', '\u2753'];
const CHAR_LIMIT = 400;
const MAX_MESSAGE_LENGTH = 4096;

const VOICE_MAX_SECONDS = 300;
const MAX_STAGED_ATTACHMENTS = 10;
const VIDEO_MAX_SECONDS = 180;
const VIDEO_MAX_BYTES = 100 * 1024 * 1024;

const SWIPE_REPLY_ACTIVATE = scaleWidth(12);
const SWIPE_REPLY_TRIGGER = scaleWidth(56);
const SWIPE_REPLY_MAX = scaleWidth(72);
const SWIPE_REPLY_OVERDRAG = 0.2;

function formatDuration(totalSeconds) {
    const seconds = Math.max(0, Math.round(totalSeconds || 0));
    const minutes = Math.floor(seconds / 60);
    const remainder = seconds % 60;
    return minutes + ':' + String(remainder).padStart(2, '0');
}

function clampRatio(value) {
    return Math.min(1, Math.max(0, value));
}

function VoiceBubble({ entry, styles, mine, active, paused, positionMs, onToggle, onSeek }) {
    const trackWidthRef = useRef(1);
    const grantXRef = useRef(0);
    const [scrubRatio, setScrubRatio] = useState(null);
    const seekPanResponder = useMemo(() => PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onMoveShouldSetPanResponder: (event, gestureState) => Math.abs(gestureState.dx) > Math.abs(gestureState.dy),
        onPanResponderTerminationRequest: () => false,
        onPanResponderGrant: (event) => {
            grantXRef.current = event.nativeEvent.locationX;
            setScrubRatio(clampRatio(grantXRef.current / trackWidthRef.current));
        },
        onPanResponderMove: (event, gestureState) => {
            setScrubRatio(clampRatio((grantXRef.current + gestureState.dx) / trackWidthRef.current));
        },
        onPanResponderRelease: (event, gestureState) => {
            const ratio = clampRatio((grantXRef.current + gestureState.dx) / trackWidthRef.current);
            setScrubRatio(null);
            onSeek(entry, ratio);
        },
        onPanResponderTerminate: () => {
            setScrubRatio(null);
        },
    }), [entry, onSeek]);
    const durationSeconds = Math.max(1, entry.mediaDurationSeconds || 1);
    const playedRatio = scrubRatio != null ? scrubRatio : (active ? clampRatio((positionMs / 1000) / durationSeconds) : 0);
    const shownSeconds = scrubRatio != null ? durationSeconds * scrubRatio : (active ? positionMs / 1000 : durationSeconds);
    return (
        <View style={styles.voiceRow}>
            <TouchableOpacity style={styles.voicePlayBtn} onPress={() => onToggle(entry)}>
                {active && !paused ? (
                    <PauseGlyph size={scaleWidth(16)} color={Black} />
                ) : (
                    <PlayGlyph size={scaleWidth(16)} color={Black} />
                )}
            </TouchableOpacity>
            <View
                style={styles.voiceTrackTouch}
                onLayout={(event) => { trackWidthRef.current = Math.max(1, event.nativeEvent.layout.width); }}
                {...seekPanResponder.panHandlers}
            >
                <View pointerEvents="none" style={[styles.voiceTrackBase, mine ? styles.voiceTrackBaseMine : styles.voiceTrackBaseHelper]} />
                <View pointerEvents="none" style={[styles.voiceTrackFill, mine ? styles.voiceTrackFillMine : styles.voiceTrackFillHelper, { width: (playedRatio * 100) + '%' }]} />
                <View pointerEvents="none" style={[styles.voiceTrackThumb, mine ? styles.voiceThumbMine : styles.voiceThumbHelper, { left: (playedRatio * 100) + '%' }]} />
            </View>
            <CustomText style={[styles.voiceTimeTxt, mine ? styles.voiceTimeMine : styles.voiceTimeHelper]}>{formatDuration(shownSeconds)}</CustomText>
        </View>
    );
}

function SwipeToReplyRow({ enabled, onReply, styles, children }) {
    const translateX = useRef(new Animated.Value(0)).current;
    const onReplyRef = useRef(onReply);
    onReplyRef.current = onReply;
    const enabledRef = useRef(enabled);
    enabledRef.current = enabled;
    const panResponder = useMemo(() => PanResponder.create({
        onMoveShouldSetPanResponder: (event, gestureState) => enabledRef.current && gestureState.dx > SWIPE_REPLY_ACTIVATE && gestureState.dx > Math.abs(gestureState.dy) * 1.5,
        onPanResponderMove: (event, gestureState) => {
            const dragDistance = Math.max(0, gestureState.dx);
            const settledDistance = dragDistance > SWIPE_REPLY_TRIGGER ? SWIPE_REPLY_TRIGGER + (dragDistance - SWIPE_REPLY_TRIGGER) * SWIPE_REPLY_OVERDRAG : dragDistance;
            translateX.setValue(Math.min(settledDistance, SWIPE_REPLY_MAX));
        },
        onPanResponderRelease: (event, gestureState) => {
            if (gestureState.dx >= SWIPE_REPLY_TRIGGER) onReplyRef.current();
            Animated.spring(translateX, { toValue: 0, friction: 7, tension: 60, useNativeDriver: true }).start();
        },
        onPanResponderTerminate: () => {
            Animated.spring(translateX, { toValue: 0, friction: 7, tension: 60, useNativeDriver: true }).start();
        },
    }), [translateX]);
    const indicatorOpacity = translateX.interpolate({ inputRange: [0, SWIPE_REPLY_TRIGGER * 0.4, SWIPE_REPLY_TRIGGER], outputRange: [0, 0.35, 1], extrapolate: 'clamp' });
    const indicatorScale = translateX.interpolate({ inputRange: [0, SWIPE_REPLY_TRIGGER], outputRange: [0.6, 1], extrapolate: 'clamp' });
    return (
        <View>
            <Animated.View pointerEvents="none" style={[styles.swipeReplyIndicatorWrap, { opacity: indicatorOpacity, transform: [{ scale: indicatorScale }] }]}>
                <View style={styles.swipeReplyIndicatorCircle}>
                    <CustomText style={styles.swipeReplyIndicatorGlyph}>{'\u21A9'}</CustomText>
                </View>
            </Animated.View>
            <Animated.View style={{ transform: [{ translateX }] }} {...panResponder.panHandlers}>
                {children}
            </Animated.View>
        </View>
    );
}

export default function ChatGroup() {
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showViewChatNameModal, setShowViewChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const [isActive, setIsActive] = useState(false);
  const [chatText, setChatText] = useState('');
  const inputRef = useRef(null);
  const [isInputFocused, setIsInputFocused] = useState(false);
  const [keyboardVisible, setKeyboardVisible] = useState(false);
  const [keyboardHeightState, setKeyboardHeightState] = useState(0);
  const [composerBarHeight, setComposerBarHeight] = useState(scaleHeight(66));
  const [unseenCount, setUnseenCount] = useState(0);
  const [composerInputHeight, setComposerInputHeight] = useState(null);
  const [stagedAttachments, setStagedAttachments] = useState([]);
  const [dotCount, setDotCount] = useState(1);
  const [authToken, setAuthToken] = useState(null);
  const [actionTarget, setActionTarget] = useState(null);
  const [failedTarget, setFailedTarget] = useState(null);
  const [reportTarget, setReportTarget] = useState(null);
  const [emojiPickerTarget, setEmojiPickerTarget] = useState(null);
  const [showAttachSheet, setShowAttachSheet] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recorderBusy, setRecorderBusy] = useState(false);
  const [recordSeconds, setRecordSeconds] = useState(0);
  const [playingVoiceId, setPlayingVoiceId] = useState(null);
  const [voicePositionMs, setVoicePositionMs] = useState(0);
  const [voicePaused, setVoicePaused] = useState(false);
  const [viewerTarget, setViewerTarget] = useState(null);
  const [replyTarget, setReplyTarget] = useState(null);
  const [highlightedMessageId, setHighlightedMessageId] = useState(null);
  const recordSecondsRef = useRef(0);
  const isRecordingRef = useRef(false);
  const finishRecordingRef = useRef(null);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const focused = useIsFocused();
  const chatGroupId = route.params?.chatGroupId ?? null;
  const chatDropdownRef = useRef(null);
  const ellipsisRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    chatDropdown: null,
    ellipsisBtn: null,
  });
  const sectionListRef = useRef(null);
  const lastScrolledMessageIdRef = useRef(null);
  const isNearBottomRef = useRef(true);
  const scrollToBottomRef = useRef(null);
  const keyboardVisibleRef = useRef(false);
  const keyboardSpacerAnim = useRef(new Animated.Value(0)).current;
  const exitHandledRef = useRef(false);
  const highlightTimerRef = useRef(null);

  const [expanded, setExpanded] = useState({});

  const cachedGroup = useSelector((state) => selectCachedChatGroup(state, chatGroupId));

  useEffect(() => {
    navigation.setOptions({ gestureEnabled: false });
  }, [navigation]);

  useEffect(() => {
    lastScrolledMessageIdRef.current = null;
    isNearBottomRef.current = true;
    exitHandledRef.current = false;
    setUnseenCount(0);
    setComposerInputHeight(null);
    setStagedAttachments([]);
  }, [chatGroupId]);

  useEffect(() => {
    const showEventName = Platform.OS === 'ios' ? 'keyboardWillShow' : 'keyboardDidShow';
    const hideEventName = Platform.OS === 'ios' ? 'keyboardWillHide' : 'keyboardDidHide';
    const showSubscription = Keyboard.addListener(showEventName, (event) => {
      setKeyboardVisible(true);
      keyboardVisibleRef.current = true;
      const keyboardHeight = event && event.endCoordinates ? event.endCoordinates.height : 0;
      setKeyboardHeightState(keyboardHeight);
      if (Platform.OS === 'ios') {
        Animated.timing(keyboardSpacerAnim, {
          toValue: keyboardHeight + scaleHeight(6),
          duration: Math.min(250, (event && event.duration) || 250),
          easing: Easing.bezier(0.17, 0.59, 0.4, 0.77),
          useNativeDriver: false,
        }).start();
      }
      scrollToBottomRef.current?.(true);
    });
    const hideSubscription = Keyboard.addListener(hideEventName, (event) => {
      setKeyboardVisible(false);
      keyboardVisibleRef.current = false;
      setKeyboardHeightState(0);
      if (Platform.OS === 'ios') {
        Animated.timing(keyboardSpacerAnim, {
          toValue: bottomSafeHeight,
          duration: Math.min(250, (event && event.duration) || 200),
          easing: Easing.bezier(0.17, 0.59, 0.4, 0.77),
          useNativeDriver: false,
        }).start();
      }
    });
    return () => {
      showSubscription.remove();
      hideSubscription.remove();
    };
  }, [bottomSafeHeight, keyboardSpacerAnim]);

  useEffect(() => {
    if (!keyboardVisibleRef.current) {
      keyboardSpacerAnim.setValue(bottomSafeHeight);
    }
  }, [bottomSafeHeight, keyboardSpacerAnim]);

  const composerPanResponder = useRef(PanResponder.create({
    onMoveShouldSetPanResponderCapture: (event, gestureState) => keyboardVisibleRef.current && gestureState.dy > 12 && Math.abs(gestureState.dy) > Math.abs(gestureState.dx) * 2,
    onPanResponderGrant: () => {
      Keyboard.dismiss();
    },
  })).current;

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      const token = await tokenStorage.getToken();
      if (cancelled) return;
      if (!token) {
        showToast('Please log in to view this chat', 'info');
        navigation.goBack();
        return;
      }
      setAuthToken(token);
    };
    load();
    return () => {
      cancelled = true;
    };
  }, [navigation]);

  const {
    status,
    orderedMessages,
    sendersById,
    callerUserAccountId,
    typingUserIds,
    groupState,
    guestMessagesRemaining,
    hasOlder,
    loadingOlder,
    loadOlder,
    send,
    sendImage,
    sendVideo,
    sendVoice,
    reactTo,
    deleteOwn,
    report,
    retrySend,
    deleteFailed,
    notifyTyping,
    isViewedByEveryoneElse,
    reload,
    refreshNow,
  } = useChatMessages({ authToken, chatGroupId, focused });

  const membersQuery = useListMembersQuery(
    { authToken, chatGroupId },
    { skip: !authToken || !chatGroupId, pollingInterval: focused ? 3000 : 0 }
  );
  const chatName = groupState?.title ?? cachedGroup?.title ?? '';
  const isPublic = groupState ? !!groupState.isPublic : (cachedGroup ? !!cachedGroup.isPublic : true);
  const ownerFromGroupState = useMemo(() => {
    if (!groupState || !callerUserAccountId) return null;
    const callerEntry = (groupState.members || []).find((member) => member.userAccountId === callerUserAccountId);
    return callerEntry ? !!callerEntry.isOwner : false;
  }, [groupState, callerUserAccountId]);
  const owner = ownerFromGroupState ?? !!cachedGroup?.owner;
  const isDirect = groupState ? !!groupState.isDirect : !!cachedGroup?.isDirect;
  const directContact = groupState?.directContact ?? cachedGroup?.directContact ?? null;
  const headerTitle = isDirect ? ((directContact && directContact.displayName) || 'Direct message') : chatName;
  const activeMemberEntries = groupState?.members ?? membersQuery.data?.members ?? [];
  const pendingMemberEntries = owner ? (membersQuery.data?.pendingMembers ?? []) : [];
  const firstPendingMember = pendingMemberEntries.length > 0 ? pendingMemberEntries[0] : null;

  const memberNamesById = useMemo(() => {
    const names = {};
    activeMemberEntries.forEach((member) => {
      names[member.userAccountId] = member.name || member.username;
    });
    return names;
  }, [activeMemberEntries]);

  const memberUsernamesById = useMemo(() => {
    const usernames = {};
    activeMemberEntries.forEach((member) => {
      if (member.username) usernames[member.userAccountId] = member.username;
    });
    return usernames;
  }, [activeMemberEntries]);

  const openSenderProfile = useCallback((senderUserAccountId) => {
    const username = memberUsernamesById[senderUserAccountId];
    if (!username) return;
    navigation.push('Profile', { username });
  }, [memberUsernamesById, navigation]);

  const messagesById = useMemo(() => {
    const map = {};
    orderedMessages.forEach((entry) => {
      if (!entry.pending) map[entry.id] = entry;
    });
    return map;
  }, [orderedMessages]);

  const [renameChatGroup] = useRenameChatGroupMutation();
  const [setChatGroupVisibility] = useSetChatGroupVisibilityMutation();
  const [deleteChatGroup] = useDeleteChatGroupMutation();
  const [leaveChatGroup] = useLeaveChatGroupMutation();
  const [approveMember] = useApproveMemberMutation();
  const [rejectMember] = useRejectMemberMutation();

  useEffect(() => {
    if (exitHandledRef.current) return;
    if (status === 'notMember') {
      exitHandledRef.current = true;
      showToast('You are no longer a member of this group', 'info');
      navigation.navigate('MainTabs', { screen: 'ChatGroups' });
    } else if (status === 'groupGone') {
      exitHandledRef.current = true;
      showToast('This group is no longer available', 'info');
      navigation.navigate('MainTabs', { screen: 'ChatGroups' });
    }
  }, [status, navigation]);

  useEffect(() => {
    if (typingUserIds.length === 0) return undefined;
    const interval = setInterval(() => {
      setDotCount(prev => (prev % 3) + 1);
    }, 500);
    return () => clearInterval(interval);
  }, [typingUserIds.length]);

  const toggleExpanded = useCallback((id) => {
    setExpanded(prev => ({ ...prev, [id]: !prev[id] }));
  }, []);

  const groupedMessages = useMemo(() => {
    const todayKey = localDateKeyForDaysAgo(0);
    const yesterdayKey = localDateKeyForDaysAgo(1);

    const groups = {};
    orderedMessages.forEach((entry) => {
      const dateKey = localDateKey(entry.createdAtUtc);
      if (!groups[dateKey]) {
        groups[dateKey] = [];
      }
      groups[dateKey].push(entry);
    });

    const keys = Object.keys(groups).sort();
    return keys.map((key) => {
      let title;
      if (key === todayKey) {
        title = 'Today';
      } else if (key === yesterdayKey) {
        title = 'Yesterday';
      } else {
        title = formatDayHeader(key);
      }
      return {
        title,
        data: groups[key],
      };
    });
  }, [orderedMessages]);

  const newestMessage = orderedMessages.length > 0 ? orderedMessages[orderedMessages.length - 1] : null;

  const scrollToBottom = useCallback((animated) => {
    if (groupedMessages.length === 0) return;
    const scrollResponder = sectionListRef.current?.getScrollResponder?.();
    if (scrollResponder && scrollResponder.scrollToEnd) {
      try {
        scrollResponder.scrollToEnd({ animated });
        return;
      } catch (error) {
      }
    }
    const lastSection = groupedMessages[groupedMessages.length - 1];
    const lastItemIndex = lastSection.data.length > 0 ? lastSection.data.length - 1 : 0;
    try {
      sectionListRef.current?.scrollToLocation({
        animated,
        sectionIndex: groupedMessages.length - 1,
        itemIndex: lastItemIndex,
        viewPosition: 1,
      });
    } catch (error) {
    }
  }, [groupedMessages]);

  useEffect(() => {
    scrollToBottomRef.current = scrollToBottom;
  }, [scrollToBottom]);

  useEffect(() => {
    if (!newestMessage || groupedMessages.length === 0) return undefined;
    if (lastScrolledMessageIdRef.current === newestMessage.id) return undefined;
    const previousNewestId = lastScrolledMessageIdRef.current;
    const isFirstFill = previousNewestId === null;
    lastScrolledMessageIdRef.current = newestMessage.id;
    const mine = !!newestMessage.pending || (!!newestMessage.senderUserAccountId && newestMessage.senderUserAccountId === callerUserAccountId);
    if (!isFirstFill && !mine && !isNearBottomRef.current) {
      const previousIndex = orderedMessages.findIndex((entry) => entry.id === previousNewestId);
      const newlyArrived = previousIndex === -1 ? 1 : orderedMessages.slice(previousIndex + 1).filter((entry) => !entry.pending && entry.senderUserAccountId !== callerUserAccountId).length;
      setUnseenCount((current) => current + Math.max(1, newlyArrived));
      return undefined;
    }
    const timer = setTimeout(() => {
      scrollToBottom(!isFirstFill);
    }, isFirstFill ? 300 : 80);
    return () => clearTimeout(timer);
  }, [newestMessage, orderedMessages, groupedMessages, callerUserAccountId, scrollToBottom]);

  const handleListScroll = useCallback((event) => {
    const { contentOffset, contentSize, layoutMeasurement } = event.nativeEvent;
    const distanceFromBottom = contentSize.height - layoutMeasurement.height - contentOffset.y;
    const nearBottom = distanceFromBottom < scaleHeight(120);
    isNearBottomRef.current = nearBottom;
    if (nearBottom) setUnseenCount(0);
  }, []);

  const handleListContentSizeChange = useCallback(() => {
    if (lastScrolledMessageIdRef.current !== null && isNearBottomRef.current) {
      scrollToBottomRef.current?.(true);
    }
  }, []);

  const openMessageActions = useCallback((entry) => {
    if (entry.pending || entry.isDeleted) return;
    setActionTarget(entry);
  }, []);

  const closeMessageActions = useCallback(() => {
    setActionTarget(null);
  }, []);

  const myReactionEmojiFor = useCallback((entry) => {
    if (!entry || !callerUserAccountId) return '';
    const mine = (entry.reactions || []).find((reaction) => reaction.userAccountId === callerUserAccountId);
    return mine ? mine.emoji : '';
  }, [callerUserAccountId]);

  const handleReactionPick = useCallback(async (entry, emoji) => {
    const currentEmoji = myReactionEmojiFor(entry);
    const nextEmoji = currentEmoji === emoji ? '' : emoji;
    closeMessageActions();
    const result = await reactTo(entry.id, nextEmoji);
    if (!result.ok && result.status !== 'removed' && result.status !== 'reacted') {
      showToast("Couldn't update your reaction", 'info');
    }
  }, [myReactionEmojiFor, closeMessageActions, reactTo]);

  const handleDeleteOwn = useCallback(async () => {
    if (!actionTarget) return;
    const messageId = actionTarget.id;
    closeMessageActions();
    const result = await deleteOwn(messageId);
    if (result.ok) {
      showToast('Message deleted', 'success');
    } else {
      showToast("Couldn't delete the message", 'info');
    }
  }, [actionTarget, closeMessageActions, deleteOwn]);

  const handleOpenReport = useCallback(() => {
    if (!actionTarget) return;
    setReportTarget(actionTarget);
    closeMessageActions();
  }, [actionTarget, closeMessageActions]);

  const handleOpenEmojiPicker = useCallback(() => {
    if (!EmojiPicker) {
      showToast('The emoji picker is unavailable on this build', 'info');
      closeMessageActions();
      return;
    }
    if (!actionTarget) return;
    setEmojiPickerTarget(actionTarget);
    closeMessageActions();
  }, [actionTarget, closeMessageActions]);

  const handleSubmitReport = useCallback(async (reason) => {
    if (!reportTarget) return;
    const messageId = reportTarget.id;
    setReportTarget(null);
    const result = await report(messageId, reason);
    if (result.status === 'reported') {
      showToast('Report submitted. Thank you.', 'success');
    } else if (result.status === 'alreadyReported') {
      showToast('You already reported this message', 'info');
    } else if (result.status === 'cannotReportOwn') {
      showToast('You cannot report your own message', 'info');
    } else if (result.status === 'messageGone') {
      showToast('That message is no longer available', 'info');
    } else if (result.status === 'invalidReason') {
      showToast('That reason is too long', 'info');
    } else {
      showToast("Couldn't submit the report", 'info');
    }
  }, [reportTarget, report]);

  const handleReplyToTarget = useCallback(() => {
    if (!actionTarget) return;
    setReplyTarget(buildReplyContext(actionTarget, sendersById));
    closeMessageActions();
    inputRef.current?.focus();
  }, [actionTarget, sendersById, closeMessageActions]);

  const handleSwipeReply = useCallback((entry) => {
    setReplyTarget(buildReplyContext(entry, sendersById));
    inputRef.current?.focus();
  }, [sendersById]);

  const handleFinishAccountPress = useCallback(() => {
    navigation.navigate('FinishAccount');
  }, [navigation]);

  const replyLineFor = useCallback((replyDisplay) => {
    if (replyDisplay.isDeleted) return 'Message deleted';
    if (replyDisplay.kind === 2) return 'Photo';
    if (replyDisplay.kind === 3) return 'Video';
    if (replyDisplay.kind === 4) return 'Voice message';
    return replyDisplay.preview || '';
  }, []);

  const handleReplyChipPress = useCallback((replyDisplay) => {
    if (!replyDisplay.parentIsLoaded) return;
    for (let sectionIndex = 0; sectionIndex < groupedMessages.length; sectionIndex++) {
      const itemIndex = groupedMessages[sectionIndex].data.findIndex((entry) => entry.id === replyDisplay.messageId);
      if (itemIndex === -1) continue;
      try {
        sectionListRef.current?.scrollToLocation({ animated: true, sectionIndex, itemIndex, viewPosition: 0.5 });
      } catch (error) {
      }
      if (highlightTimerRef.current) clearTimeout(highlightTimerRef.current);
      setHighlightedMessageId(replyDisplay.messageId);
      highlightTimerRef.current = setTimeout(() => setHighlightedMessageId(null), 1400);
      return;
    }
  }, [groupedMessages]);

  useEffect(() => {
    return () => {
      if (highlightTimerRef.current) clearTimeout(highlightTimerRef.current);
    };
  }, []);

  const renderReplyChip = useCallback((entry, mine) => {
    const replyDisplay = resolveReplyDisplay(entry.replyTo, messagesById, sendersById);
    if (!replyDisplay) return null;
    const senderName = replyDisplay.senderDisplayName || 'Former member';
    return (
      <TouchableOpacity
        style={[styles.replyChip, mine ? styles.replyChipMine : styles.replyChipHelper]}
        activeOpacity={0.7}
        onPress={() => handleReplyChipPress(replyDisplay)}
      >
        <CustomText style={[styles.replyChipSenderTxt, mine ? styles.replyChipSenderMine : styles.replyChipSenderHelper]} numberOfLines={1} ellipsizeMode="tail">{senderName}</CustomText>
        <CustomText style={[styles.replyChipBodyTxt, mine ? styles.replyChipBodyMine : styles.replyChipBodyHelper, replyDisplay.isDeleted && styles.replyChipDeletedTxt]} numberOfLines={1} ellipsizeMode="tail">{replyLineFor(replyDisplay)}</CustomText>
      </TouchableOpacity>
    );
  }, [styles, messagesById, sendersById, handleReplyChipPress, replyLineFor]);

  const renderReactions = useCallback((entry, mine) => {
    const reactions = entry.reactions || [];
    if (reactions.length === 0 || entry.isDeleted) return null;
    const countsByEmoji = new Map();
    reactions.forEach((reaction) => {
      countsByEmoji.set(reaction.emoji, (countsByEmoji.get(reaction.emoji) || 0) + 1);
    });
    const myEmoji = myReactionEmojiFor(entry);
    return (
      <View style={[styles.reactionsRow, mine && styles.reactionsRowMine]}>
        {[...countsByEmoji.entries()].map(([emoji, count]) => (
          <TouchableOpacity
            key={emoji}
            style={[styles.reactionChip, myEmoji === emoji && styles.reactionChipMine]}
            onPress={() => handleReactionPick(entry, emoji)}
          >
            <CustomText style={styles.reactionChipTxt}>
              {emoji} {count}
            </CustomText>
          </TouchableOpacity>
        ))}
      </View>
    );
  }, [styles, myReactionEmojiFor, handleReactionPick]);

  const renderMessageBody = useCallback((entry, mine) => {
    if (entry.isDeleted) {
      return (
        <CustomText style={[mine ? styles.myChatText : styles.helperChatText, styles.tombstoneTxt]}>
          Message deleted
        </CustomText>
      );
    }
    if (entry.kind === 2 && (entry.mediaUrl || entry.localUri)) {
      const aspectRatio = entry.mediaWidth && entry.mediaHeight ? entry.mediaWidth / entry.mediaHeight : 1;
      return (
        <TouchableOpacity style={styles.chatSingleImageView} activeOpacity={0.85} onPress={() => handleOpenViewer(entry)}>
          {entry.localUri ? (
            <Image source={{ uri: entry.localUri }} style={[styles.chatSingleImage, { aspectRatio }]} fadeDuration={0} />
          ) : (
            <RemoteImage uri={entry.mediaUrl} style={[styles.chatSingleImage, { aspectRatio }]} />
          )}
        </TouchableOpacity>
      );
    }
    if (entry.kind === 3) {
      if (!entry.mediaUrl) {
        return (
          <CustomText style={[mine ? styles.myChatText : styles.helperChatText, styles.mediaPlaceholderTxt]}>
            Sending video...
          </CustomText>
        );
      }
      return (
        <TouchableOpacity
          style={styles.videoBubbleBox}
          activeOpacity={0.85}
          onPress={() => handleOpenViewer(entry)}
        >
          {Video ? (
            <Video
              source={{ uri: baseService.getMediaUrl(entry.mediaUrl) }}
              style={styles.videoPreviewFill}
              paused
              muted
              resizeMode="cover"
              onError={() => {}}
            />
          ) : null}
          <View style={styles.videoPreviewScrim} />
          <View style={styles.videoPreviewPlayCircle}>
            <PlayGlyph size={scaleWidth(22)} color={White} />
          </View>
          <CustomText style={styles.videoPreviewDurationTxt}>{formatDuration(entry.mediaDurationSeconds)}</CustomText>
        </TouchableOpacity>
      );
    }
    if (entry.kind === 4) {
      if (!entry.mediaUrl) {
        return (
          <CustomText style={[mine ? styles.myChatText : styles.helperChatText, styles.mediaPlaceholderTxt]}>
            Sending voice message...
          </CustomText>
        );
      }
      return (
        <VoiceBubble
          entry={entry}
          styles={styles}
          mine={mine}
          active={playingVoiceId === entry.id}
          paused={voicePaused}
          positionMs={voicePositionMs}
          onToggle={handleToggleVoice}
          onSeek={handleSeekVoice}
        />
      );
    }
    const body = entry.body || '';
    const isExpanded = !!expanded[entry.id];
    const isLong = body.length > CHAR_LIMIT;
    const shownMessage = isExpanded ? body : (isLong ? body.slice(0, CHAR_LIMIT) : body);
    return (
      <CustomText style={mine ? styles.myChatText : styles.helperChatText}>
        {shownMessage}
        {isLong && (
          <CustomText
            style={mine ? styles.myReadMoreTxt : styles.helperReadMoreTxt}
            onPress={() => toggleExpanded(entry.id)}
          >
            {isExpanded ? ' Read less' : ' Read more...'}
          </CustomText>
        )}
      </CustomText>
    );
  }, [styles, expanded, toggleExpanded, playingVoiceId, voicePaused, voicePositionMs, handleToggleVoice, handleSeekVoice, handleOpenViewer]);

  const renderChatItem = useCallback(({ item }) => {
    const mine = !!item.senderUserAccountId && item.senderUserAccountId === callerUserAccountId;
    const timeStamp = formatMessageTime(item.createdAtUtc);
    const highlighted = item.id === highlightedMessageId;
    const swipeEnabled = !item.pending && !item.isDeleted && guestMessagesRemaining !== 0;

    if (!mine) {
      const sender = sendersById[item.senderUserAccountId];
      const senderName = sender ? sender.displayName : 'Former member';
      return (
        <SwipeToReplyRow enabled={swipeEnabled} onReply={() => handleSwipeReply(item)} styles={styles}>
        <Pressable style={[styles.helperChatMessageView, highlighted && styles.highlightedRow]} onLongPress={() => openMessageActions(item)}>
          <TouchableOpacity
            style={styles.helperProfilePictureContainer}
            disabled={!memberUsernamesById[item.senderUserAccountId]}
            onPress={() => openSenderProfile(item.senderUserAccountId)}
          >
            <Avatar
              uri={sender ? sender.profilePhotoUrl : null}
              color={sender ? sender.avatarColor : null}
              initial={senderName ? senderName.charAt(0).toUpperCase() : '?'}
              style={styles.helperProfilePicture}
              initialStyle={styles.avatarInitialTxt}
            />
          </TouchableOpacity>
          <View style={styles.helperChatView}>
            <View style={styles.helperChatTextBox}>
              <CustomText style={styles.helperSenderNameTxt} numberOfLines={1} ellipsizeMode="tail">
                {senderName}
              </CustomText>
              {renderReplyChip(item, false)}
              <View>
                {renderMessageBody(item, false)}
              </View>
              <View style={styles.helperTimeStamp}>
                <CustomText style={styles.helperTimeStampTxt}>{timeStamp}</CustomText>
              </View>
            </View>
            {renderReactions(item, false)}
          </View>
        </Pressable>
        </SwipeToReplyRow>
      );
    }

    const viewed = !item.pending && isViewedByEveryoneElse(item.sequence);
    return (
      <SwipeToReplyRow enabled={swipeEnabled} onReply={() => handleSwipeReply(item)} styles={styles}>
      <Pressable style={[styles.myChatMessageView, highlighted && styles.highlightedRow]} onLongPress={() => openMessageActions(item)} onPress={item.failed ? () => setFailedTarget(item) : undefined}>
        <View style={styles.myChatTextBox}>
          {renderReplyChip(item, true)}
          <View>
            {renderMessageBody(item, true)}
          </View>
          <View style={styles.myTimeStamp}>
            <CustomText style={styles.myTimeStampTxt}>{timeStamp}</CustomText>
            {item.failed ? (
              <View style={styles.failedIndicator}>
                <CustomText style={styles.failedIndicatorTxt}>!</CustomText>
              </View>
            ) : item.pending ? (
              <ClockIcon {...styles.sentIndicatorAndClockIcon} />
            ) : viewed ? (
              <ViewedMessageIcon {...styles.sentIndicatorAndClockIcon} />
            ) : (
              <SentMessageIcon {...styles.sentIndicatorAndClockIcon} />
            )}
          </View>
        </View>
        {item.failed && (
          <CustomText style={styles.notDeliveredTxt}>Not Delivered</CustomText>
        )}
        {renderReactions(item, true)}
      </Pressable>
      </SwipeToReplyRow>
    );
  }, [styles, callerUserAccountId, sendersById, isViewedByEveryoneElse, openMessageActions, renderMessageBody, renderReactions, renderReplyChip, highlightedMessageId, guestMessagesRemaining, handleSwipeReply, memberUsernamesById, openSenderProfile]);

  const renderSectionHeader = useCallback(({ section: { title } }) => (
    <View style={styles.dayHeader}>
      <CustomText style={styles.dayHeaderTxt}>{title}</CustomText>
    </View>
  ), [styles]);

  const ChatMessageSeparator = useCallback(() => <View style={styles.ChatMessageSeparator} />, [styles]);

  const deliverAttachment = useCallback(async (attachment, replyTo) => {
    if (attachment.kind === 2) {
      const result = await sendImage(attachment.file, replyTo);
      if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone' && result.status !== 'guestLimitReached') {
        if (result.status === 'tooLarge') {
          showToast('That image is too large', 'info');
        } else if (result.status === 'invalidMedia') {
          showToast("That file isn't a supported image", 'info');
        } else if (result.status !== 'unreachable') {
          showToast("Couldn't send your photo", 'info');
        }
      }
      return result;
    }
    if (attachment.kind === 3) {
      const result = await sendVideo(attachment.file, attachment.durationSeconds, replyTo);
      if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone' && result.status !== 'guestLimitReached') {
        if (result.status === 'tooLarge') {
          showToast('That video is too large to send', 'info');
        } else if (result.status === 'invalidMedia' || result.status === 'invalidDuration') {
          showToast("That video can't be sent", 'info');
        } else if (result.status !== 'unreachable') {
          showToast("Couldn't send your video", 'info');
        }
      }
      return result;
    }
    const result = await sendVoice(attachment.file, attachment.durationSeconds, replyTo);
    if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone' && result.status !== 'guestLimitReached') {
      if (result.status === 'tooLarge') {
        showToast('That voice message is too large to send', 'info');
      } else if (result.status === 'invalidMedia' || result.status === 'invalidDuration') {
        showToast("That recording can't be sent", 'info');
      } else if (result.status !== 'unreachable') {
        showToast("Couldn't send your voice message", 'info');
      }
    }
    return result;
  }, [sendImage, sendVideo, sendVoice]);

  const handleSend = useCallback(async () => {
    const body = chatText.trim();
    const attachments = stagedAttachments;
    if (!body && attachments.length === 0) return;
    const replyTo = replyTarget;
    setReplyTarget(null);
    if (attachments.length > 0) setStagedAttachments([]);
    if (body) {
      setChatText('');
      setComposerInputHeight(null);
    }
    const textCarriesReply = !!body;
    let attachmentIndex = 0;
    for (const attachment of attachments) {
      const attachmentReplyTo = !textCarriesReply && attachmentIndex === 0 ? replyTo : null;
      const attachmentResult = await deliverAttachment(attachment, attachmentReplyTo);
      attachmentIndex++;
      if (attachmentResult && attachmentResult.status === 'guestLimitReached') {
        if (body) {
          setChatText(body);
          setReplyTarget(replyTo);
        }
        return;
      }
    }
    if (!body) return;
    const result = await send(body, replyTo);
    if (!result.ok && result.status === 'guestLimitReached') {
      setChatText(body);
      setReplyTarget(replyTo);
      return;
    }
    if (!result.ok && result.status === 'notFriends') {
      showToast('You can only message friends', 'error');
      return;
    }
    if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone' && result.status !== 'unreachable') {
      showToast("Couldn't send your message", 'info');
      setChatText(body);
    }
  }, [chatText, stagedAttachments, replyTarget, deliverAttachment, send]);

  const handleChangeChatText = useCallback((text) => {
    setChatText(text);
    if (text.trim()) notifyTyping();
  }, [notifyTyping]);

  const handleComposerContentSizeChange = useCallback((event) => {
    setComposerInputHeight(event.nativeEvent.contentSize.height);
  }, []);

  const handleChatNamePress = useCallback(() => {
    if (isDirect) {
      if (directContact && directContact.username) navigation.push('Profile', { username: directContact.username });
      return;
    }
    setShowViewChatNameModal(true);
  }, [isDirect, directContact, navigation]);

  const handleRetryFailed = useCallback(() => {
    if (!failedTarget) return;
    const messageId = failedTarget.id;
    setFailedTarget(null);
    retrySend(messageId);
  }, [failedTarget, retrySend]);

  const handleDeleteFailed = useCallback(() => {
    if (!failedTarget) return;
    const messageId = failedTarget.id;
    setFailedTarget(null);
    deleteFailed(messageId);
  }, [failedTarget, deleteFailed]);

  const handleConfirmEditName = useCallback(async (newName) => {
    setShowEditChatNameModal(false);
    try {
      await renameChatGroup({ authToken, chatGroupId, name: newName }).unwrap();
      refreshNow();
    } catch (error) {
      showToast("Couldn't rename the group", 'info');
    }
  }, [authToken, chatGroupId, renameChatGroup, refreshNow]);

  const handleConfirmDeleteChatGroup = useCallback(async () => {
    setShowDeleteChatGroupModal(false);
    try {
      await deleteChatGroup({ authToken, chatGroupId }).unwrap();
      navigation.navigate('MainTabs', { screen: 'ChatGroups' });
    } catch (error) {
      showToast("Couldn't delete the group", 'info');
    }
  }, [authToken, chatGroupId, deleteChatGroup, navigation]);

  const handleConfirmLeaveChatGroup = useCallback(async () => {
    setShowLeaveChatGroupModal(false);
    try {
      const result = await leaveChatGroup({ authToken, chatGroupId }).unwrap();
      if (result.status === 'lastOwner') {
        showToast('You are the only owner. Delete the group or make it public first.', 'info');
        return;
      }
      navigation.navigate('MainTabs', { screen: 'ChatGroups' });
    } catch (error) {
      showToast("Couldn't leave the group", 'info');
    }
  }, [authToken, chatGroupId, leaveChatGroup, navigation]);

  const handleAcceptRequest = useCallback(async () => {
    if (!firstPendingMember) return;
    try {
      await approveMember({ authToken, chatGroupId, memberUserAccountId: firstPendingMember.userAccountId }).unwrap();
      refreshNow();
    } catch (error) {
      showToast("Couldn't approve the request", 'info');
    }
  }, [authToken, chatGroupId, firstPendingMember, approveMember, refreshNow]);

  const handleDeclineRequest = useCallback(async () => {
    if (!firstPendingMember) return;
    try {
      await rejectMember({ authToken, chatGroupId, memberUserAccountId: firstPendingMember.userAccountId }).unwrap();
      refreshNow();
    } catch (error) {
      showToast("Couldn't decline the request", 'info');
    }
  }, [authToken, chatGroupId, firstPendingMember, rejectMember, refreshNow]);

  const appendStagedAttachment = useCallback((attachment) => {
    setStagedAttachments((current) => {
      if (current.length >= MAX_STAGED_ATTACHMENTS) return current;
      return [...current, { ...attachment, stagedId: 'staged-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 8) }];
    });
  }, []);

  const handleRemoveStaged = useCallback((stagedId) => {
    setStagedAttachments((current) => current.filter((attachment) => attachment.stagedId !== stagedId));
  }, []);

  const handleSendImage = useCallback(async (image) => {
    if (stagedAttachments.length >= MAX_STAGED_ATTACHMENTS) {
      showToast('You can attach up to 10 items', 'info');
      return;
    }
    appendStagedAttachment({ kind: 2, file: { uri: image.path, type: image.mime, name: 'photo.jpg', width: image.width, height: image.height }, durationSeconds: 0 });
  }, [stagedAttachments, appendStagedAttachment]);

  const handlePickFromCamera = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const image = await ImagePicker.openCamera({ mediaType: 'photo', forceJpg: true, compressImageQuality: 0.9 });
      await handleSendImage(image);
    } catch (error) {
    }
  }, [handleSendImage]);

  const handlePickFromLibrary = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const picked = await ImagePicker.openPicker({ mediaType: 'photo', forceJpg: true, compressImageQuality: 0.9, multiple: true, maxFiles: MAX_STAGED_ATTACHMENTS });
      const images = Array.isArray(picked) ? picked : [picked];
      const room = MAX_STAGED_ATTACHMENTS - stagedAttachments.length;
      if (room <= 0 || images.length > room) {
        showToast('You can attach up to 10 items', 'info');
      }
      images.slice(0, Math.max(0, room)).forEach((image) => {
        appendStagedAttachment({ kind: 2, file: { uri: image.path, type: image.mime, name: 'photo.jpg', width: image.width, height: image.height }, durationSeconds: 0 });
      });
    } catch (error) {
    }
  }, [stagedAttachments, appendStagedAttachment]);

  const handleSendVideo = useCallback(async (video) => {
    const durationSeconds = Math.max(1, Math.round((video.duration || 0) / 1000));
    if (durationSeconds > VIDEO_MAX_SECONDS) {
      showToast('Videos can be up to 3 minutes long', 'info');
      return;
    }
    if (video.size && video.size > VIDEO_MAX_BYTES) {
      showToast('That video is too large to send', 'info');
      return;
    }
    if (stagedAttachments.length >= MAX_STAGED_ATTACHMENTS) {
      showToast('You can attach up to 10 items', 'info');
      return;
    }
    appendStagedAttachment({ kind: 3, file: { uri: video.path, type: video.mime, name: 'video.mp4', width: video.width, height: video.height }, durationSeconds });
  }, [stagedAttachments, appendStagedAttachment]);

  const handleRecordVideo = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const video = await ImagePicker.openCamera({ mediaType: 'video' });
      await handleSendVideo(video);
    } catch (error) {
    }
  }, [handleSendVideo]);

  const handlePickVideoFromLibrary = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const video = await ImagePicker.openPicker({ mediaType: 'video' });
      await handleSendVideo(video);
    } catch (error) {
    }
  }, [handleSendVideo]);

  const stopVoicePlayback = useCallback(async () => {
    setPlayingVoiceId(null);
    setVoicePositionMs(0);
    setVoicePaused(false);
    if (!Sound) return;
    Sound.removePlayBackListener();
    Sound.removePlaybackEndListener();
    try {
      await Sound.stopPlayer();
    } catch (error) {
    }
  }, []);

  const beginVoicePlayback = useCallback(async (entry, startRatio) => {
    await stopVoicePlayback();
    try {
      Sound.addPlayBackListener((event) => {
        const bucketedPosition = Math.floor(event.currentPosition / 250) * 250;
        setVoicePositionMs((current) => (current === bucketedPosition ? current : bucketedPosition));
      });
      Sound.addPlaybackEndListener(() => {
        setPlayingVoiceId(null);
        setVoicePositionMs(0);
        setVoicePaused(false);
        Sound.removePlayBackListener();
        Sound.removePlaybackEndListener();
      });
      await Sound.startPlayer(baseService.getMediaUrl(entry.mediaUrl));
      if (startRatio > 0 && typeof Sound.seekToPlayer === 'function') {
        const targetMs = Math.round((entry.mediaDurationSeconds || 0) * 1000 * startRatio);
        if (targetMs > 0) {
          await Sound.seekToPlayer(targetMs);
          setVoicePositionMs(targetMs);
        }
      }
      setPlayingVoiceId(entry.id);
      setVoicePaused(false);
    } catch (error) {
      await stopVoicePlayback();
      showToast("Couldn't play that voice message", 'info');
    }
  }, [stopVoicePlayback]);

  const handleSeekVoice = useCallback(async (entry, ratio) => {
    if (!Sound || isRecordingRef.current) return;
    if (playingVoiceId === entry.id && typeof Sound.seekToPlayer === 'function') {
      const targetMs = Math.round((entry.mediaDurationSeconds || 0) * 1000 * ratio);
      try {
        await Sound.seekToPlayer(targetMs);
        setVoicePositionMs(targetMs);
      } catch (error) {
      }
      return;
    }
    await beginVoicePlayback(entry, ratio);
  }, [playingVoiceId, beginVoicePlayback]);

  const viewerItems = useMemo(() => orderedMessages
    .filter((entry) => (entry.kind === 2 || entry.kind === 3) && !entry.isDeleted && (entry.mediaUrl || entry.localUri))
    .map((entry) => ({ key: entry.id, kind: entry.kind, mediaUrl: entry.mediaUrl || null, localUri: entry.localUri || null })), [orderedMessages]);
  const viewerItemsRef = useRef([]);
  viewerItemsRef.current = viewerItems;
  const stagedAttachmentsRef = useRef([]);
  stagedAttachmentsRef.current = stagedAttachments;

  const openViewerAt = useCallback((anchorKey, fallbackItem) => {
    stopVoicePlayback();
    const stagedViewerItems = stagedAttachmentsRef.current
      .filter((item) => item.kind === 2 || item.kind === 3)
      .map((item) => ({ key: item.stagedId, kind: item.kind, mediaUrl: null, localUri: item.file.uri }));
    const combinedItems = [...viewerItemsRef.current, ...stagedViewerItems];
    const index = combinedItems.findIndex((item) => item.key === anchorKey);
    if (index === -1) {
      if (!fallbackItem) return;
      setViewerTarget({ items: [fallbackItem], index: 0, nonce: Date.now() });
      return;
    }
    setViewerTarget({ items: combinedItems, index, nonce: Date.now() });
  }, [stopVoicePlayback]);

  const handleOpenViewer = useCallback((entry) => {
    openViewerAt(entry.id, { key: entry.id, kind: entry.kind, mediaUrl: entry.mediaUrl || null, localUri: entry.localUri || null });
  }, [openViewerAt]);

  const handleOpenStagedPreview = useCallback((attachment) => {
    openViewerAt(attachment.stagedId, null);
  }, [openViewerAt]);

  const handleToggleVoice = useCallback(async (entry) => {
    if (!Sound) {
      showToast('Voice playback is unavailable on this build', 'info');
      return;
    }
    if (isRecordingRef.current) return;
    if (playingVoiceId === entry.id) {
      try {
        if (voicePaused) {
          if (typeof Sound.resumePlayer === 'function') {
            await Sound.resumePlayer();
            setVoicePaused(false);
          } else {
            await beginVoicePlayback(entry, clampRatio((voicePositionMs / 1000) / Math.max(1, entry.mediaDurationSeconds || 1)));
          }
        } else {
          if (typeof Sound.pausePlayer === 'function') {
            await Sound.pausePlayer();
            setVoicePaused(true);
          } else {
            await stopVoicePlayback();
          }
        }
      } catch (error) {
        await stopVoicePlayback();
      }
      return;
    }
    await beginVoicePlayback(entry, 0);
  }, [playingVoiceId, voicePaused, voicePositionMs, stopVoicePlayback, beginVoicePlayback]);

  const finishRecording = useCallback(async (sendIt) => {
    if (!Sound || !isRecordingRef.current || recorderBusy) return;
    setRecorderBusy(true);
    isRecordingRef.current = false;
    try {
      const path = await Sound.stopRecorder();
      Sound.removeRecordBackListener();
      const seconds = Math.min(VOICE_MAX_SECONDS, Math.max(1, recordSecondsRef.current));
      setIsRecording(false);
      setRecordSeconds(0);
      recordSecondsRef.current = 0;
      if (sendIt && path) {
        const file = Platform.OS === 'ios'
          ? { uri: path, type: 'audio/mp4', name: 'voice.m4a' }
          : { uri: path, type: 'audio/mp4', name: 'voice.mp4' };
        appendStagedAttachment({ kind: 4, file, durationSeconds: seconds });
      }
    } catch (error) {
      setIsRecording(false);
      setRecordSeconds(0);
      recordSecondsRef.current = 0;
      Sound.removeRecordBackListener();
      try {
        await Sound.stopRecorder();
      } catch (finalizeError) {
      }
    } finally {
      setRecorderBusy(false);
    }
  }, [recorderBusy, appendStagedAttachment]);

  useEffect(() => {
    finishRecordingRef.current = finishRecording;
  }, [finishRecording]);

  useEffect(() => {
    if (!focused && isRecordingRef.current) {
      finishRecordingRef.current?.(false);
    }
  }, [focused]);

  useEffect(() => {
    const appStateSubscription = AppState.addEventListener('change', (nextAppState) => {
      if (nextAppState !== 'active' && isRecordingRef.current) {
        finishRecordingRef.current?.(false);
      }
    });
    return () => {
      appStateSubscription.remove();
    };
  }, []);

  const handleMicPress = useCallback(async () => {
    if (!Sound) {
      showToast('Voice recording is unavailable on this build', 'info');
      return;
    }
    if (recorderBusy || isRecordingRef.current) return;
    setRecorderBusy(true);
    try {
      if (Platform.OS === 'android') {
        const granted = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.RECORD_AUDIO);
        if (granted !== PermissionsAndroid.RESULTS.GRANTED) {
          showToast('Microphone permission is needed for voice messages', 'info');
          return;
        }
      }
      await stopVoicePlayback();
      try {
        await Sound.stopRecorder();
      } catch (resetError) {
      }
      Sound.removeRecordBackListener();
      recordSecondsRef.current = 0;
      setRecordSeconds(0);
      Sound.addRecordBackListener((event) => {
        const seconds = Math.floor(event.currentPosition / 1000);
        recordSecondsRef.current = seconds;
        setRecordSeconds(seconds);
        if (seconds >= VOICE_MAX_SECONDS && finishRecordingRef.current) {
          finishRecordingRef.current(true);
        }
      });
      try {
        await Sound.startRecorder();
      } catch (startError) {
        try {
          await Sound.stopRecorder();
        } catch (retryResetError) {
        }
        await new Promise((resolve) => setTimeout(resolve, 400));
        await Sound.startRecorder();
      }
      isRecordingRef.current = true;
      setIsRecording(true);
    } catch (error) {
      Sound.removeRecordBackListener();
      showToast("Couldn't start recording. Try again in a moment.", 'info');
    } finally {
      setRecorderBusy(false);
    }
  }, [recorderBusy, stopVoicePlayback]);

  useEffect(() => {
    return () => {
      if (!Sound) return;
      Sound.removePlayBackListener();
      Sound.removePlaybackEndListener();
      Sound.removeRecordBackListener();
      Sound.stopPlayer().catch(() => {});
      if (isRecordingRef.current) {
        Sound.stopRecorder().catch(() => {});
      }
    };
  }, []);

  const closeDropdown = useCallback(() => {
    setIsActive(false);
  }, []);

  const handleEllipsisPress = useCallback(() => {
    swallowNextCloseRef.current = true;
    setIsActive((curr) => !curr);
  }, []);

  const handleRootTouchEndCapture = useCallback((e) => {
    if (swallowNextCloseRef.current) {
      swallowNextCloseRef.current = false;
      return;
    }
    if (!isActive) return;
    const { pageX: x, pageY: y } = e.nativeEvent;
    const { chatDropdown, ellipsisBtn } = rectsRef.current;
    if (
      pointInRect(x, y, chatDropdown) ||
      pointInRect(x, y, ellipsisBtn)
    ) {
      return;
    }
    closeDropdown();
  }, [isActive, closeDropdown]);

  const pointInRect = useCallback((x, y, r) => {
    return !!r && x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height;
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

  const handleEditNamePressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    setShowEditChatNameModal(true);
  }, []);

  const handleMembersPressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    navigation.navigate('Members', { chatGroupId, isOwner: owner });
  }, [navigation, chatGroupId, owner]);

  const handleMakeChatPrivatePressIn = useCallback(async () => {
    swallowNextCloseRef.current = true;
    closeDropdown();
    try {
      await setChatGroupVisibility({ authToken, chatGroupId, isPublic: false }).unwrap();
      refreshNow();
    } catch (error) {
      showToast("Couldn't make the chat private", 'info');
    }
  }, [authToken, chatGroupId, setChatGroupVisibility, closeDropdown, refreshNow]);

  const handleMakeChatPublicPressIn = useCallback(async () => {
    swallowNextCloseRef.current = true;
    closeDropdown();
    try {
      await setChatGroupVisibility({ authToken, chatGroupId, isPublic: true }).unwrap();
      refreshNow();
    } catch (error) {
      showToast("Couldn't make the chat public", 'info');
    }
  }, [authToken, chatGroupId, setChatGroupVisibility, closeDropdown, refreshNow]);

  const handleDeleteChatPressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    setShowDeleteChatGroupModal(true);
  }, []);

  const handleLeaveChatGroupPress = useCallback(() => {
    swallowNextCloseRef.current = true;
    setShowLeaveChatGroupModal(true);
  }, []);

  useEffect(() => {
    if (isActive) {
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
  }, [isActive, measureToRect]);

  const showRequestToJoin = owner && !isPublic && !!firstPendingMember;

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: 0
  };
  const listBottomInset = composerBarHeight + (keyboardVisible ? keyboardHeightState + scaleHeight(6) : bottomSafeHeight);

  const renderedMembers = useMemo(() => {
    const memberNames = activeMemberEntries.map((member) => member.name || member.username || 'Member');
    const elements = [];
    if (memberNames.length === 0) {
      return elements;
    }
    if (memberNames.length === 1) {
      elements.push(
        <CustomText
          key="you-and-member"
          style={[styles.membersTxt, styles.lightMembersColor, styles.memberNameShrink]}
          numberOfLines={1}
          ellipsizeMode="tail"
        >
          {memberNames[0].length > 6 ? memberNames[0].slice(0, 6) + '\u2026' : memberNames[0]}
        </CustomText>
      );
    } else {
      const maxDisplay = 3;
      const displayedMembers = memberNames.slice(0, maxDisplay);
      displayedMembers.forEach((member, index) => {
        elements.push(
          <CustomText
            key={`member-${index}`}
            style={[styles.membersTxt, styles.lightMembersColor, styles.memberNameShrink]}
            numberOfLines={1}
            ellipsizeMode="tail"
          >
            {member.length > 6 ? member.slice(0, 6) + '\u2026' : member}
          </CustomText>
        );
        if (index < displayedMembers.length - 1) {
          elements.push(
            <CustomText
              key={`comma-${index}`}
              style={[styles.membersTxt, styles.lightMembersColor]}
            >
              ,
            </CustomText>
          );
        }
      });
      if (memberNames.length > maxDisplay) {
        elements.push(
          <CustomText
            key="comma-before-extra"
            style={[styles.membersTxt, styles.lightMembersColor]}
          >
            ,
          </CustomText>
        );
        elements.push(
          <CustomText
            key="extra-members"
            style={[styles.membersTxt, styles.blackMembersColor]}
            numberOfLines={1}
            ellipsizeMode="tail"
          >
            +{memberNames.length - maxDisplay}
          </CustomText>
        );
      }
    }
    return elements;
  }, [activeMemberEntries, styles.membersTxt, styles.lightMembersColor, styles.blackMembersColor, styles.memberNameShrink]);

  const typingMembersText = useMemo(() => {
    if (typingUserIds.length === 0) return '';
    const maxNames = 4;
    const typingNames = typingUserIds.map((userId) => {
      const senderName = sendersById[userId] && sendersById[userId].displayName;
      const memberName = memberNamesById[userId];
      const displayName = senderName || memberName;
      return displayName ? displayName.slice(0, 8) : 'Someone';
    });
    let formattedMembers;
    if (typingNames.length <= maxNames) {
      formattedMembers = typingNames.join(', ');
    } else {
      formattedMembers = typingNames.slice(0, maxNames).join(', ') + ` +${typingNames.length - maxNames}`;
    }
    const verb = typingNames.length === 1 ? 'is' : 'are';
    return `${'\u2022'.repeat(dotCount)} ${formattedMembers} ${verb} typing`;
  }, [typingUserIds, sendersById, memberNamesById, dotCount]);

  const myReactionEmojiForTarget = actionTarget ? myReactionEmojiFor(actionTarget) : '';
  const actionTargetIsMine = !!actionTarget && !!actionTarget.senderUserAccountId && actionTarget.senderUserAccountId === callerUserAccountId;
  const guestWalled = guestMessagesRemaining === 0;
  const showGuestCountdown = guestMessagesRemaining != null && guestMessagesRemaining > 0 && guestMessagesRemaining <= 3;

  return (
    <>
        <View style={{ flex: 1 }}>
            <View style={rootStyle} onTouchEndCapture={handleRootTouchEndCapture}>
                <View style={styles.topNav}>
                <View style={styles.chatHeaderRow}>
                    <View style={styles.backArrowAndChatNameRow}>
                        <View>
                            <TouchableOpacity
                                style={styles.BackArrow}
                                onPress={() => navigation.goBack()}
                            >
                                <BackArrow {...styles.largeIcons}/>
                            </TouchableOpacity>
                        </View>
                        <TouchableOpacity style={[styles.chatNameAndMembers, isDirect && styles.directHeaderRow]} onPress={handleChatNamePress}>
                            {isDirect && (
                                <Avatar
                                    uri={directContact ? directContact.profilePhotoUrl : null}
                                    color={directContact ? directContact.avatarColor : null}
                                    initial={directContact ? directContact.initial : '?'}
                                    style={styles.directHeaderAvatar}
                                    initialStyle={styles.directHeaderAvatarInitial}
                                />
                            )}
                            <View style={isDirect ? styles.directHeaderTextColumn : styles.headerTextColumn}>
                                <CustomText style={styles.chatNameTxt} numberOfLines={1} ellipsizeMode="tail">{headerTitle}</CustomText>
                                <View style={styles.membersRow}>
                                {isDirect ? (
                                    <CustomText style={[styles.membersTxt, styles.lightMembersColor, styles.memberNameShrink]} numberOfLines={1} ellipsizeMode="tail">
                                        {directContact && directContact.username ? '@' + directContact.username : ''}
                                    </CustomText>
                                ) : renderedMembers}
                                </View>
                            </View>
                        </TouchableOpacity>
                    </View>
                    {!isDirect && (
                    <View style={styles.privacyLabelAndEllipsisRow}>
                        <View style={[styles.privacyLabel, isPublic ? styles.publicBackgroundColor : styles.privateBackgroundColor]}>
                            <CustomText style={styles.privacyLabelTxt}>
                                {isPublic ? 'Public' : 'Private'}
                            </CustomText>
                        </View>
                        <View>
                            <TouchableOpacity
                                ref={ellipsisRef}
                                style={styles.ellipsisBackground}
                                onPressIn={handleEllipsisPress}
                            >
                                <EllipsisIcon {...styles.largeIcons} />
                            </TouchableOpacity>
                        </View>
                    </View>
                    )}
                </View>
                </View>
                <View style={styles.chatGroup}>
                    {status === 'loading' && (
                        <View style={styles.emptyStateView}>
                            <CustomText style={styles.emptyStateTxt}>Loading messages...</CustomText>
                        </View>
                    )}
                    {status === 'unreachable' && (
                        <View style={styles.emptyStateView}>
                            <CustomText style={styles.emptyStateTxt}>Couldn't load messages. Check your connection.</CustomText>
                            <TouchableOpacity style={styles.retryBtn} onPress={reload}>
                                <CustomText style={styles.retryBtnTxt}>Retry</CustomText>
                            </TouchableOpacity>
                        </View>
                    )}
                    {status === 'ok' && (
                        <SectionList
                            ref={sectionListRef}
                            sections={groupedMessages}
                            keyExtractor={item => item.id}
                            renderItem={renderChatItem}
                            renderSectionHeader={renderSectionHeader}
                            ItemSeparatorComponent={ChatMessageSeparator}
                            showsVerticalScrollIndicator={false}
                            stickySectionHeadersEnabled={false}
                            keyboardDismissMode={Platform.OS === 'ios' ? 'interactive' : 'on-drag'}
                            contentContainerStyle={{ paddingBottom: listBottomInset + scaleHeight(10) }}
                            onScroll={handleListScroll}
                            onContentSizeChange={handleListContentSizeChange}
                            scrollEventThrottle={100}
                            onScrollToIndexFailed={() => {}}
                            onStartReached={hasOlder ? loadOlder : undefined}
                            onStartReachedThreshold={0.2}
                            maintainVisibleContentPosition={{ minIndexForVisible: 0 }}
                            ListHeaderComponent={loadingOlder ? (
                                <CustomText style={styles.loadingOlderTxt}>Loading earlier messages...</CustomText>
                            ) : null}
                        />
                    )}
                    {unseenCount > 0 && (
                        <TouchableOpacity
                            style={[styles.scrollToBottomPill, { bottom: listBottomInset + scaleHeight(8) }]}
                            onPress={() => { setUnseenCount(0); scrollToBottom(true); }}
                        >
                            <CustomText style={styles.scrollToBottomPillTxt}>
                                {(unseenCount > 99 ? '99+' : String(unseenCount)) + ' new ' + (unseenCount === 1 ? 'message' : 'messages')}
                            </CustomText>
                        </TouchableOpacity>
                    )}
                    {isActive && (
                        <Pressable
                            ref={chatDropdownRef}
                            onLayout={() => measureToRect(chatDropdownRef, 'chatDropdown')}
                            style={styles.chatGroupDropdown}
                        >
                            {owner && (
                                <TouchableOpacity
                                    onPressIn={handleEditNamePressIn}
                                    onPressOut={closeDropdown}
                                    style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                >
                                    <CustomText style={styles.dropdownBlackTxt}>Edit name</CustomText>
                                    <EditIcon {...styles.dropdownIcons} />
                                </TouchableOpacity>
                            )}
                            <TouchableOpacity
                                onPressIn={handleMembersPressIn}
                                onPressOut={closeDropdown}
                                style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                            >
                                <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                                <MembersIcon {...styles.dropdownIcons} />
                                {pendingMemberEntries.length > 0 && <PendingMembersCircle {...styles.pendingMembersCircle} />}
                            </TouchableOpacity>
                            {owner && isPublic &&
                                (
                                    <TouchableOpacity
                                        onPressIn={handleMakeChatPrivatePressIn}
                                        onPressOut={closeDropdown}
                                        style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                    >
                                        <CustomText style={styles.dropdownBlackTxt}>Make Chat Private</CustomText>
                                        <PrivateIcon {...styles.dropdownIcons} />
                                    </TouchableOpacity>
                                )
                            }
                            {owner && !isPublic &&
                                (
                                    <TouchableOpacity
                                        onPressIn={handleMakeChatPublicPressIn}
                                        onPressOut={closeDropdown}
                                        style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                    >
                                        <CustomText style={styles.dropdownBlackTxt}>Make Chat Public</CustomText>
                                        <PrivateIcon {...styles.dropdownIcons} />
                                    </TouchableOpacity>
                                )
                            }
                            <TouchableOpacity
                                onPressIn={handleLeaveChatGroupPress}
                                onPressOut={closeDropdown}
                                style={[styles.deleteOption, owner && styles.chatGroupDropdownOptionsBorderBottom]}
                            >
                                <CustomText style={styles.dropdownRedTxt}>Leave group</CustomText>
                                <LeaveGroupIcon {...styles.dropdownIcons} />
                            </TouchableOpacity>
                            {owner &&
                                (
                                    <TouchableOpacity
                                        onPressIn={handleDeleteChatPressIn}
                                        onPressOut={closeDropdown}
                                        style={styles.deleteOption}
                                    >
                                        <CustomText style={styles.dropdownRedTxt}>Delete group</CustomText>
                                        <TrashIcon {...styles.dropdownIcons} />
                                    </TouchableOpacity>
                                )
                            }
                        </Pressable>
                    )}
                </View>
<View style={styles.composerOverlay} pointerEvents="box-none">
<View pointerEvents="none" style={styles.composerBackdrop} />
<Pressable
    style={styles.textBoxContainer}
    onLayout={(event) => setComposerBarHeight(event.nativeEvent.layout.height)}
    onPress={() => {
        if (isInputFocused) {
            Keyboard.dismiss();
        } else {
            inputRef.current?.focus();
        }
    }}
>
    {showRequestToJoin && (
    <Pressable style={styles.requestToJoin}>
        <View style={styles.requestToJoinRow}>
            <View style={styles.requestToJoinImageAndNames}>
                <Avatar
                    uri={firstPendingMember.profilePhotoUrl}
                    color={firstPendingMember.avatarColor}
                    initial={firstPendingMember.name ? firstPendingMember.name.charAt(0).toUpperCase() : '?'}
                    style={styles.requestToJoinProfilePicture}
                    initialStyle={styles.avatarInitialTxt}
                />
                <View>
                    <CustomText
                        style={styles.requestToJoinNameTxt}
                        numberOfLines={1}
                        ellipsizeMode="tail"
                    >{firstPendingMember.name}</CustomText>
                    <CustomText
                        style={styles.requestToJoinusernameTxt}
                        numberOfLines={1}
                        ellipsizeMode="tail"
                    >@{firstPendingMember.username}</CustomText>
                </View>
            </View>
            <View style={styles.requestToJoinOptions}>
                <TouchableOpacity
                    style={styles.requestToJoinAcceptBtn}
                    onPress={handleAcceptRequest}
                >
                    <CustomText style={styles.requestToJoinAcceptTxt}>Accept</CustomText>
                </TouchableOpacity>
                <TouchableOpacity
                    style={styles.requestToJoinXBtn}
                    onPress={handleDeclineRequest}
                >
                    <XIcon {...styles.xIcon}/>
                </TouchableOpacity>
            </View>
        </View>
    </Pressable>
    )}
    {typingMembersText !== '' && (
        <View style={styles.peopleTyping}>
            <CustomText style={styles.peopleTypingTxt} numberOfLines={1} ellipsizeMode="tail">
                {typingMembersText}
            </CustomText>
        </View>
    )}
    {replyTarget && !isRecording && (
        <View style={styles.replyBar}>
            <View style={styles.replyBarTextColumn}>
                <CustomText style={styles.replyBarSenderTxt} numberOfLines={1} ellipsizeMode="tail">
                    Replying to {replyTarget.senderDisplayName || 'Former member'}
                </CustomText>
                <CustomText style={styles.replyBarBodyTxt} numberOfLines={1} ellipsizeMode="tail">
                    {replyLineFor(replyTarget)}
                </CustomText>
            </View>
            <TouchableOpacity style={styles.replyBarCloseBtn} onPress={() => setReplyTarget(null)}>
                <CustomText style={styles.replyBarCloseTxt}>{'\u00D7'}</CustomText>
            </TouchableOpacity>
        </View>
    )}
    {stagedAttachments.length > 0 && !isRecording && (
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.stagedAttachmentBar} contentContainerStyle={styles.stagedAttachmentBarContent} keyboardShouldPersistTaps="handled">
            {stagedAttachments.map((attachment) => (
                <View key={attachment.stagedId} style={styles.stagedItem}>
                    {attachment.kind === 2 ? (
                        <TouchableOpacity activeOpacity={0.85} onPress={() => handleOpenStagedPreview(attachment)}>
                            <Image source={{ uri: attachment.file.uri }} style={styles.stagedThumb} fadeDuration={0} />
                        </TouchableOpacity>
                    ) : attachment.kind === 3 ? (
                        <TouchableOpacity style={styles.stagedThumb} activeOpacity={0.85} onPress={() => handleOpenStagedPreview(attachment)}>
                            {Video ? (
                                <Video source={{ uri: attachment.file.uri }} style={styles.stagedThumbFill} paused muted resizeMode="cover" onError={() => {}} />
                            ) : null}
                            <View style={styles.stagedThumbScrim} />
                            <PlayGlyph size={scaleWidth(16)} color={White} />
                        </TouchableOpacity>
                    ) : (
                        <View style={styles.stagedVoiceChip}>
                            <PlayGlyph size={scaleWidth(12)} color={Black} />
                            <CustomText style={styles.stagedVoiceTxt}>{formatDuration(attachment.durationSeconds)}</CustomText>
                        </View>
                    )}
                    <TouchableOpacity style={styles.stagedRemoveBtn} onPress={() => handleRemoveStaged(attachment.stagedId)}>
                        <CustomText style={styles.stagedRemoveTxt}>{'\u00D7'}</CustomText>
                    </TouchableOpacity>
                </View>
            ))}
        </ScrollView>
    )}
    {showGuestCountdown && (
        <TouchableOpacity style={styles.guestCountdownPill} onPress={handleFinishAccountPress}>
            <CustomText style={styles.guestCountdownTxt}>
                {guestMessagesRemaining === 1 ? '1 free guest message left. Create your account to keep chatting.' : guestMessagesRemaining + ' free guest messages left. Create your account to keep chatting.'}
            </CustomText>
        </TouchableOpacity>
    )}
    {isRecording ? (
        <View style={styles.recordingBar}>
            <TouchableOpacity style={styles.recordingCancelBtn} onPress={() => finishRecording(false)} disabled={recorderBusy}>
                <CustomText style={styles.recordingCancelTxt}>Cancel</CustomText>
            </TouchableOpacity>
            <View style={styles.recordingTimerView}>
                <View style={styles.recordingDot} />
                <CustomText style={styles.recordingTimerTxt}>{formatDuration(recordSeconds)}</CustomText>
            </View>
            <TouchableOpacity style={styles.recordingSendBtn} onPress={() => finishRecording(true)} disabled={recorderBusy}>
                <CustomText style={styles.recordingSendTxt}>Send</CustomText>
            </TouchableOpacity>
        </View>
    ) : guestWalled ? (
        <View style={styles.guestWall}>
            <CustomText style={styles.guestWallTxt}>You have used all of your free guest messages. Create your account to keep the conversation going.</CustomText>
            <TouchableOpacity style={styles.guestWallBtn} onPress={handleFinishAccountPress}>
                <CustomText style={styles.guestWallBtnTxt}>Finish Your Account</CustomText>
            </TouchableOpacity>
        </View>
    ) : (
    <View style={styles.inputRow} {...composerPanResponder.panHandlers}>
        <View style={styles.inputView}>
            <CustomTextInput
                ref={inputRef}
                style={[styles.input, composerInputHeight != null && { height: composerInputHeight }]}
                keyboardType="default"
                autoCapitalize="sentences"
                autoCorrect={false}
                value={chatText}
                onChangeText={handleChangeChatText}
                maxLength={MAX_MESSAGE_LENGTH}
                placeholderTextColor="rgba(35, 35, 35, 0.50)"
                placeholder="Message"
                multiline
                onContentSizeChange={handleComposerContentSizeChange}
                onFocus={() => setIsInputFocused(true)}
                onBlur={() => setIsInputFocused(false)}
            />
        </View>
        <View style={[styles.plusView, styles.textBoxBtnViews]}>
            <TouchableOpacity style={styles.textBoxBtns} onPress={() => { Keyboard.dismiss(); setShowAttachSheet(true); }}>
                <PlusIcon {...styles.largeIcons}/>
            </TouchableOpacity>
        </View>
        {!chatText.trim() && stagedAttachments.length === 0 ?
            (
                <View style={[styles.microphoneAndSendView, styles.textBoxBtnViews]}>
                    <TouchableOpacity style={styles.textBoxBtns} onPress={handleMicPress}>
                        <MicrophoneIcon {...styles.largeIcons}/>
                    </TouchableOpacity>
                </View>
            )
        :
            (
                <View style={[styles.microphoneAndSendView, styles.textBoxBtnViews]}>
                    <TouchableOpacity style={styles.textBoxBtns} onPress={handleSend}>
                        <SendMessageIcon {...styles.largeIcons}/>
                    </TouchableOpacity>
                </View>
            )
        }
    </View>
    )}
</Pressable>
                {showAttachSheet && (
                    <Pressable style={styles.attachSheetOverlay} onPress={() => setShowAttachSheet(false)}>
                        <Pressable style={styles.actionSheetCard} onPress={() => {}}>
                            <TouchableOpacity style={styles.actionSheetOption} onPress={handlePickFromCamera}>
                                <CustomText style={styles.actionSheetBlackTxt}>Take Photo</CustomText>
                            </TouchableOpacity>
                            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handlePickFromLibrary}>
                                <CustomText style={styles.actionSheetBlackTxt}>Photo Library</CustomText>
                            </TouchableOpacity>
                            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handleRecordVideo}>
                                <CustomText style={styles.actionSheetBlackTxt}>Record Video</CustomText>
                            </TouchableOpacity>
                            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handlePickVideoFromLibrary}>
                                <CustomText style={styles.actionSheetBlackTxt}>Video Library</CustomText>
                            </TouchableOpacity>
                            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={() => setShowAttachSheet(false)}>
                                <CustomText style={styles.actionSheetRedTxt}>Cancel</CustomText>
                            </TouchableOpacity>
                        </Pressable>
                    </Pressable>
                )}
<Animated.View style={{ height: keyboardSpacerAnim }} />
</View>
            </View>
        </View>
      {!!failedTarget && (
      <Modal visible transparent animationType="fade" onRequestClose={() => setFailedTarget(null)}>
        <Pressable style={styles.actionSheetOverlay} onPress={() => setFailedTarget(null)}>
          <Pressable style={styles.actionSheetCard} onPress={() => {}}>
            <TouchableOpacity style={styles.actionSheetOption} onPress={handleRetryFailed}>
              <CustomText style={styles.actionSheetBlackTxt}>Try Again</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handleDeleteFailed}>
              <CustomText style={styles.actionSheetRedTxt}>Delete Message</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={() => setFailedTarget(null)}>
              <CustomText style={styles.actionSheetBlackTxt}>Cancel</CustomText>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>
      )}
      {!!actionTarget && (
      <Modal visible transparent animationType="fade" onRequestClose={closeMessageActions}>
        <Pressable style={styles.actionSheetOverlay} onPress={closeMessageActions}>
          <Pressable style={styles.actionSheetCard} onPress={() => {}}>
            <View style={styles.reactionBarRow}>
              {QUICK_REACTIONS.map((emoji) => (
                <TouchableOpacity
                  key={emoji}
                  style={[styles.reactionBarBtn, myReactionEmojiForTarget === emoji && styles.reactionBarBtnActive]}
                  onPress={() => actionTarget && handleReactionPick(actionTarget, emoji)}
                >
                  <CustomText style={styles.reactionBarEmojiTxt}>{emoji}</CustomText>
                </TouchableOpacity>
              ))}
              <TouchableOpacity style={styles.reactionBarBtn} onPress={handleOpenEmojiPicker}>
                <CustomText style={styles.reactionBarEmojiTxt}>+</CustomText>
              </TouchableOpacity>
            </View>
            {!guestWalled && (
            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handleReplyToTarget}>
              <CustomText style={styles.actionSheetBlackTxt}>Reply</CustomText>
            </TouchableOpacity>
            )}
            {actionTargetIsMine ? (
              <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handleDeleteOwn}>
                <CustomText style={styles.actionSheetRedTxt}>Delete message</CustomText>
              </TouchableOpacity>
            ) : (
              <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={handleOpenReport}>
                <CustomText style={styles.actionSheetRedTxt}>Report message</CustomText>
              </TouchableOpacity>
            )}
            <TouchableOpacity style={[styles.actionSheetOption, styles.actionSheetOptionBorderTop]} onPress={closeMessageActions}>
              <CustomText style={styles.actionSheetBlackTxt}>Cancel</CustomText>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>
      )}
      {!!emojiPickerTarget && !!EmojiPicker && (
      <EmojiPicker
        open
        onClose={() => setEmojiPickerTarget(null)}
        onEmojiSelected={(selected) => {
          const target = emojiPickerTarget;
          setEmojiPickerTarget(null);
          if (target && selected && selected.emoji) {
            handleReactionPick(target, selected.emoji);
          }
        }}
      />
      )}
      {!!reportTarget && (
      <ReportMessageModal
        visible
        onSubmit={handleSubmitReport}
        onCancel={() => setReportTarget(null)}
      />
      )}
      <MediaViewerModal
        visible={!!viewerTarget}
        items={viewerTarget ? viewerTarget.items : null}
        initialIndex={viewerTarget ? viewerTarget.index : 0}
        sessionKey={viewerTarget ? viewerTarget.nonce : 0}
        onClose={() => { setViewerTarget(null); }}
      />
      <ViewChatNameModal
        visible={showViewChatNameModal}
        chatName={chatName}
        onClose={() => { setShowViewChatNameModal(false); }}
      />
      <EditChatNameModal
        visible={showEditChatNameModal}
        initialName={chatName}
        maxLen={100}
        onConfirm={handleConfirmEditName}
        onCancel={() => { setShowEditChatNameModal(false); }}
      />
      <DeleteChatGroupModal
        visible={showDeleteChatGroupModal}
        onConfirm={handleConfirmDeleteChatGroup}
        onCancel={() => { setShowDeleteChatGroupModal(false); }}
      />
      <LeaveChatGroupModal
        visible={showLeaveChatGroupModal}
        onConfirm={handleConfirmLeaveChatGroup}
        onCancel={() => { setShowLeaveChatGroupModal(false); }}
      />
    </>
  );
}