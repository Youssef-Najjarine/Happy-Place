import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, SectionList, useWindowDimensions, KeyboardAvoidingView, Platform, Pressable, ScrollView, Keyboard } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute } from '@react-navigation/native';
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
import ChatGroupPhoto from 'assets/images/placeholderProfiles/chatGroup-photo.png';
import Image1 from 'assets/images/placeholderProfiles/profile-1.png';
import Image2 from 'assets/images/placeholderProfiles/profile-2.png';
import Image3 from 'assets/images/placeholderProfiles/profile-3.png';
import Image4 from 'assets/images/placeholderProfiles/profile-4.png';
import Image5 from 'assets/images/placeholderProfiles/profile-5.png';
import Image6 from 'assets/images/placeholderProfiles/profile-6.png';
import Image7 from 'assets/images/placeholderProfiles/profile-7.jpg';
import Image8 from 'assets/images/placeholderProfiles/profile-8.jpg';
import Image9 from 'assets/images/placeholderProfiles/profile-9.jpg';
import Image10 from 'assets/images/placeholderProfiles/profile-10.jpg';
import Image11 from 'assets/images/placeholderProfiles/profile-11.jpg';
import Image12 from 'assets/images/placeholderProfiles/profile-12.jpg';
import Image13 from 'assets/images/placeholderProfiles/profile-13.jpg';
import Image14 from 'assets/images/placeholderProfiles/profile-14.jpg';
import Image15 from 'assets/images/placeholderProfiles/profile-15.jpg';
import Image16 from 'assets/images/placeholderProfiles/profile-16.jpg';
import Image17 from 'assets/images/placeholderProfiles/profile-17.jpg';
import Image18 from 'assets/images/placeholderProfiles/profile-18.jpg';
import Image19 from 'assets/images/placeholderProfiles/profile-19.jpg';
import Image20 from 'assets/images/placeholderProfiles/profile-20.jpg';

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
        resizeMode: 'repeat'
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
    microphoneView: {
        top: scaleHeight(6),
        right: scaleWidth(6),
        backgroundColor: HappyColor
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
        resizeMode: 'repeat'
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
    microphoneView: {
        top: scaleHeight(8.05),
        right: scaleWidth(8.05),
        backgroundColor: HappyColor
    }
});

function parseTime(timeStr) {
  const [time, ampm] = timeStr.split(' ');
  let [hour, min] = time.split(':').map(Number);
  if (ampm.toLowerCase() === 'pm' && hour !== 12) hour += 12;
  if (ampm.toLowerCase() === 'am' && hour === 12) hour = 0;
  return hour * 60 + min;
}

