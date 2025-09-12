import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, KeyboardAvoidingView, Platform, Pressable, ScrollView } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
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
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import PrivateIcon from 'assets/images/global/private-chat-icon.svg';
import LeaveGroupIcon from 'assets/images/global/leave-and-remove-chat-icon.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';
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
        backgroundColor: '#F9F5EA', 
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
        backgroundColor: '#F9F9F9' 
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
        backgroundColor: '#F9F9F9'
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
        color: 'rgba(35, 35, 35, 0.50)'
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
        backgroundColor: '#F9F9F9'
    },
    privateBackgroundColor: {
        backgroundColor: 'rgba(237, 83, 112, 0.20)'
    },
    privacyLabelTxt: {
        fontSize: scaleFont(14),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.14),
        fontWeight: 600,
        color: Black
    },
    ellipsisBackground: { 
        width: scaleWidth(36), 
        height: scaleHeight(36), 
        borderRadius: scaleWidth(99), 
        backgroundColor: '#F9F9F9', 
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
        borderColor: 'rgba(238, 238, 238, 0.40)',
        backgroundColor: White,
        shadowColor: 'rgba(83, 26, 255, 0.1)',
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
        borderBottomColor: 'rgba(0, 0, 0, 0.25)' 
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
    chatGroup: {
        gap: scaleHeight(16),
        paddingHorizontal: scaleWidth(20),
        marginBottom: scaleHeight(16),
        flex: 1,
        minHeight: 0,
        position: 'relative',
        zIndex: 1000,
        elevation: 1000,
        overflow: 'visible'
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
        color: 'rgba(255, 255, 255, 0.50)' 
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
        color: 'rgba(35, 35, 35, 0.50)' 
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
        height: '100%'
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
        backgroundColor: 'rgba(35, 35, 35, 0.50)'

    },
    chatMessageImageTimeStampTxt: {
        fontSize: scaleFont(12),
        lineHeight: scaleLineHeight(18),
        letterSpacing: scaleLetterSpacing(-0.12),
        fontWeight: 500,
        color: White
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
        width: '100%',
        height: '100%',
        paddingLeft: scaleWidth(50),
        paddingRight: scaleWidth(16),
        fontSize: scaleFont(16),
        lineHeight: scaleLineHeight(21),
        letterSpacing: scaleLetterSpacing(-0.16),
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

const tabletStyles = StyleSheet.create({});

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
  const [chatText, setChatText] = useState('')
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

  const toggleExpanded = useCallback((id) => {
    setExpanded(prev => ({ ...prev, [id]: !prev[id] }));
  }, []);

  const CHAR_LIMIT = 400;

  const chatMessages = useMemo(() => [
    {
      id: '1',
      helperChat: true,
      message: "I hope the weather will be nice. Who's going paddle paddleboarding tomorrow? Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop",
      profilePicture: Image1,
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
      timeStamp: "11:18 am"
    },
    {
      id: '2',
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
      message: "Hi Marry",    
      timeStamp: "11:10 am"
    },    
    {
      id: '3',
      chatImages: [],        
      helperChat: false,
      message: "Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop Design your day with intention. Chip away at the hard parts first, then let easy wins refill your energy. Ask better questions, capture notes, and revisit them. Debug with curiosity, not blame. Ship something small, gather feedback, and improve. Protect your focus; silence noise, batch tasks, and take mindful breaks. Progress compounds when you return tomorrow; set a tiny next step before you stop",
      timeStamp: "11:15 am"
    },
    {
      id: '4',
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
      timeStamp: "1:10 pm"
    },
    {
      id: '6',
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
      message: "Hey Marry, let me know if you need anything",    
      timeStamp: "2:28 pm"
    },    
    {
      id: '5',
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
      timeStamp: "2:26 pm"
    }           
  ], []);

  const sortedChatMessages = useMemo(() => {
    return [...chatMessages].sort((a, b) => parseTime(a.timeStamp) - parseTime(b.timeStamp));
  }, [chatMessages]);

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

  const ItemSeparator = useCallback(() => <View style={{ height: scaleHeight(16) }} />, []);

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
                        {chat.isPublic ?
                            (
                                <View style={[styles.privacyLabel,styles.publicBackgroundColor]}>
                                    <CustomText style={styles.privacyLabelTxt}>Public</CustomText>
                                </View>
                            )
                        :
                            (
                                <View style={[styles.privacyLabel,styles.privateBackgroundColor]}>
                                    <CustomText style={styles.privacyLabelTxt}>{chat.isPublic}Private</CustomText>
                                </View>
                            )
                        }
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
                  <FlatList
                    data={sortedChatMessages}
                    renderItem={renderChatItem}
                    keyExtractor={item => item.id}
                    ItemSeparatorComponent={ItemSeparator}
                    showsVerticalScrollIndicator={false}
                  />
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
                <View style={styles.textBoxContainer}>
                    <View style={styles.inputRow}>
                        <View style={styles.inputView}>
                            <CustomTextInput
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
                </View>      
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