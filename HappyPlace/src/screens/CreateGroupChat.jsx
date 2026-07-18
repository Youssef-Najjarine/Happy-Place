import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet, FlatList, ActivityIndicator } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black, WarmIvory, SoftGray, VeryLightGray } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import Avatar from 'src/components/Avatar';
import { showToast } from 'src/components/Toast';
import tokenStorage from 'src/services/tokenStorage';
import { useListFriendsPageQuery, useLazyListFriendsPageQuery } from 'src/store/friendsApi';
import { useCreateFriendsGroupMutation } from 'src/store/chatGroupsApi';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';

const MaxSelectableFriends = 20;

const phoneStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    height: '100%',
    width: '100%'
  },
  topNav: {
    gap: scaleHeight(12),
    paddingBottom: scaleHeight(16),
    paddingHorizontal: scaleWidth(20),
    borderBottomLeftRadius: scaleWidth(24),
    borderBottomRightRadius: scaleWidth(24),
    width: '100%',
    backgroundColor: White
  },
  headerRow: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center'
  },
  backArrowIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28),
    resizeMode: 'contain'
  },
  headerTitle: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: Black
  },
  nameInput: {
    borderRadius: scaleWidth(99),
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(10),
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    width: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  selectionCountTxt: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(19),
    letterSpacing: scaleLetterSpacing(-0.13),
    fontWeight: 500,
    opacity: 0.6,
    color: Black
  },
  friendsList: {
    flex: 1
  },
  friendsListContent: {
    paddingHorizontal: scaleWidth(20),
    paddingTop: scaleHeight(16),
    gap: scaleHeight(12)
  },
  friendCard: {
    borderRadius: scaleWidth(16),
    paddingVertical: scaleHeight(12),
    paddingHorizontal: scaleWidth(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '100%',
    backgroundColor: White
  },
  friendImageAndName: {
    gap: scaleWidth(12),
    flexDirection: 'row',
    alignItems: 'center',
    flexShrink: 1
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
  friendNameColumn: {
    gap: scaleHeight(2),
    flexShrink: 1
  },
  friendFullName: {
    fontSize: scaleFont(15),
    lineHeight: scaleLineHeight(22),
    letterSpacing: scaleLetterSpacing(-0.15),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(19),
    letterSpacing: scaleLetterSpacing(-0.13),
    fontWeight: 500,
    opacity: 0.6,
    color: Black
  },
  selectCircle: {
    width: scaleWidth(22),
    height: scaleWidth(22),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(2),
    borderColor: SoftGray,
    alignItems: 'center',
    justifyContent: 'center'
  },
  selectCircleSelected: {
    borderColor: HappyColor,
    backgroundColor: HappyColor
  },
  selectCircleInner: {
    width: scaleWidth(8),
    height: scaleWidth(8),
    borderRadius: scaleWidth(99),
    backgroundColor: White
  },
  emptyStateTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500,
    opacity: 0.6,
    paddingTop: scaleHeight(24),
    textAlign: 'center',
    color: Black
  },
  pageLoadingFooter: {
    paddingVertical: scaleHeight(16),
    alignItems: 'center'
  },
  createBar: {
    paddingHorizontal: scaleWidth(20),
    paddingTop: scaleHeight(12),
    width: '100%',
    backgroundColor: WarmIvory
  },
  createBtn: {
    borderRadius: scaleWidth(99),
    paddingVertical: scaleHeight(14),
    width: '100%',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  createBtnDisabled: {
    opacity: 0.4
  },
  createBtnTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
  }
});