export default function ChatGroup() {
  const [showEditChatNameModal, setShowEditChatNameModal] = useState(false);
  const [showDeleteChatGroupModal, setShowDeleteChatGroupModal] = useState(false);
  const [showLeaveChatGroupModal, setShowLeaveChatGroupModal] = useState(false);
  const [isActive, setIsActive] = useState(false);
  const [chatText, setChatText] = useState('');
  const [showRequestToJoin, setShowRequestToJoin] = useState(false);
  const inputRef = useRef(null);
  const [isInputFocused, setIsInputFocused] = useState(false);
  const [dotCount, setDotCount] = useState(1);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const cameFromLogin = route.params?.from === 'login';
  const [chat, setChat] = useState(() => ({
    isPublic: true,
    owner: true,
    pendingMembers: true,
    chatName: "I'm depressed!",
    members: ["JaydonJaydonJaydonJaydon", "Mary", "Yulia", "Youssef", "Abe", "Omar", "Ed"],
  }));
  const chatDropdownRef = useRef(null);
  const ellipsisRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    chatDropdown: null,
    ellipsisBtn: null,
  });

  const [expanded, setExpanded] = useState({});

  // Animation for typing dots
  useEffect(() => {
    const interval = setInterval(() => {
      setDotCount(prev => (prev % 3) + 1);
    }, 500); // Update every 500ms
    return () => clearInterval(interval);
  }, []);

  const toggleExpanded = useCallback((id) => {
    setExpanded(prev => ({ ...prev, [id]: !prev[id] }));
  }, []);

  const CHAR_LIMIT = 400;
  const requestToJoin = {
    profilePicture: Image11,
    name: "Jaydon Najjarine Smith",
    username: "jaydon671"
  };
  const chatMessages = useMemo(() => [
    {
      id: '1',
      helperChat: true,
      chatImages: [],
      message: "Good morning everyone! Let's have a productive day.",
      profilePicture: Image1,
      timeStamp: "8:30 am",
      createdAt: new Date(2025, 8, 5, 8, 30),
    },
    {
      id: '2',
      helperChat: false,
      chatImages: [],
      message: "Morning!",
      timeStamp: "8:32 am",
      createdAt: new Date(2025, 8, 5, 8, 32),
    },
    {
      id: '3',
      helperChat: true,
      profilePicture: Image2,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "10:00 am"
        }
      ],
      message: "",
      timeStamp: "10:00 am",
      createdAt: new Date(2025, 8, 5, 10, 0),
    },
    {
      id: '4',
      helperChat: true,
      chatImages: [],
      profilePicture: Image3,
      message: "Hey, does anyone have the report from last meeting?",
      timeStamp: "2:00 pm",
      createdAt: new Date(2025, 8, 12, 14, 0),
    },
    {
      id: '5',
      helperChat: false,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:05 pm"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:05 pm"
        }
      ],
      message: "",
      timeStamp: "2:05 pm",
      createdAt: new Date(2025, 8, 12, 14, 5),
    },
    {
      id: '6',
      helperChat: true,
      chatImages: [],
      profilePicture: Image4,
      message: "I can send it over.",
      timeStamp: "2:10 pm",
      createdAt: new Date(2025, 8, 12, 14, 10),
    },
    {
      id: '7',
      helperChat: false,
      chatImages: [],
      message: "Thanks a lot!",
      timeStamp: "2:15 pm",
      createdAt: new Date(2025, 8, 12, 14, 15),
    },
    {
      id: '8',
      helperChat: false,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "11:10 am"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "11:10 am"
        }
      ],      
      message: "Hi Mary",     
      timeStamp: "11:10 am",
      createdAt: new Date(2025, 8, 13, 11, 10),
    },     
    {
      id: '9',
      chatImages: [],        
      helperChat: false,
      message: "Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop I hope the weather will be nice. Who's going paddleboarding tomorrow? Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop",
      timeStamp: "11:15 am",
      createdAt: new Date(2025, 8, 13, 11, 15),
    },
    {
      id: '10',
      helperChat: true,
      profilePicture: Image5,
      message: "I hope the weather will be nice. Who's going paddleboarding tomorrow? Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stopI hope the weather will be nice. Who's going paddleboarding tomorrow? Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop",
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "11:18 am"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "11:18 am"
        }        
      ],
      timeStamp: "11:18 am",
      createdAt: new Date(2025, 8, 13, 11, 18),
    },
    {
      id: '11',
      helperChat: false,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "1:10 pm"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "1:10 pm"
        }        
      ],      
      message: "",     
      timeStamp: "1:10 pm",
      createdAt: new Date(2025, 8, 13, 13, 10),
    },
    {
      id: '12',
      helperChat: true,
      profilePicture: Image1,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:26 pm"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:26 pm"
        }        
      ],      
      message: "",     
      timeStamp: "2:26 pm",
      createdAt: new Date(2025, 8, 13, 14, 26),
    },     
    {
      id: '13',
      helperChat: true,
      profilePicture: Image1,
      chatImages: [
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:28 pm"
        },
        {
          image: ChatGroupPhoto,
          imageTimeStamp: "2:28 pm"
        }        
      ],      
      message: "Hey Mary, let me know if you need anything",     
      timeStamp: "2:28 pm",
      createdAt: new Date(2025, 8, 13, 14, 28),
    }          
  ], []);

  const groupedMessages = useMemo(() => {
    const now = new Date(2025, 8, 15);
    const todayKey = now.toISOString().split('T')[0];
    const yesterday = new Date(now.getTime() - 86400000);
    const yesterdayKey = yesterday.toISOString().split('T')[0];

    const sorted = [...chatMessages].sort((a, b) => a.createdAt - b.createdAt);
    const groups = {};
    sorted.forEach((msg) => {
      const dateKey = msg.createdAt.toISOString().split('T')[0];
      if (!groups[dateKey]) {
        groups[dateKey] = [];
      }
      groups[dateKey].push(msg);
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
      const sectionMessages = groups[key].sort((a, b) => parseTime(a.timeStamp) - parseTime(b.timeStamp));
      return {
        title,
        data: sectionMessages,
      };
    });
  }, [chatMessages]);

  const renderSectionHeader = useCallback(({ section: { title } }) => (
    <View style={styles.dayHeader}>
      <CustomText style={styles.dayHeaderTxt}>{title}</CustomText>
    </View>
  ), [styles]);

  const renderChatItem = useCallback(({ item }) => {
    const isExpanded = !!expanded[item.id];
    const isLong = item.message.length > CHAR_LIMIT;
    const shownMessage = isExpanded ? item.message : (isLong ? item.message.slice(0, CHAR_LIMIT) : item.message);

    if (item.helperChat) {
      return (
        <View style={styles.helperChatMessageView}>
          <View style={styles.helperProfilePictureContainer}>
            <Image source={item.profilePicture} style={styles.helperProfilePicture} fadeDuration={0} />
          </View>
          <View style={styles.helperChatView}>
            {item.message.trim() && (
                <View style={styles.helperChatTextBox}>
                    <View>
                        <CustomText style={styles.helperChatText}>
                        {shownMessage}
                        {isLong && (
                            <CustomText
                            style={styles.helperReadMoreTxt}
                            onPress={() => toggleExpanded(item.id)}
                            >
                            {isExpanded ? ' Read less' : ' Read more...'}
                            </CustomText>
                        )}
                        </CustomText>
                    </View>
                    <View style={styles.helperTimeStamp}>
                        <CustomText style={styles.helperTimeStampTxt}>{item.timeStamp}</CustomText>
                    </View>
                </View>
            )}
            {item.chatImages.length > 0 && (
              <View style={styles.chatMessageImages}>
                {item.chatImages.map((img, idx) => (
                  <View key={idx} style={styles.chatMessageImageView}>
                    <Image source={img.image} style={[styles.chatMessageImage, styles.helperChatMessageImage]} fadeDuration={0} />
                    <View style={styles.chatMessageImageTimeStamp}>
                        <CustomText style={styles.chatMessageImageTimeStampTxt}>{img.imageTimeStamp}</CustomText>
                        <ClockIcon {...styles.sentIndicatorAndClockIcon}/>
                    </View>
                  </View>
                ))}
              </View>
            )}
          </View>
        </View>
      );
    } else {
      return (
        <View style={styles.myChatMessageView}>
            {item.message.trim() && (
                <View style={styles.myChatTextBox}>
                    <View>
                        <CustomText style={styles.myChatText}>
                            {shownMessage}
                            {isLong && (
                            <CustomText
                                style={styles.myReadMoreTxt}
                                onPress={() => toggleExpanded(item.id)}
                            >
                                {isExpanded ? ' Read less' : ' Read more...'}
                            </CustomText>
                            )}
                        </CustomText>
                    </View>
                    <View style={styles.myTimeStamp}>
                        <CustomText style={styles.myTimeStampTxt}>{item.timeStamp}</CustomText>
                        <ViewedMessageIcon {...styles.sentIndicatorAndClockIcon} />
                    </View>
                </View>
            )}
            {item.chatImages.length > 0 && (
              <View style={styles.chatMessageImages}>
                {item.chatImages.map((img, idx) => (
                  <View key={idx} style={styles.chatMessageImageView}>
                    <Image source={img.image} style={[styles.chatMessageImage, styles.myChatMessageImage]} fadeDuration={0} />
                    <View style={styles.chatMessageImageTimeStamp}>
                        <CustomText style={styles.chatMessageImageTimeStampTxt}>{img.imageTimeStamp}</CustomText>
                        <ClockIcon {...styles.sentIndicatorAndClockIcon}/>
                    </View>
                  </View>
                ))}
              </View>
            )}
        </View>
      );
    }
  }, [styles, expanded, toggleExpanded]);

  const ChatMessageSeparator = useCallback(() => <View style={styles.ChatMessageSeparator} />, []);

  const handleTextSubmit = useCallback(() => {
    setChatText('');
  }, [setChatText]);
  const handleLoginPressIn = useCallback(() => {
    navigation.navigate('LoginOptions');
  }, [navigation]);

  const handleConfirmEditName = useCallback((newName) => {
    setShowEditChatNameModal(false);
    setChat(prev => ({ ...prev, chatName: newName }));
  }, []);

  const handleConfirmDeleteChatGroup = useCallback(() => {
    setShowDeleteChatGroupModal(false);
    navigation.navigate('ChatGroups');
  }, []);

  const handleConfirmLeaveChatGroup = useCallback(() => {
    setShowLeaveChatGroupModal(false);
    navigation.navigate('ChatGroups');
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

  const handleMakeChatPrivatePressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    setChat(prev => ({ ...prev, isPublic: false }));
    closeDropdown();
  }, [closeDropdown]);

  const handleMakeChatPublicPressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
    setChat(prev => ({ ...prev, isPublic: true }));
    closeDropdown();
  }, [closeDropdown]);

  const handleShareChatPressIn = useCallback(() => {
    swallowNextCloseRef.current = true;
  }, []);

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

  useEffect(() => {
    setShowRequestToJoin(!chat.isPublic && chat.pendingMembers);
  }, [chat.isPublic, chat.pendingMembers]);

  const handleAcceptRequest = useCallback(() => {
    setChat(prev => ({
      ...prev,
      members: [...prev.members, requestToJoin.name],
      pendingMembers: false
    }));
    setShowRequestToJoin(false);
  }, []);

  const handleDeclineRequest = useCallback(() => {
    setShowRequestToJoin(false);
  }, []);

  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight,
    paddingBottom: bottomSafeHeight
  };

  const renderedMembers = useMemo(() => {
    const elements = [];
    if (chat.members.length === 1) {
      elements.push(
        <CustomText 
          key="you-and-member" 
          style={[styles.membersTxt, styles.lightMembersColor]} 
          numberOfLines={1} 
          ellipsizeMode="tail"
        >
          You & {chat.members[0].slice(0, 6)}
        </CustomText>
      );
    } else {
      const maxDisplay = 3;
      const displayedMembers = chat.members.slice(0, maxDisplay);
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
      if (chat.members.length > maxDisplay) {
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
            +{chat.members.length - maxDisplay}
          </CustomText>
        );
      }
    }
    return elements;
  }, [chat.members, styles.membersTxt, styles.lightMembersColor, styles.blackMembersColor]);

  const typingMembersText = useMemo(() => {
    const maxNames = 4;
    let formattedMembers;
    if (chat.members.length <= maxNames) {
      formattedMembers = chat.members.map(member => member.slice(0, 8)).join(', ');
    } else {
      const firstNames = chat.members.slice(0, maxNames).map(member => member.slice(0, 8));
      formattedMembers = firstNames.join(', ') + ` +${chat.members.length - maxNames}`;
    }
    return `${'•'.repeat(dotCount)} ${formattedMembers} is typing`;
  }, [chat.members, dotCount]);

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
                            <CustomText style={styles.chatNameTxt}>{chat.chatName}</CustomText>
                            <View style={styles.membersRow}>
                            {renderedMembers}
                            </View>
                        </View>
                    </View>
                    <View style={styles.privacyLabelAndEllipsisRow}>
                        <View style={[styles.privacyLabel, chat.isPublic ? styles.publicBackgroundColor : styles.privateBackgroundColor]}>
                            <CustomText style={styles.privacyLabelTxt}>
                                {chat.isPublic ? 'Public' : 'Private'}
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
                    <SectionList
                    sections={groupedMessages}
                    keyExtractor={item => item.id}
                    renderItem={renderChatItem}
                    renderSectionHeader={renderSectionHeader}
                    ItemSeparatorComponent={ChatMessageSeparator}
                    showsVerticalScrollIndicator={false}
                    stickySectionHeadersEnabled={false}
                    />
                    <View style={styles.peopleTyping}>
                        <CustomText style={styles.peopleTypingTxt} numberOfLines={1} ellipsizeMode="tail">
                            {typingMembersText}
                        </CustomText>
                    </View>
                    {showRequestToJoin && (
                    <Pressable style={styles.requestToJoin}>
                        <View style={styles.requestToJoinRow}>
                            <View style={styles.requestToJoinImageAndNames}>
                                <Image 
                                    source={requestToJoin.profilePicture}
                                    style={styles.requestToJoinProfilePicture}
                                />
                                <View>
                                    <CustomText 
                                        style={styles.requestToJoinNameTxt}
                                        numberOfLines={1}
                                        ellipsizeMode="tail"
                                    >{requestToJoin.name}</CustomText>
                                    <CustomText 
                                        style={styles.requestToJoinusernameTxt}
                                        numberOfLines={1}
                                        ellipsizeMode="tail"
                                    >@{requestToJoin.username}</CustomText>
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
                            {chat.owner && (
                                <TouchableOpacity
                                    onPressIn={handleEditNamePressIn}
                                    onPressOut={closeDropdown}
                                    style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                >
                                    <CustomText style={styles.dropdownBlackTxt}>Edit name</CustomText>
                                    <EditIcon {...styles.dropdownIcons} />
                                </TouchableOpacity>
                            )}
                            {chat.owner && 
                                (
                                    <TouchableOpacity
                                        onPressIn={handleMembersPressIn}
                                        onPressOut={closeDropdown}
                                        style={[styles.chatGroupDropdownOptions, styles.chatGroupDropdownOptionsBorderBottom]}
                                    >
                                        <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                                        <MembersIcon {...styles.dropdownIcons} />
                                        {chat.pendingMembers && chat.owner && <PendingMembersCircle {...styles.pendingMembersCircle} />}
                                    </TouchableOpacity>
                                )
                            }                    
                            {chat.owner && chat.isPublic &&
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
                            {chat.owner && !chat.isPublic &&
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
                                style={[styles.deleteOption, chat.owner && styles.chatGroupDropdownOptionsBorderBottom]}
                            >
                                <CustomText style={styles.dropdownRedTxt}>Leave group</CustomText>
                                <LeaveGroupIcon {...styles.dropdownIcons} />
                            </TouchableOpacity>
                            {chat.owner && 
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
    <View style={styles.inputRow}>
        <View style={styles.inputView}>
            <CustomTextInput
                ref={inputRef}
                style={styles.input}
                keyboardType="default"
                autoCapitalize="sentences"
                autoCorrect={false}
                maxLength={100}
                value={chatText}
                onChangeText={setChatText}
                placeholderTextColor="rgba(35, 35, 35, 0.50)"
                placeholder="Message"
                returnKeyType="done"
                onSubmitEditing={handleTextSubmit}
                blurOnSubmit
                onFocus={() => setIsInputFocused(true)}
                onBlur={() => setIsInputFocused(false)}
            />
        </View>
        <View style={[styles.plusView, styles.textBoxBtnViews]}>
            <TouchableOpacity style={styles.textBoxBtns}>
                <PlusIcon {...styles.largeIcons}/>
            </TouchableOpacity>
        </View>
        {!chatText.trim() ? 
            (
                <View style={[styles.microphoneView, styles.textBoxBtnViews]}>
                    <TouchableOpacity style={styles.textBoxBtns}>
                        <MicrophoneIcon {...styles.largeIcons}/>
                    </TouchableOpacity>    
                </View> 
            )
        :
            (
                <View style={[styles.microphoneView, styles.textBoxBtnViews]}>
                    <TouchableOpacity style={styles.textBoxBtns}>
                        <SendMessageIcon {...styles.largeIcons}/>
                    </TouchableOpacity>    
                </View>
            )
        }                                    
    </View>
</Pressable> 
            </View>
        </KeyboardAvoidingView>
      <EditChatNameModal
        visible={showEditChatNameModal}
        initialName={chat.chatName}
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