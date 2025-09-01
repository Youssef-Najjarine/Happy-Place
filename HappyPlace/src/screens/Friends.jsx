import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import SearchIcon from 'assets/images/global/search-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import ProfileIcon from 'assets/images/friends/profile-black-icon.svg';
import ChatIcon from 'assets/images/friends/chat-bubble-icon.svg';
import UnfriendIcon from 'assets/images/friends/unfriend-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import PlusIcon from 'assets/images/global/white-plus-icon.svg';
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
    backgroundColor: '#F9F9F9'
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
  friendsViewType: {
    paddingVertical: scaleHeight(2),
    paddingHorizontal: scaleWidth(2),
    borderRadius: scaleWidth(67.067),
    height: scaleHeight(39),
    width: '100%',
    backgroundColor: '#F9F9F9',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendsViewTypeSelectedBtn: {
    width: scaleWidth(164.5),
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
    width: scaleWidth(164.5),
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
    backgroundColor: '#F9F9F9',
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
  ellipsisBackground: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    backgroundColor: '#F9F9F9',
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
    shadowColor: 'rgba(83, 26, 255, 0.1)',
    elevation: 12,
    position: 'absolute',
    borderColor: 'rgba(238, 238, 238, 0.40)',
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
    borderBottomColor: 'rgba(0, 0, 0, 0.25)' 
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
    backgroundColor: '#F9F9F9'
  },
  xIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28)
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
    backgroundColor: '#F9F9F9'
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
  friendsViewType: {
    paddingVertical: scaleHeight(4),
    paddingHorizontal: scaleWidth(4),
    borderRadius: scaleWidth(132.792),
    height: scaleHeight(56.34),
    width: '100%',
    backgroundColor: '#F9F9F9',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  friendsViewTypeSelectedBtn: {
    width: scaleWidth(344),
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
    width: scaleWidth(344),
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
    color: '#1D1E25'
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
    backgroundColor: '#F9F9F9',
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
  friendFullName: {
    width: scaleWidth(450.291),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(450.291),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  ellipsisBackground: {
      borderRadius: scaleWidth(132.792),
      width: 78.14,
      height: 78.14,
    backgroundColor: '#F9F9F9',
    justifyContent: 'center',
    alignItems: 'center'
  },
  ellipsis: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557)
  },
  friendDropdown: {
    top: scaleHeight(27.95),
    right: scaleWidth(26.83),
    width: scaleWidth(215.995),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowRadius: scaleWidth(40.24),
    shadowOffset: { 
      width: scaleWidth(10.731), 
      height: scaleHeight(10.731) 
    },
    shadowColor: 'rgba(83, 26, 255, 1)',
    shadowOpacity: 0.1,
    elevation: 16,
    shadowColor: 'rgba(83, 26, 255, 0.1)',
    position: 'absolute',
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: White,
    zIndex: 2000,
  },
  dropdownIcons: { 
    width: scaleWidth(32.192), 
    height: scaleHeight(32.192),
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
    borderBottomWidth: scaleHeight(1.341), 
    borderBottomColor: 'rgba(0, 0, 0, 0.25)' 
  },
  unfriendOption: { 
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
  requestOptions: {
    flexDirection: 'row',
    gap: scaleWidth(10.73)
  },
  acceptBtn: {
    width: scaleWidth(95.192),
    height: scaleHeight(54.144),
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
      borderRadius: scaleWidth(132.792),
      width: 78.14,
      height: 78.14,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  xIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557)
  }
});
export default function Friends() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [friendsViewType, setSelectedFriendType] = useState('friends');
  const [search, setSearch] = useState('');
  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const ellipsisRefs = useRef([]);
  const friendsRef = useRef(null);
  const friendsDropdownRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    friendsDropdown: null,
    ellipsisBtn: null,
  });
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always', onScrollBeginDrag: closeAllMenus }),
    [closeAllMenus]
  );
  const friends = {
    friends: [
        {
            photo: Image1,
            name: "Jaydon HerWitzJaydon HerWitzJaydon HerWitz",
            username: "jaydon671jaydon671jaydon671jaydon671jaydon671",
        },
        {
            photo: Image2,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image3,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image4,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image5,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image6,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image7,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image8,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image9,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image10,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image11,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image12,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },   
        {
            photo: Image1,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image2,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image3,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image4,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image5,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image6,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image7,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image8,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image9,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image10,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image11,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image12,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },                          
    ],
    requests: [
        {
            photo: Image13,
            name: "Jaydon HerWitz Jaydon HerWitz",
            username: "jaydon671 Jaydon HerWitzJaydon HerWitz",
        },
        {
            photo: Image14,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image18,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image19,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image20,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image1,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image2,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image3,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image4,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },   
        {
            photo: Image5,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image6,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image7,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image8,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image9,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image10,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image11,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image12,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image13,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image14,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },                 
    ]
  };
  const closeAllMenus = useCallback(() => {
    setActiveDropdownIndex(null);
  }, []);
  const handleFriendsScroll = useCallback(() => {
    if (activeDropdownIndex !== null) setActiveDropdownIndex(null);
  }, [activeDropdownIndex]);
  const handleEllipsisPress = useCallback((index) => {
    swallowNextCloseRef.current = true;
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
  }, []);
  const handleSearchFocusOrTouch = useCallback(() => {
    closeAllMenus();
  }, [closeAllMenus]);
  const handleRootTouchEndCapture = useCallback((e) => {
    if (swallowNextCloseRef.current) {
      swallowNextCloseRef.current = false;
      return;
    }
    if (activeDropdownIndex === null) return;
    const { pageX: x, pageY: y } = e.nativeEvent;
    const { friendsDropdown, ellipsisBtn } = rectsRef.current;
    if (
      pointInRect(x, y, friendsDropdown) ||
      pointInRect(x, y, ellipsisBtn)
    ) {
      return;
    }
    closeAllMenus();
  }, [activeDropdownIndex, closeAllMenus, pointInRect]);
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
  const handleViewProfilePressIn = useCallback((index) => {
    swallowNextCloseRef.current = true;
  }, []);
  const handleMessagePressIn = useCallback((index) => {
    swallowNextCloseRef.current = true;
  }, []);
  const handleUnfriendPressIn = useCallback((index) => {
    swallowNextCloseRef.current = true;
  }, []);
  const friendsListContent = useMemo(() => ({
    ...styles.friendsListContent,
    paddingBottom: bottomSafeHeight + styles.friendsListContent.paddingBottom,
  }), [styles.friendsListContent, bottomSafeHeight]);
  useEffect(() => {
    ellipsisRefs.current = Array(friends.friends.length)
      .fill(null)
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [friends.friends.length]);
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
  const renderFriend = useCallback(({ item, index }) => {
    const isActive = activeDropdownIndex === index;
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          <View style={styles.friendImage}>
            <Image
              source={item.photo}
              style={styles.friendPhoto}
              accessible={true}
              accessibilityLabel="Friend photo"
            />
          </View>
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
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
              onPressIn={() => handleViewProfilePressIn(index)}
              onPressOut={closeAllMenus}
              style={[styles.friendDropdownOptions,styles.friendDropdownOptionsBorderBottom]}
            >
              <CustomText style={styles.dropdownBlackTxt}>View Profile</CustomText>
              <ProfileIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
            <TouchableOpacity
            onPressIn={() => handleMessagePressIn(index)}
            onPressOut={closeAllMenus}
            style={[styles.friendDropdownOptions, styles.friendDropdownOptionsBorderBottom]}
            >
            <CustomText style={styles.dropdownBlackTxt}>Message</CustomText>
            <ChatIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
            <TouchableOpacity
            onPressIn={() => handleUnfriendPressIn(index)}
            onPressOut={closeAllMenus}
            style={styles.unfriendOption}
            >
                <CustomText style={styles.dropdownRedTxt}>Unfriend</CustomText>
                <UnfriendIcon {...styles.dropdownIcons} />
            </TouchableOpacity>
          </Pressable>
        )}
      </View>
    );
  }, [activeDropdownIndex, styles, closeAllMenus]);
  const renderRequest = useCallback(({ item, index }) => {
    const isActive = activeDropdownIndex === index;
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          <View style={styles.friendImage}>
            <Image
              source={item.photo}
              style={styles.friendPhoto}
              accessible={true}
              accessibilityLabel="Request photo"
            />
          </View>
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View style={styles.requestOptions}>
            <TouchableOpacity style={styles.acceptBtn}>
                <CustomText style={styles.acceptTxt}>Accept</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.xBtn}>
                <XIcon {...styles.xIcon}/>
            </TouchableOpacity>
        </View>
      </View>
    );
  }, [activeDropdownIndex, styles, closeAllMenus]);
  const rootStyle = {
  ...styles.root,
  paddingTop: statusBarHeight + styles.root.paddingTop
  };
  return (
    <View style={rootStyle} onTouchEndCapture={handleRootTouchEndCapture}>
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
                    <View>
                        <CustomText style={styles.friendsTxt}>Friends ({friends.friends.length})</CustomText>
                    </View>
                </View>
                <View style={styles.addFriend}>
                    <TouchableOpacity 
                        style={styles.addFriendBtn}
                        onPress={() => navigation.navigate('AddFriends')}
                    >
                        <PlusIcon {...styles.plusIcon}/>
                        <CustomText style={styles.addFriendTxt}>Add Friend</CustomText>
                    </TouchableOpacity>
                </View>
            </View>
            <View style={styles.friendsViewType}>
              <TouchableOpacity
                style={friendsViewType === 'friends' ? styles.friendsViewTypeSelectedBtn : styles.friendsViewTypeNotSelectedBtn}
                onPress={() => {
                    setSelectedFriendType('friends');
                    setSearch('');
                }}
              >
                  <CustomText style={friendsViewType === 'friends' ? styles.friendsViewTypeSelectedtxt : styles.friendsViewTypeNotSelectedTxt}>Friends ({friends.friends.length})</CustomText>
              </TouchableOpacity>
              <TouchableOpacity
                style={friendsViewType === 'requests' ? styles.friendsViewTypeSelectedBtn : styles.friendsViewTypeNotSelectedBtn}
                onPress={() => {
                    setSelectedFriendType('requests');
                    setSearch('');
                }}
              >
                  <CustomText style={friendsViewType === 'requests' ? styles.friendsViewTypeSelectedtxt : styles.friendsViewTypeNotSelectedTxt}>Requests ({friends.requests.length})</CustomText>
              </TouchableOpacity>
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
              data={friendsViewType === 'friends' ? friends.friends : friends.requests}
              contentContainerStyle={friendsListContent}
              showsVerticalScrollIndicator={false}
              keyExtractor={(item, index) => `${friendsViewType}-${index}`}
              onScroll={handleFriendsScroll}
              scrollEventThrottle={16}
              removeClippedSubviews={false}
              extraData={activeDropdownIndex}
              renderItem={friendsViewType === 'friends' ? renderFriend : renderRequest}
              CellRendererComponent={ActiveListCell}
              {...listCommonProps}
            />
          </ActiveIndexContext.Provider>
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