import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, SectionList, useWindowDimensions, KeyboardAvoidingView, Platform, Pressable, ScrollView, Keyboard, Modal, PermissionsAndroid } from 'react-native';
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
import Avatar from 'src/components/Avatar';
import RemoteImage from 'src/components/RemoteImage';
import ReportMessageModal from 'src/components/ReportMessageModal';
import ImagePicker from 'react-native-image-crop-picker';
import baseService from 'src/services/baseService';

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
import useChatMessages from 'src/hooks/useChatMessages';
import { selectCachedChatGroup } from 'src/store/chatMessagesApi';
import { useListMembersQuery, useRenameChatGroupMutation, useSetChatGroupVisibilityMutation, useDeleteChatGroupMutation, useLeaveChatGroupMutation, useApproveMemberMutation, useRejectMemberMutation } from 'src/store/chatGroupsApi';
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
import Image1 from 'assets/images/placeholderProfiles/profile-1.png';

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
        backgroundColor: White
    },
    profileAndLogin: {
        height: scaleHeight(44),
        paddingHorizontal: scaleWidth(20),
        width: '100%',
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center'
    },
    welcomeBackTxt: {
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: Black
    },
    profileImage: { 
        width: scaleWidth(44), 
        height: scaleHeight(44), 
        borderRadius: scaleWidth(99),
        resizeMode: 'contain' 
    },
    loginBg: { 
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
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(24),
        letterSpacing: scaleLetterSpacing(-0.16),
        fontWeight: 600,
        color: White
    },
    chatHeaderRow: {
        paddingHorizontal: scaleWidth(20),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between'
    },
    backArrowAndChatNameRow: {
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
    chatNameAndMembers: {
        gap: scaleHeight(2)
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
    privacyLabelAndEllipsisRow: {
        gap: scaleWidth(12),
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
        marginBottom: scaleHeight(16),
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
        paddingHorizontal: scaleWidth(20)
    },
    inputRow: {
        height: scaleHeight(54),
        paddingVertical: scaleHeight(6),
        paddingHorizontal: scaleWidth(6),
        borderRadius: scaleWidth(50),
        width: '100%',
        position: 'relative',
        backgroundColor: White
    },
    inputView: {
        width: '100%',
        height: '100%'
    },
    input: {
        paddingLeft: scaleWidth(56),
        paddingRight: scaleWidth(56),
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.16),
        width: '100%',
        height: '100%',
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
        top: scaleHeight(6),
        left: scaleWidth(6),
        backgroundColor: "#F9F5EA"
    },  
    microphoneAndSendView: {
        top: scaleHeight(6),
        right: scaleWidth(6),
        backgroundColor: HappyColor
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
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(6),
        backgroundColor: Charcoal
    },
    videoPlayGlyphTxt: {
        fontSize: scaleFont(28),
        color: White
    },
    videoDurationTxt: {
        fontSize: scaleFont(12),
        fontWeight: 600,
        color: White
    },
    videoPlayer: {
        width: scaleWidth(220),
        height: scaleHeight(160),
        borderRadius: scaleWidth(12),
        backgroundColor: Charcoal
    },
    voiceRow: {
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
    voicePlayGlyphTxt: {
        fontSize: scaleFont(16),
        color: Black
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
        backgroundColor: White
    },
    profileAndLogin: {
        height: 84,
        paddingHorizontal: scaleWidth(24),
        width: '100%',
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center'
    },
    welcomeBackTxt: {
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        fontWeight: 600,
        color: Black
    },
    profileImage: { 
        width: 83.23,
        height: 83.23, 
        borderRadius: scaleWidth(132.792),
        resizeMode: 'contain' 
    },
    loginBg: { 
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
    chatHeaderRow: {
        paddingHorizontal: scaleWidth(24),
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between'
    },
    backArrowAndChatNameRow: {
        gap: scaleWidth(16.1),
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
        gap: scaleHeight(2.68)
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
    privacyLabelAndEllipsisRow: {
        gap: scaleWidth(16.1),
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
        marginBottom: scaleHeight(20),
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
        paddingHorizontal: scaleWidth(24)
    },
    inputRow: {
        height: scaleHeight(72.43201),
        paddingVertical: scaleHeight(8.05),
        paddingHorizontal: scaleWidth(8.05),
        borderRadius: scaleWidth(67.067),
        width: '100%',
        position: 'relative',
        backgroundColor: White
    },
    inputView: {
        width: '100%',
        height: '100%'
    },
    input: {
        paddingLeft: scaleWidth(60),
        paddingRight: scaleWidth(60),
        fontSize: scaleFont(20),
        lineHeight: scaleLineHeight(30),
        letterSpacing: scaleLetterSpacing(-0.2),
        width: '100%',
        height: '100%',
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
        top: scaleHeight(8.05),
        left: scaleWidth(8.05),
        backgroundColor: "#F9F5EA"
    },  
    microphoneAndSendView: {
        top: scaleHeight(8.05),
        right: scaleWidth(8.05),
        backgroundColor: HappyColor
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
        alignItems: 'center',
        justifyContent: 'center',
        gap: scaleHeight(6),
        backgroundColor: Charcoal
    },
    videoPlayGlyphTxt: {
        fontSize: scaleFont(28),
        color: White
    },
    videoDurationTxt: {
        fontSize: scaleFont(12),
        fontWeight: 600,
        color: White
    },
    videoPlayer: {
        width: scaleWidth(220),
        height: scaleHeight(160),
        borderRadius: scaleWidth(12),
        backgroundColor: Charcoal
    },
    voiceRow: {
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
    voicePlayGlyphTxt: {
        fontSize: scaleFont(16),
        color: Black
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
});

const QUICK_REACTIONS = ['\u2764\uFE0F', '\uD83D\uDC4D', '\uD83D\uDE0A', '\uD83D\uDE22', '\u203C\uFE0F', '\u2753'];
const CHAR_LIMIT = 400;
const MAX_MESSAGE_LENGTH = 4096;

function formatTimeStamp(createdAtUtc) {
    const date = new Date(createdAtUtc);
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true }).toLowerCase();
}

const VOICE_MAX_SECONDS = 300;
const VIDEO_MAX_SECONDS = 180;
const VIDEO_MAX_BYTES = 100 * 1024 * 1024;

function formatDuration(totalSeconds) {
    const seconds = Math.max(0, Math.round(totalSeconds || 0));
    const minutes = Math.floor(seconds / 60);
    const remainder = seconds % 60;
    return minutes + ':' + String(remainder).padStart(2, '0');
}

export default function ChatGroup() {
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const [isActive, setIsActive] = useState(false);
  const [chatText, setChatText] = useState('');
  const inputRef = useRef(null);
  const [isInputFocused, setIsInputFocused] = useState(false);
  const [dotCount, setDotCount] = useState(1);
  const [authToken, setAuthToken] = useState(null);
  const [actionTarget, setActionTarget] = useState(null);
  const [reportTarget, setReportTarget] = useState(null);
  const [emojiPickerTarget, setEmojiPickerTarget] = useState(null);
  const [showAttachSheet, setShowAttachSheet] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recorderBusy, setRecorderBusy] = useState(false);
  const [recordSeconds, setRecordSeconds] = useState(0);
  const [playingVoiceId, setPlayingVoiceId] = useState(null);
  const [voicePositionMs, setVoicePositionMs] = useState(0);
  const [playingVideoId, setPlayingVideoId] = useState(null);
  const recordSecondsRef = useRef(0);
  const isRecordingRef = useRef(false);
  const finishRecordingRef = useRef(null);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const focused = useIsFocused();
  const cameFromLogin = route.params?.from === 'login';
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
  const exitHandledRef = useRef(false);

  const [expanded, setExpanded] = useState({});

  const cachedGroup = useSelector((state) => selectCachedChatGroup(state, chatGroupId));
  const chatName = cachedGroup?.title ?? '';
  const isPublic = cachedGroup ? !!cachedGroup.isPublic : true;
  const owner = !!cachedGroup?.owner;

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
    notifyTyping,
    isViewedByEveryoneElse,
    reload,
  } = useChatMessages({ authToken, chatGroupId, focused });

  const membersQuery = useListMembersQuery(
    { authToken, chatGroupId },
    { skip: !authToken || !chatGroupId }
  );
  const activeMemberEntries = membersQuery.data?.members ?? [];
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
      navigation.navigate('ChatGroups');
    } else if (status === 'groupGone') {
      exitHandledRef.current = true;
      showToast('This group is no longer available', 'info');
      navigation.navigate('ChatGroups');
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
    const now = new Date();
    const todayKey = now.toISOString().split('T')[0];
    const yesterday = new Date(now.getTime() - 86400000);
    const yesterdayKey = yesterday.toISOString().split('T')[0];

    const groups = {};
    orderedMessages.forEach((entry) => {
      const createdAt = new Date(entry.createdAtUtc);
      const dateKey = createdAt.toISOString().split('T')[0];
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
        const date = new Date(key);
        title = date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
      }
      return {
        title,
        data: groups[key],
      };
    });
  }, [orderedMessages]);

  const newestMessageId = orderedMessages.length > 0 ? orderedMessages[orderedMessages.length - 1].id : null;

  useEffect(() => {
    if (!newestMessageId || groupedMessages.length === 0) return undefined;
    if (lastScrolledMessageIdRef.current === newestMessageId) return undefined;
    lastScrolledMessageIdRef.current = newestMessageId;
    const timer = setTimeout(() => {
      const lastSection = groupedMessages[groupedMessages.length - 1];
      const lastItemIndex = lastSection.data.length > 0 ? lastSection.data.length - 1 : 0;
      try {
        sectionListRef.current?.scrollToLocation({
          animated: false,
          sectionIndex: groupedMessages.length - 1,
          itemIndex: lastItemIndex,
          viewPosition: 1,
        });
      } catch (error) {
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [newestMessageId, groupedMessages]);

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
        <View style={styles.chatSingleImageView}>
          {entry.localUri ? (
            <Image source={{ uri: entry.localUri }} style={[styles.chatSingleImage, { aspectRatio }]} fadeDuration={0} />
          ) : (
            <RemoteImage uri={entry.mediaUrl} style={[styles.chatSingleImage, { aspectRatio }]} />
          )}
        </View>
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
      if (Video && playingVideoId === entry.id) {
        return (
          <Video
            source={{ uri: baseService.getMediaUrl(entry.mediaUrl) }}
            style={styles.videoPlayer}
            controls
            paused={false}
            resizeMode="contain"
            onEnd={() => setPlayingVideoId(null)}
            onError={() => {
              setPlayingVideoId(null);
              showToast("Couldn't play that video", 'info');
            }}
          />
        );
      }
      return (
        <TouchableOpacity
          style={styles.videoBubbleBox}
          onPress={() => {
            if (!Video) {
              showToast('Video playback is unavailable on this build', 'info');
              return;
            }
            setPlayingVideoId(entry.id);
          }}
        >
          <CustomText style={styles.videoPlayGlyphTxt}>{'\u25B6'}</CustomText>
          <CustomText style={styles.videoDurationTxt}>{formatDuration(entry.mediaDurationSeconds)}</CustomText>
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
      const isPlayingThisVoice = playingVoiceId === entry.id;
      const voiceLabel = isPlayingThisVoice
        ? formatDuration(voicePositionMs / 1000) + ' / ' + formatDuration(entry.mediaDurationSeconds)
        : formatDuration(entry.mediaDurationSeconds);
      return (
        <View style={styles.voiceRow}>
          <TouchableOpacity style={styles.voicePlayBtn} onPress={() => handleToggleVoice(entry)}>
            <CustomText style={styles.voicePlayGlyphTxt}>{isPlayingThisVoice ? '\u23F8' : '\u25B6'}</CustomText>
          </TouchableOpacity>
          <CustomText style={mine ? styles.myChatText : styles.helperChatText}>{voiceLabel}</CustomText>
        </View>
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
  }, [styles, expanded, toggleExpanded, playingVideoId, playingVoiceId, voicePositionMs, handleToggleVoice]);

  const renderChatItem = useCallback(({ item }) => {
    const mine = !!item.senderUserAccountId && item.senderUserAccountId === callerUserAccountId;
    const timeStamp = formatTimeStamp(item.createdAtUtc);

    if (!mine) {
      const sender = sendersById[item.senderUserAccountId];
      const senderName = sender ? sender.displayName : 'Former member';
      return (
        <Pressable style={styles.helperChatMessageView} onLongPress={() => openMessageActions(item)}>
          <TouchableOpacity
            style={styles.helperProfilePictureContainer}
            disabled={!memberUsernamesById[item.senderUserAccountId]}
            onPress={() => openSenderProfile(item.senderUserAccountId)}
          >
            <Avatar
              uri={sender ? sender.profilePhotoUrl : null}
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
      );
    }

    const viewed = !item.pending && isViewedByEveryoneElse(item.sequence);
    return (
      <Pressable style={styles.myChatMessageView} onLongPress={() => openMessageActions(item)}>
        <View style={styles.myChatTextBox}>
          <View>
            {renderMessageBody(item, true)}
          </View>
          <View style={styles.myTimeStamp}>
            <CustomText style={styles.myTimeStampTxt}>{timeStamp}</CustomText>
            {item.pending ? (
              <ClockIcon {...styles.sentIndicatorAndClockIcon} />
            ) : viewed ? (
              <ViewedMessageIcon {...styles.sentIndicatorAndClockIcon} />
            ) : (
              <SentMessageIcon {...styles.sentIndicatorAndClockIcon} />
            )}
          </View>
        </View>
        {renderReactions(item, true)}
      </Pressable>
    );
  }, [styles, callerUserAccountId, sendersById, isViewedByEveryoneElse, openMessageActions, renderMessageBody, renderReactions, memberUsernamesById, openSenderProfile]);

  const renderSectionHeader = useCallback(({ section: { title } }) => (
    <View style={styles.dayHeader}>
      <CustomText style={styles.dayHeaderTxt}>{title}</CustomText>
    </View>
  ), [styles]);

  const ChatMessageSeparator = useCallback(() => <View style={styles.ChatMessageSeparator} />, [styles]);

  const handleSend = useCallback(async () => {
    const body = chatText.trim();
    if (!body) return;
    setChatText('');
    const result = await send(body);
    if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone') {
      showToast("Couldn't send your message", 'info');
      setChatText(body);
    }
  }, [chatText, send]);

  const handleChangeChatText = useCallback((text) => {
    setChatText(text);
    if (text.trim()) notifyTyping();
  }, [notifyTyping]);

  const handleLoginPressIn = useCallback(() => {
    navigation.navigate('LoginOptions');
  }, [navigation]);

  const handleConfirmEditName = useCallback(async (newName) => {
    setShowEditChatNameModal(false);
    try {
      await renameChatGroup({ authToken, chatGroupId, name: newName }).unwrap();
    } catch (error) {
      showToast("Couldn't rename the group", 'info');
    }
  }, [authToken, chatGroupId, renameChatGroup]);

  const handleConfirmDeleteChatGroup = useCallback(async () => {
    setShowDeleteChatGroupModal(false);
    try {
      await deleteChatGroup({ authToken, chatGroupId }).unwrap();
      navigation.navigate('ChatGroups');
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
      navigation.navigate('ChatGroups');
    } catch (error) {
      showToast("Couldn't leave the group", 'info');
    }
  }, [authToken, chatGroupId, leaveChatGroup, navigation]);

  const handleAcceptRequest = useCallback(async () => {
    if (!firstPendingMember) return;
    try {
      await approveMember({ authToken, chatGroupId, memberUserAccountId: firstPendingMember.userAccountId }).unwrap();
    } catch (error) {
      showToast("Couldn't approve the request", 'info');
    }
  }, [authToken, chatGroupId, firstPendingMember, approveMember]);

  const handleDeclineRequest = useCallback(async () => {
    if (!firstPendingMember) return;
    try {
      await rejectMember({ authToken, chatGroupId, memberUserAccountId: firstPendingMember.userAccountId }).unwrap();
    } catch (error) {
      showToast("Couldn't decline the request", 'info');
    }
  }, [authToken, chatGroupId, firstPendingMember, rejectMember]);

  const handleSendImage = useCallback(async (image) => {
    const result = await sendImage({ uri: image.path, type: image.mime, name: 'photo.jpg', width: image.width, height: image.height });
    if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone') {
      if (result.status === 'tooLarge') {
        showToast('That image is too large', 'info');
      } else if (result.status === 'invalidMedia') {
        showToast("That file isn't a supported image", 'info');
      } else {
        showToast("Couldn't send your photo", 'info');
      }
    }
  }, [sendImage]);

  const handlePickFromCamera = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const image = await ImagePicker.openCamera({ mediaType: 'photo', compressImageQuality: 0.9 });
      await handleSendImage(image);
    } catch (error) {
    }
  }, [handleSendImage]);

  const handlePickFromLibrary = useCallback(async () => {
    setShowAttachSheet(false);
    try {
      const image = await ImagePicker.openPicker({ mediaType: 'photo', compressImageQuality: 0.9 });
      await handleSendImage(image);
    } catch (error) {
    }
  }, [handleSendImage]);

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
    const result = await sendVideo({ uri: video.path, type: video.mime, name: 'video.mp4', width: video.width, height: video.height }, durationSeconds);
    if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone') {
      if (result.status === 'tooLarge') {
        showToast('That video is too large to send', 'info');
      } else if (result.status === 'invalidMedia' || result.status === 'invalidDuration') {
        showToast("That video can't be sent", 'info');
      } else {
        showToast("Couldn't send your video", 'info');
      }
    }
  }, [sendVideo]);

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
    if (!Sound) return;
    Sound.removePlayBackListener();
    Sound.removePlaybackEndListener();
    try {
      await Sound.stopPlayer();
    } catch (error) {
    }
  }, []);

  const handleToggleVoice = useCallback(async (entry) => {
    if (!Sound) {
      showToast('Voice playback is unavailable on this build', 'info');
      return;
    }
    if (isRecordingRef.current) return;
    if (playingVoiceId === entry.id) {
      await stopVoicePlayback();
      return;
    }
    await stopVoicePlayback();
    try {
      Sound.addPlayBackListener((event) => {
        setVoicePositionMs(event.currentPosition);
      });
      Sound.addPlaybackEndListener(() => {
        setPlayingVoiceId(null);
        setVoicePositionMs(0);
        Sound.removePlayBackListener();
        Sound.removePlaybackEndListener();
      });
      await Sound.startPlayer(baseService.getMediaUrl(entry.mediaUrl));
      setPlayingVoiceId(entry.id);
    } catch (error) {
      await stopVoicePlayback();
      showToast("Couldn't play that voice message", 'info');
    }
  }, [playingVoiceId, stopVoicePlayback]);

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
        const result = await sendVoice(file, seconds);
        if (!result.ok && result.status !== 'notMember' && result.status !== 'groupGone') {
          if (result.status === 'tooLarge') {
            showToast('That voice message is too large to send', 'info');
          } else if (result.status === 'invalidMedia' || result.status === 'invalidDuration') {
            showToast("That recording can't be sent", 'info');
          } else {
            showToast("Couldn't send your voice message", 'info');
          }
        }
      }
    } catch (error) {
      setIsRecording(false);
      setRecordSeconds(0);
      recordSecondsRef.current = 0;
      Sound.removeRecordBackListener();
    } finally {
      setRecorderBusy(false);
    }
  }, [recorderBusy, sendVoice]);

  useEffect(() => {
    finishRecordingRef.current = finishRecording;
  }, [finishRecording]);

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
      await Sound.startRecorder();
      isRecordingRef.current = true;
      setIsRecording(true);
    } catch (error) {
      Sound.removeRecordBackListener();
      showToast("Couldn't start recording. Check microphone permission.", 'info');
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
    navigation.navigate('Members');
  }, [navigation]);

  const handleMakeChatPrivatePressIn = useCallback(async () => {
    swallowNextCloseRef.current = true;
    closeDropdown();
    try {
      await setChatGroupVisibility({ authToken, chatGroupId, isPublic: false }).unwrap();
    } catch (error) {
      showToast("Couldn't make the chat private", 'info');
    }
  }, [authToken, chatGroupId, setChatGroupVisibility, closeDropdown]);

  const handleMakeChatPublicPressIn = useCallback(async () => {
    swallowNextCloseRef.current = true;
    closeDropdown();
    try {
      await setChatGroupVisibility({ authToken, chatGroupId, isPublic: true }).unwrap();
    } catch (error) {
      showToast("Couldn't make the chat public", 'info');
    }
  }, [authToken, chatGroupId, setChatGroupVisibility, closeDropdown]);

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
    paddingBottom: bottomSafeHeight
  };

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
          style={[styles.membersTxt, styles.lightMembersColor]}
          numberOfLines={1}
          ellipsizeMode="tail"
        >
          {memberNames[0].slice(0, 6)}
        </CustomText>
      );
    } else {
      const maxDisplay = 3;
      const displayedMembers = memberNames.slice(0, maxDisplay);
      displayedMembers.forEach((member, index) => {
        elements.push(
          <CustomText
            key={`member-${index}`}
            style={[styles.membersTxt, styles.lightMembersColor]}
            numberOfLines={1}
            ellipsizeMode="tail"
          >
            {member.slice(0, 6)}
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
  }, [activeMemberEntries, styles.membersTxt, styles.lightMembersColor, styles.blackMembersColor]);

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

  return (
    <>
        <KeyboardAvoidingView
            style={{ flex: 1 }}
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            keyboardVerticalOffset={statusBarHeight}
        >
            <View style={rootStyle} onTouchEndCapture={handleRootTouchEndCapture}>
                <View style={styles.topNav}>
                {cameFromLogin ? (
                        <View style={styles.profileAndLogin}>
                        <CustomText style={styles.welcomeBackTxt}>Welcome Back!</CustomText>
                        <View>
                            <TouchableOpacity onPress={() => navigation.navigate('Profile')}>
                            <Image source={Image1} style={styles.profileImage} fadeDuration={0} />
                            </TouchableOpacity>
                        </View>
                        </View>
                ) : (
                        <View style={[styles.profileAndLogin, styles.loginBg]}>
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
                        <View style={styles.chatNameAndMembers}>
                            <CustomText style={styles.chatNameTxt} numberOfLines={1} ellipsizeMode="tail">{chatName}</CustomText>
                            <View style={styles.membersRow}>
                            {renderedMembers}
                            </View>
                        </View>
                    </View>
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
                            onScrollToIndexFailed={() => {}}
                            onStartReached={hasOlder ? loadOlder : undefined}
                            onStartReachedThreshold={0.2}
                            maintainVisibleContentPosition={{ minIndexForVisible: 0 }}
                            ListHeaderComponent={loadingOlder ? (
                                <CustomText style={styles.loadingOlderTxt}>Loading earlier messages...</CustomText>
                            ) : null}
                        />
                    )}
                    {typingMembersText !== '' && (
                        <View style={styles.peopleTyping}>
                            <CustomText style={styles.peopleTypingTxt} numberOfLines={1} ellipsizeMode="tail">
                                {typingMembersText}
                            </CustomText>
                        </View>
                    )}
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
                            {owner &&
                                (
                                    <TouchableOpacity
                                        onPressIn={handleMembersPressIn}
                                        onPressOut={closeDropdown}
                                        style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                    >
                                        <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                                        <MembersIcon {...styles.dropdownIcons} />
                                        {pendingMemberEntries.length > 0 && <PendingMembersCircle {...styles.pendingMembersCircle} />}
                                    </TouchableOpacity>
                                )
                            }
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
<Pressable
    style={styles.textBoxContainer}
    onPress={() => {
        if (isInputFocused) {
            Keyboard.dismiss();
        } else {
            inputRef.current?.focus();
        }
    }}
>
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
    ) : (
    <View style={styles.inputRow}>
        <View style={styles.inputView}>
            <CustomTextInput
                ref={inputRef}
                style={styles.input}
                keyboardType="default"
                autoCapitalize="sentences"
                autoCorrect={false}
                value={chatText}
                onChangeText={handleChangeChatText}
                maxLength={MAX_MESSAGE_LENGTH}
                placeholderTextColor="rgba(35, 35, 35, 0.50)"
                placeholder="Message"
                returnKeyType="done"
                onSubmitEditing={handleSend}
                blurOnSubmit
                onFocus={() => setIsInputFocused(true)}
                onBlur={() => setIsInputFocused(false)}
            />
        </View>
        <View style={[styles.plusView, styles.textBoxBtnViews]}>
            <TouchableOpacity style={styles.textBoxBtns} onPress={() => { Keyboard.dismiss(); setShowAttachSheet(true); }}>
                <PlusIcon {...styles.largeIcons}/>
            </TouchableOpacity>
        </View>
        {!chatText.trim() ?
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
            </View>
        </KeyboardAvoidingView>
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