const tabletStyles = StyleSheet.create({
  root: {
    backgroundColor: WarmIvory,
    height: '100%',
    width: '100%'
  },
  topNav: {
    gap: scaleHeight(16),
    paddingBottom: scaleHeight(20),
    paddingHorizontal: scaleWidth(24),
    borderBottomLeftRadius: scaleWidth(32),
    borderBottomRightRadius: scaleWidth(32),
    width: '100%',
    backgroundColor: White
  },
  headerRow: {
    gap: scaleWidth(16),
    flexDirection: 'row',
    alignItems: 'center'
  },
  backArrowIcon: {
    width: scaleWidth(37.557),
    height: scaleHeight(37.557),
    resizeMode: 'contain'
  },
  headerTitle: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
  },
  nameInput: {
    borderRadius: scaleWidth(99),
    paddingHorizontal: scaleWidth(20),
    paddingVertical: scaleHeight(12),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    width: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  selectionCountTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    opacity: 0.6,
    color: Black
  },
  friendsList: {
    flex: 1
  },
  friendsListContent: {
    paddingHorizontal: scaleWidth(24),
    paddingTop: scaleHeight(20),
    gap: scaleHeight(16)
  },
  friendCard: {
    borderRadius: scaleWidth(21.461),
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(20),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '100%',
    backgroundColor: White
  },
  friendImageAndName: {
    gap: scaleWidth(16),
    flexDirection: 'row',
    alignItems: 'center',
    flexShrink: 1
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
  friendNameColumn: {
    gap: scaleHeight(2.68),
    flexShrink: 1
  },
  friendFullName: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 500,
    opacity: 0.6,
    color: Black
  },
  selectCircle: {
    width: scaleWidth(26),
    height: scaleWidth(26),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(2.5),
    borderColor: SoftGray,
    alignItems: 'center',
    justifyContent: 'center'
  },
  selectCircleSelected: {
    borderColor: HappyColor,
    backgroundColor: HappyColor
  },
  selectCircleInner: {
    width: scaleWidth(10),
    height: scaleWidth(10),
    borderRadius: scaleWidth(99),
    backgroundColor: White
  },
  emptyStateTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    opacity: 0.6,
    paddingTop: scaleHeight(32),
    textAlign: 'center',
    color: Black
  },
  pageLoadingFooter: {
    paddingVertical: scaleHeight(20),
    alignItems: 'center'
  },
  createBar: {
    paddingHorizontal: scaleWidth(24),
    paddingTop: scaleHeight(16),
    width: '100%',
    backgroundColor: WarmIvory
  },
  createBtn: {
    borderRadius: scaleWidth(99),
    paddingVertical: scaleHeight(18),
    width: '100%',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  createBtnDisabled: {
    opacity: 0.4
  },
  createBtnTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  }
});

