import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList, Pressable, ScrollView } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute, useIsFocused } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { 
  HappyColor, 
  White, 
  Black, 
  VeryLightGray, 
  SoftGray, 
  VeryLightLavenderTint
} from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import Avatar from 'src/components/Avatar';
import tokenStorage from 'src/services/tokenStorage';
import {
  useListMembersQuery,
  useApproveMemberMutation,
  useRejectMemberMutation,
  useRemoveMemberMutation,
} from 'src/store/chatGroupsApi';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import RemoveIcon from 'assets/images/global/leave-and-remove-chat-icon.svg';
import XIcon from 'assets/images/global/black-x-icon.svg';
import InviteIcon from 'assets/images/members/invite-white-icon.svg';
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
    flex: 1,
    backgroundColor: White
  },
  topNav: {
    paddingBottom: scaleHeight(16)
  },
  membersHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  backArrowAndMembersRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(12)
  },
  BackArrow: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    justifyContent: 'center',
    alignItems: 'center'
  },
  backArrowIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  membersTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  invite: {
    width: scaleWidth(95),
    height: scaleHeight(40)
  },
  inviteBtn: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(99),
    flexDirection: 'row',
    gap: scaleWidth(6),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  inviteIcon: {
    width: scaleWidth(16),
    height: scaleHeight(16),
    resizeMode: 'contain'
  },
  inviteTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  membersBody: {
    flex: 1
  },
  sectionHeader: {
    paddingVertical: scaleHeight(8)
  },
  sectionHeaderTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black,
    opacity: 0.6
  },
  pendingMembersListContent: {
    gap: scaleHeight(12)
  },
  currentMembersListContent: {
    gap: scaleHeight(12)
  },
  memberCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  memberImageAndName: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(12),
    flexShrink: 1
  },
  memberImage: {
    width: scaleWidth(44),
    height: scaleHeight(44)
  },
  memberPhoto: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(99)
  },
  memberFullName: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  memberUsername: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: Black,
    opacity: 0.6
  },
  ellipsisBackground: {
    width: scaleWidth(40),
    height: scaleHeight(40),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  ellipsis: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    resizeMode: 'contain'
  },
  memberDropdown: {
    top: scaleHeight(44),
    right: 0,
    width: scaleWidth(160),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowRadius: scaleWidth(30),
    shadowOffset: {
        width: scaleWidth(8),
        height: scaleHeight(8)
    },
    shadowOpacity: 0.1,
    elevation: 16,
    shadowColor: VeryLightLavenderTint,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000,
  },
  dropdownIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  memberDropdownOption: {
      paddingVertical: scaleHeight(10.5),
    paddingLeft: scaleWidth(15),
    paddingRight: scaleWidth(12),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dropdownRedTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    color: HappyColor
  },
  pendingOptions: {
    flexDirection: 'row',
    gap: scaleWidth(8)
  },
  acceptBtn: {
    width: scaleWidth(71),
    height: scaleHeight(40.5),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  acceptTxt: {
    fontSize: scaleFont(15),
    lineHeight: scaleLineHeight(22.5),
    letterSpacing: scaleLetterSpacing(-0.15),
    fontWeight: 600,
    color: White
  },
  xBtn: {
    borderRadius: scaleWidth(99),
    width: 58.5,
    height: 58.5,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  xIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28)
  }
});
const tabletStyles = StyleSheet.create({
  root: {
    paddingTop: scaleHeight(16),
    paddingHorizontal: scaleWidth(24),
    flex: 1,
    backgroundColor: White
  },
  topNav: {
    paddingBottom: scaleHeight(20)
  },
  membersHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  backArrowAndMembersRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(16)
  },
  BackArrow: {
    width: scaleWidth(32),
    height: scaleHeight(32),
    justifyContent: 'center',
    alignItems: 'center'
  },
  backArrowIcon: {
    width: scaleWidth(32),
    height: scaleHeight(32),
    resizeMode: 'contain'
  },
  membersTxt: {
    fontSize: scaleFont(26),
    lineHeight: scaleLineHeight(39),
    letterSpacing: scaleLetterSpacing(-0.26),
    fontWeight: 600,
    color: Black
  },
  invite: {
    width: scaleWidth(127),
    height: scaleHeight(53)
  },
  inviteBtn: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(132.792),
    flexDirection: 'row',
    gap: scaleWidth(8),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  inviteIcon: {
    width: scaleWidth(21.5),
    height: scaleHeight(21.5),
    resizeMode: 'contain'
  },
  inviteTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: White
  },
  membersBody: {
    flex: 1
  },
  sectionHeader: {
    paddingVertical: scaleHeight(10.5)
  },
  sectionHeaderTxt: {
    fontSize: scaleFont(21.5),
    lineHeight: scaleLineHeight(32),
    letterSpacing: scaleLetterSpacing(-0.215),
    fontWeight: 600,
    color: Black,
    opacity: 0.6
  },
  pendingMembersListContent: {
    gap: scaleHeight(16)
  },
  currentMembersListContent: {
    gap: scaleHeight(16)
  },
  memberCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  memberImageAndName: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: scaleWidth(16),
    flexShrink: 1
  },
  memberImage: {
    width: scaleWidth(59),
    height: scaleHeight(59)
  },
  memberPhoto: {
    width: '100%',
    height: '100%',
    borderRadius: scaleWidth(132.792)
  },
  memberFullName: {
    fontSize: scaleFont(21.5),
    lineHeight: scaleLineHeight(32),
    letterSpacing: scaleLetterSpacing(-0.215),
    fontWeight: 600,
    color: Black
  },
  memberUsername: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    color: Black,
    opacity: 0.6
  },
  ellipsisBackground: {
    width: scaleWidth(53.5),
    height: scaleHeight(53.5),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  ellipsis: {
    width: scaleWidth(27),
    height: scaleHeight(27),
    resizeMode: 'contain'
  },
  memberDropdown: {
    top: scaleHeight(59),
    right: 0,
    width: scaleWidth(214.5),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowRadius: scaleWidth(40.24),
    shadowOffset: {
        width: scaleWidth(10.731),
        height: scaleHeight(10.731)
    },
    shadowOpacity: 0.1,
    elevation: 16,
    shadowColor: VeryLightLavenderTint,
    position: 'absolute',
    borderColor: SoftGray,
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
    backgroundColor: VeryLightGray
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
  const route = useRoute();
  const chatGroupId = route.params?.chatGroupId;
  const isOwner = !!route.params?.isOwner;
  const [authToken, setAuthToken] = useState(null);
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
  const isFocused = useIsFocused();
  const { data: membersData } = useListMembersQuery(
    { authToken, chatGroupId },
    { skip: !authToken || !chatGroupId, pollingInterval: isFocused ? 3000 : 0 }
  );
  const [approveMember] = useApproveMemberMutation();
  const [rejectMember] = useRejectMemberMutation();
  const [removeMember] = useRemoveMemberMutation();
  const members = {
    members: membersData?.members || [],
    pending: membersData?.pendingMembers || [],
  };
  const closeAllMenus = useCallback(() => {
    setActiveDropdownIndex(null);
  }, []);
  const handleEllipsisPress = useCallback((index) => {
    swallowNextCloseRef.current = true;
    setActiveDropdownIndex((curr) => (curr === index ? null : index));
  }, []);
  const pointInRect = useCallback((x, y, r) => {
    return !!r && x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height;
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
  const measureToRect = useCallback((ref, key) => {
    if (!ref?.current) {
      rectsRef.current[key] = null;
      return;
    }
    ref.current.measureInWindow((x, y, width, height) => {
      rectsRef.current[key] = { x, y, width, height };
    });
  }, []);
  const handleRemovePressIn = useCallback((memberUserAccountId) => {
    swallowNextCloseRef.current = true;
    if (authToken && chatGroupId) removeMember({ authToken, chatGroupId, memberUserAccountId });
  }, [authToken, chatGroupId, removeMember]);
  const handleAcceptPress = useCallback((memberUserAccountId) => {
    if (authToken && chatGroupId) approveMember({ authToken, chatGroupId, memberUserAccountId });
  }, [authToken, chatGroupId, approveMember]);
  const handleRejectPress = useCallback((memberUserAccountId) => {
    if (authToken && chatGroupId) rejectMember({ authToken, chatGroupId, memberUserAccountId });
  }, [authToken, chatGroupId, rejectMember]);
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
    const canRemove = isOwner && !item.isOwner;
    return (
      <View style={styles.memberCard}>
        <View style={styles.memberImageAndName}>
          <View style={styles.memberImage}>
            <Avatar
              uri={item.profilePhotoUrl}
              color={item.avatarColor}
              initial={(item.name || '?').charAt(0).toUpperCase()}
              style={styles.memberPhoto}
              initialStyle={{ color: White, fontWeight: '600', fontSize: scaleFont(16) }}
            />
          </View>
          <View>
            <CustomText style={styles.memberFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
            {item.username ? (
              <CustomText style={styles.memberUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
            ) : null}
          </View>
        </View>
        {canRemove && (
          <View>
            <TouchableOpacity
              ref={(ref) => (ellipsisRefs.current[index] = ref)}
              style={styles.ellipsisBackground}
              onPressIn={() => handleEllipsisPress(index)}
            >
              <EllipsisIcon {...styles.ellipsis} />
            </TouchableOpacity>
          </View>
        )}
        {canRemove && isActive && (
          <Pressable
            ref={memberDropdownRef}
            onLayout={() => measureToRect(memberDropdownRef, 'membersDropdown')}
            style={styles.memberDropdown}
          >
            <TouchableOpacity
                onPressIn={() => handleRemovePressIn(item.userAccountId)}
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
  }, [activeDropdownIndex, styles, closeAllMenus, isOwner, handleEllipsisPress, handleRemovePressIn, measureToRect]);
  const renderPending = useCallback(({ item, index }) => {
    return (
      <View style={styles.memberCard}>
        <View style={styles.memberImageAndName}>
          <View style={styles.memberImage}>
            <Avatar
              uri={item.profilePhotoUrl}
              color={item.avatarColor}
              initial={(item.name || '?').charAt(0).toUpperCase()}
              style={styles.memberPhoto}
              initialStyle={{ color: White, fontWeight: '600', fontSize: scaleFont(16) }}
            />
          </View>
          <View>
            <CustomText style={styles.memberFullName} numberOfLines={1} ellipsizeMode="tail">{item.name}</CustomText>
            {item.username ? (
              <CustomText style={styles.memberUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
            ) : null}
          </View>
        </View>
        <View style={styles.pendingOptions}>
            <TouchableOpacity style={styles.acceptBtn} onPress={() => handleAcceptPress(item.userAccountId)}>
                <CustomText style={styles.acceptTxt}>Accept</CustomText>
            </TouchableOpacity>
            <TouchableOpacity style={styles.xBtn} onPress={() => handleRejectPress(item.userAccountId)}>
                <XIcon {...styles.xIcon}/>
            </TouchableOpacity>
        </View>
      </View>
    );
  }, [styles, handleAcceptPress, handleRejectPress]);
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
                    <TouchableOpacity style={styles.inviteBtn}>
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
            {members.pending.length > 0 && (
              <>
                <View style={styles.sectionHeader}>
                  <CustomText style={styles.sectionHeaderTxt}>Pending</CustomText>
                </View>
                <FlatList
                  data={members.pending}
                  contentContainerStyle={styles.pendingMembersListContent}
                  showsVerticalScrollIndicator={false}
                  keyExtractor={(item) => `pending-${item.userAccountId}`}
                  removeClippedSubviews={false}
                  renderItem={renderPending}
                  scrollEnabled={false}
                  {...listCommonProps}
                />
              </>
            )}
            {members.members.length > 0 && (
              <>
                <View style={styles.sectionHeader}>
                  <CustomText style={styles.sectionHeaderTxt}>Current</CustomText>
                </View>
                <ActiveIndexContext.Provider value={activeDropdownIndex}>
                  <FlatList
                    ref={membersRef}
                    data={members.members}
                    contentContainerStyle={styles.currentMembersListContent}
                    showsVerticalScrollIndicator={false}
                    keyExtractor={(item) => `member-${item.userAccountId}`}
                    removeClippedSubviews={false}
                    extraData={activeDropdownIndex}
                    renderItem={renderMember}
                    CellRendererComponent={ActiveListCell}
                    scrollEnabled={false}
                    {...listCommonProps}
                  />
                </ActiveIndexContext.Provider>
              </>
            )}
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