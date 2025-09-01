import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, Pressable, ScrollView } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import RemoveIcon from 'assets/images/global/leave-and-remove-chat-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import InviteIcon from 'assets/images/members/invite-white-icon.svg';
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
    paddingBottom: scaleHeight(16),
    marginBottom: scaleHeight(20)
  },
  membersHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  backArrowAndMembersRow: {
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
  membersTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  invite: {
    width: scaleWidth(93),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99)
  },
  inviteBtn: {
    borderRadius: scaleWidth(99),
    gap: scaleWidth(6),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  inviteIcon: {
    width: scaleWidth(20),
    height: scaleHeight(20)
  },
  inviteTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  },
  membersBody: {
    flex: 1
  },
  sectionHeader: {
    marginBottom: scaleHeight(16)
  },
  sectionHeaderTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  pendingMembersListContent: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(20)
  },
  currentMembersListContent: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(40)
  },
  memberCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  memberImageAndName: {
    gap: scaleWidth(8),
    flexDirection: 'row',
    alignItems: 'center'
  },
  memberImage: {
    width: scaleWidth(42),
    height: scaleHeight(42),
    borderRadius: scaleWidth(50)
  },
  memberPhoto: {
    borderRadius: scaleWidth(50),
    width: '100%',
    height: '100%',
    resizeMode: 'contain'
  },
  memberFullName: {
    width: scaleWidth(153),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  memberUsername: {
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
  memberDropdown: {
    top: scaleHeight(21),
    right: scaleWidth(20),
    width: scaleWidth(180),
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
  dropdownIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  memberDropdownOption: {
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(10.5),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownRedTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    color: HappyColor
  },
  pendingOptions: {
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
    paddingBottom: scaleHeight(21.46),
    marginBottom: scaleHeight(26.83)
  },
  membersHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  backArrowAndMembersRow: {
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
  membersTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  invite: {
    width: scaleWidth(121.432),
    height: scaleHeight(56.336),
    borderRadius: scaleWidth(132.792)
  },
  inviteBtn: {
    borderRadius: scaleWidth(132.792),
    gap: scaleWidth(8.05),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  inviteIcon: {
    width: scaleWidth(26.83),
    height: scaleHeight(26.83)
  },
  inviteTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },
  membersBody: {
    flex: 1
  },
  sectionHeader: {
    marginBottom: scaleHeight(21.46)
  },
  sectionHeaderTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  pendingMembersListContent: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(26.83)
  },
  currentMembersListContent: {
    gap: scaleHeight(16.1),
    paddingBottom: scaleHeight(40)
  },
  memberCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  memberImageAndName: {
    gap: scaleWidth(10.73),
    flexDirection: 'row',
    alignItems: 'center'
  },
  memberImage: {
      borderRadius: scaleWidth(67.067),
    width: 78.14,
    height: 78.14
  },
  memberPhoto: {
    borderRadius: scaleWidth(67.067),
    width: '100%',
    height: '100%',
    resizeMode: 'contain'
  },
  memberFullName: {
    width: scaleWidth(450.291),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  memberUsername: {
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
  memberDropdown: {
    top: scaleHeight(28.17),
    right: scaleWidth(26.83),
    width: scaleWidth(241.44),
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
  dropdownIcon: {
    width: scaleWidth(32.192),
    height: scaleHeight(32.192),
    resizeMode: 'contain'
  },
  memberDropdownOption: {
      paddingVertical: scaleHeight(14.08),
    paddingLeft: scaleWidth(20),
    paddingRight: scaleWidth(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownRedTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: HappyColor
  },
  pendingOptions: {
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
export default function Members() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const ellipsisRefs = useRef([]);
  const membersRef = useRef(null);
  const memberDropdownRef = useRef(null);
  const swallowNextCloseRef = useRef(false);
  const rectsRef = useRef({
    membersDropdown: null,
    ellipsisBtn: null,
  });
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always' }),
    []
  );
  const members = {
    members: [
        {
            photo: Image1,
            name: "Jaydon HerWitzJaydon HerWitzJaydon HerWitz HerWitzJaydon HerWitz",
            username: "jaydon671jaydon671jaydon671jaydon671jaydon671 HerWitzJaydon HerWitz",
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
    pending: [
        {
            photo: Image13,
            name: "Jaydon HerWitz Jaydon HerWitz HerWitzJaydon HerWitz",
            username: "jaydon671 Jaydon HerWitzJaydon HerWitz HerWitzJaydon HerWitz",
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
        }
    ]
  };
  const closeAllMenus = useCallback(() => {
    setActiveDropdownIndex(null);
  }, []);
  const handleEllipsisPress = useCallback((index) => {
    swallowNextCloseRef.current = true;
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
  }, []);
  const handleRootTouchEndCapture = useCallback((e) => {
    if (swallowNextCloseRef.current) {
      swallowNextCloseRef.current = false;
      return;
    }
    if (activeDropdownIndex === null) return;
    const { pageX: x, pageY: y } = e.nativeEvent;
    const { membersDropdown, ellipsisBtn } = rectsRef.current;
    if (
      pointInRect(x, y, membersDropdown) ||
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
  const handleRemovePressIn = useCallback((index) => {
    swallowNextCloseRef.current = true;
  }, []);
  const scrollableMembersListContent = useMemo(() => ({
    paddingBottom: bottomSafeHeight
  }), [bottomSafeHeight]);
 
  useEffect(() => {
    ellipsisRefs.current = Array(members.members.length)
      .fill(null)
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [members.members.length]);
  useEffect(() => {
    if (activeDropdownIndex !== null) {
      const ellipsisRef = ellipsisRefs.current[activeDropdownIndex];
      requestAnimationFrame(() => {
        if (ellipsisRef?.current) {
          ellipsisRef.current.measureInWindow((x, y, width, height) => {
            rectsRef.current.ellipsisBtn = { x, y, width, height };
          });
        }
        if (memberDropdownRef.current) {
          measureToRect(memberDropdownRef, 'membersDropdown');
        }
      });
    } else {
      rectsRef.current.ellipsisBtn = null;
      rectsRef.current.membersDropdown = null;
    }
  }, [activeDropdownIndex, measureToRect]);
  const renderMember = useCallback(({ item, index }) => {
    const isActive = activeDropdownIndex === index;
    return (
      <View style={styles.memberCard}>
        <View style={styles.memberImageAndName}>
          <View style={styles.memberImage}>
            <Image
              source={item.photo}
              style={styles.memberPhoto}
              accessible={true}
              accessibilityLabel="Member photo"
            />
          </View>
          <View>
            <CustomText style={styles.memberFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
            <CustomText style={styles.memberUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
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
            ref={memberDropdownRef}
            onLayout={() => measureToRect(memberDropdownRef, 'membersDropdown')}
            style={styles.memberDropdown}
          >
            <TouchableOpacity
                onPressIn={() => handleRemovePressIn(index)}
                onPressOut={closeAllMenus}
                style={styles.memberDropdownOption}
            >
                <CustomText style={styles.dropdownRedTxt}>Remove</CustomText>
                <RemoveIcon {...styles.dropdownIcon} />
            </TouchableOpacity>
          </Pressable>
        )}
      </View>
    );
  }, [activeDropdownIndex, styles, closeAllMenus]);
  const renderPending = useCallback(({ item, index }) => {
    return (
      <View style={styles.memberCard}>
        <View style={styles.memberImageAndName}>
          <View style={styles.memberImage}>
            <Image
              source={item.photo}
              style={styles.memberPhoto}
              accessible={true}
              accessibilityLabel="Pending photo"
            />
          </View>
          <View>
            <CustomText style={styles.memberFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
            <CustomText style={styles.memberUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View style={styles.pendingOptions}>
            <TouchableOpacity style={styles.acceptBtn}>
                <CustomText style={styles.acceptTxt}>Accept</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.xBtn}>
                <XIcon {...styles.xIcon}/>
            </TouchableOpacity>
        </View>
      </View>
    );
  }, [styles]);
  const rootStyle = {
    ...styles.root,
    paddingTop: statusBarHeight + styles.root.paddingTop
  };
  return (
    <View style={rootStyle} onTouchEndCapture={handleRootTouchEndCapture}>
        <View style={styles.topNav}>
            <View style={styles.membersHeaderRow}>
                <View style={styles.backArrowAndMembersRow}>
                    <View>
                        <TouchableOpacity
                            style={styles.BackArrow}
                            onPress={() => navigation.goBack()}
                        >
                            <BackArrow {...styles.backArrowIcon}/>
                        </TouchableOpacity>
                    </View>
                    <View>
                        <CustomText style={styles.membersTxt}>Members</CustomText>
                    </View>
                </View>
                <View style={styles.invite}>
                    <TouchableOpacity
                        style={styles.inviteBtn}
                        onPress={() => navigation.navigate('AddFriends')}
                    >
                        <InviteIcon {...styles.inviteIcon}/>
                        <CustomText style={styles.inviteTxt}>Invite</CustomText>
                    </TouchableOpacity>
                </View>
            </View>
        </View>
        <ScrollView 
          style={styles.membersBody} 
          contentContainerStyle={scrollableMembersListContent}
          onScrollBeginDrag={closeAllMenus}
          showsVerticalScrollIndicator={false}
        >
            <View style={styles.sectionHeader}>
    <CustomText style={styles.sectionHeaderTxt}>Pending</CustomText>
            </View>
          <FlatList
            data={members.pending}
            contentContainerStyle={styles.pendingMembersListContent}
            showsVerticalScrollIndicator={false}
            keyExtractor={(item, index) => `pending-${index}`}
            removeClippedSubviews={false}
            renderItem={renderPending}
            scrollEnabled={false}
            {...listCommonProps}
          />
          <View style={styles.sectionHeader}>
            <CustomText style={styles.sectionHeaderTxt}>Current</CustomText>
          </View>
          <ActiveIndexContext.Provider value={activeDropdownIndex}>
            <FlatList
              ref={membersRef}
              data={members.members}
              contentContainerStyle={styles.currentMembersListContent}
              showsVerticalScrollIndicator={false}
              keyExtractor={(item, index) => `member-${index}`}
              removeClippedSubviews={false}
              extraData={activeDropdownIndex}
              renderItem={renderMember}
              CellRendererComponent={ActiveListCell}
              scrollEnabled={false}
              {...listCommonProps}
            />
          </ActiveIndexContext.Provider>
        </ScrollView>
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