export default function CreateGroupChat() {
  const navigation = useNavigation();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const [authToken, setAuthToken] = useState(tokenStorage.peekToken());
  useEffect(() => tokenStorage.subscribe((token) => setAuthToken(token)), []);
  const [groupName, setGroupName] = useState('');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);
  const [selectedUsernames, setSelectedUsernames] = useState([]);
  const [isCreating, setIsCreating] = useState(false);
  const { data: friendsPage, isFetching: isFetchingFriends } = useListFriendsPageQuery({ authToken, search: debouncedSearch || null }, { skip: !authToken });
  const [fetchNextFriendsPage, { isFetching: isFetchingNextFriendsPage }] = useLazyListFriendsPageQuery();
  const [createFriendsGroup] = useCreateFriendsGroupMutation();
  const friends = friendsPage?.items || [];

  const toggleFriend = useCallback((username) => {
    setSelectedUsernames((current) => {
      if (current.includes(username)) return current.filter((entry) => entry !== username);
      if (current.length >= MaxSelectableFriends) {
        showToast(`You can add up to ${MaxSelectableFriends} friends`, 'info');
        return current;
      }
      return [...current, username];
    });
  }, []);

  const handleEndReached = useCallback(() => {
    const nextCursor = friendsPage?.nextCursor;
    if (!authToken || !nextCursor || isFetchingNextFriendsPage) return;
    fetchNextFriendsPage({ authToken, search: debouncedSearch || null, cursor: nextCursor });
  }, [authToken, debouncedSearch, friendsPage?.nextCursor, isFetchingNextFriendsPage, fetchNextFriendsPage]);

  const handleCreate = useCallback(async () => {
    const trimmedName = groupName.trim();
    if (!trimmedName || selectedUsernames.length === 0 || isCreating || !authToken) return;
    setIsCreating(true);
    try {
      const data = await createFriendsGroup({ authToken, name: trimmedName, usernames: selectedUsernames }).unwrap();
      if (data?.status === 'created') {
        navigation.replace('ChatGroup', { chatGroupId: data.chatGroupId });
        return;
      }
      if (data?.status === 'accountRequired') {
        showToast('Create an account to start group chats', 'error');
        navigation.navigate('FinishAccount');
        return;
      }
      if (data?.status === 'invalidName') {
        showToast('Please enter a group name', 'error');
        return;
      }
      if (data?.status === 'notFriends') {
        showToast('You can only add friends', 'error');
        return;
      }
      showToast('Something went wrong. Please try again.', 'error');
    } catch (error) {
      showToast('Something went wrong. Please try again.', 'error');
    } finally {
      setIsCreating(false);
    }
  }, [groupName, selectedUsernames, isCreating, authToken, createFriendsGroup, navigation]);

  const renderFriend = useCallback(({ item }) => {
    const isSelected = selectedUsernames.includes(item.username);
    return (
      <TouchableOpacity style={styles.friendCard} onPress={() => toggleFriend(item.username)}>
        <View style={styles.friendImageAndName}>
          <View style={styles.friendImage}>
            <Avatar
              uri={item.profilePhotoUrl}
              color={item.avatarColor}
              initial={(item.displayName || item.username || '?')[0].toUpperCase()}
              style={styles.friendPhoto}
              initialStyle={styles.avatarInitialTxt}
            />
          </View>
          <View style={styles.friendNameColumn}>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">{item.displayName}</CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">@{item.username}</CustomText>
          </View>
        </View>
        <View style={[styles.selectCircle, isSelected && styles.selectCircleSelected]}>
          {isSelected && <View style={styles.selectCircleInner} />}
        </View>
      </TouchableOpacity>
    );
  }, [styles, selectedUsernames, toggleFriend]);

  const renderEmpty = useCallback(() => {
    if (isFetchingFriends) return null;
    return (
      <CustomText style={styles.emptyStateTxt}>
        {debouncedSearch ? 'No friends match your search.' : 'Add some friends first to start a group chat.'}
      </CustomText>
    );
  }, [styles, isFetchingFriends, debouncedSearch]);

  const renderFooter = useCallback(() => {
    if (!isFetchingNextFriendsPage) return null;
    return (
      <View style={styles.pageLoadingFooter}>
        <ActivityIndicator color={HappyColor} />
      </View>
    );
  }, [isFetchingNextFriendsPage, styles]);

  const topNavStyle = useMemo(
    () => ({ ...styles.topNav, paddingTop: statusBarHeight }),
    [styles.topNav, statusBarHeight]
  );
  const canCreate = !!groupName.trim() && selectedUsernames.length > 0 && !isCreating;

  return (
    <View style={styles.root}>
      <View style={topNavStyle}>
        <View style={styles.headerRow}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <BackArrow {...styles.backArrowIcon} />
          </TouchableOpacity>
          <CustomText style={styles.headerTitle}>New Group Chat</CustomText>
        </View>
        <CustomTextInput
          style={styles.nameInput}
          placeholder="Group name"
          keyboardType="default"
          autoCapitalize="sentences"
          autoCorrect={false}
          value={groupName}
          onChangeText={setGroupName}
        />
        <CustomTextInput
          style={styles.nameInput}
          placeholder="Search friends"
          keyboardType="default"
          autoCapitalize="none"
          autoCorrect={false}
          returnKeyType="search"
          value={search}
          onChangeText={setSearch}
        />
        <CustomText style={styles.selectionCountTxt}>{selectedUsernames.length}/{MaxSelectableFriends} friends selected</CustomText>
      </View>
      <FlatList
        style={styles.friendsList}
        contentContainerStyle={styles.friendsListContent}
        data={friends}
        keyExtractor={(item) => item.username}
        renderItem={renderFriend}
        ListEmptyComponent={renderEmpty}
        ListFooterComponent={renderFooter}
        onEndReached={handleEndReached}
        onEndReachedThreshold={0.4}
        keyboardShouldPersistTaps="always"
        showsVerticalScrollIndicator={false}
      />
      <View style={[styles.createBar, { paddingBottom: bottomSafeHeight + scaleHeight(12) }]}>
        <TouchableOpacity
          style={[styles.createBtn, !canCreate && styles.createBtnDisabled]}
          disabled={!canCreate}
          onPress={handleCreate}
        >
          <CustomText style={styles.createBtnTxt}>{isCreating ? 'Creating...' : 'Create Group Chat'}</CustomText>
        </TouchableOpacity>
      </View>
    </View>
  );
}