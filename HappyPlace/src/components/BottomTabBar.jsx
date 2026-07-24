import React, { useState, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Platform } from 'react-native';
import { useSelector } from 'react-redux';
import tokenStorage from 'src/services/tokenStorage';
import { useUnreadTotalQuery } from 'src/store/chatGroupsApi';
import { selectRealtimeConnected } from 'src/store/realtimeSlice';
import { unreadBadgePollingInterval } from 'src/utils/pollingPolicy';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { shouldRedirectToFinishAccount } from 'src/utils/guestGate';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import Avatar from 'src/components/Avatar';
import { HappyColor, White, Black, Graphite, LightGray } from 'src/constants/colors';
import HelpTabIcon from 'assets/images/tabBar/help-tab-icon.svg';
import ChatsTabIcon from 'assets/images/tabBar/chats-tab-icon.svg';
import FriendsTabIcon from 'assets/images/tabBar/friends-tab-icon.svg';
import ProfileTabIcon from 'assets/images/tabBar/profile-tab-icon.svg';

const tabIcons = {
  Help: HelpTabIcon,
  ChatGroups: ChatsTabIcon,
  MyFriends: FriendsTabIcon,
  MyProfile: ProfileTabIcon
};

const phoneStyles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    backgroundColor: White,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: LightGray,
    ...Platform.select({
      ios: {
        shadowColor: Black,
        shadowOffset: { width: 0, height: -2 },
        shadowOpacity: 0.04,
        shadowRadius: 8
      },
      android: {
        elevation: 8
      }
    })
  },
  tabItem: {
    flex: 1,
    gap: scaleHeight(4),
    paddingTop: scaleHeight(8),
    paddingBottom: scaleHeight(8),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabIcon: {
    width: scaleWidth(24),
    height: scaleHeight(24)
  },
  chatsIconWrap: {
    width: scaleWidth(44),
    height: scaleHeight(28),
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabBadge: {
    top: 0,
    right: 0,
    minWidth: scaleWidth(16),
    height: scaleWidth(16),
    borderRadius: scaleWidth(99),
    paddingHorizontal: scaleWidth(4),
    position: 'absolute',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HappyColor
  },
  tabBadgeTxt: {
    fontSize: scaleFont(9),
    fontWeight: 700,
    color: White
  },
  iconSlot: {
    height: scaleHeight(28),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabAvatarRing: {
    width: scaleWidth(28),
    height: scaleHeight(28),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(1.5),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabAvatar: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    borderRadius: scaleWidth(99)
  },
  tabAvatarInitial: {
    fontSize: scaleFont(12),
    fontWeight: 700,
    color: White
  },
  tabLabel: {
    fontSize: scaleFont(11),
    lineHeight: scaleLineHeight(13),
    letterSpacing: scaleLetterSpacing(-0.11),
    fontWeight: 600
  }
});

const tabletStyles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    backgroundColor: White,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: LightGray,
    ...Platform.select({
      ios: {
        shadowColor: Black,
        shadowOffset: { width: 0, height: -2 },
        shadowOpacity: 0.04,
        shadowRadius: 8
      },
      android: {
        elevation: 8
      }
    })
  },
  tabItem: {
    flex: 1,
    gap: scaleHeight(5),
    paddingTop: scaleHeight(10),
    paddingBottom: scaleHeight(10),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabIcon: {
    width: scaleWidth(28),
    height: scaleHeight(28)
  },
  chatsIconWrap: {
    width: scaleWidth(50),
    height: scaleHeight(32),
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabBadge: {
    top: 0,
    right: 0,
    minWidth: scaleWidth(18),
    height: scaleWidth(18),
    borderRadius: scaleWidth(99),
    paddingHorizontal: scaleWidth(5),
    position: 'absolute',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HappyColor
  },
  tabBadgeTxt: {
    fontSize: scaleFont(10),
    fontWeight: 700,
    color: White
  },
  iconSlot: {
    height: scaleHeight(32),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabAvatarRing: {
    width: scaleWidth(32),
    height: scaleHeight(32),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(1.5),
    alignItems: 'center',
    justifyContent: 'center'
  },
  tabAvatar: {
    width: scaleWidth(28),
    height: scaleHeight(28),
    borderRadius: scaleWidth(99)
  },
  tabAvatarInitial: {
    fontSize: scaleFont(14),
    fontWeight: 700,
    color: White
  },
  tabLabel: {
    fontSize: scaleFont(13),
    lineHeight: scaleLineHeight(16),
    letterSpacing: scaleLetterSpacing(-0.13),
    fontWeight: 600
  }
});

export default function BottomTabBar({ state, descriptors, navigation }) {
  const { bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const user = useSelector((storeState) => storeState.user);
  const [authToken, setAuthToken] = useState(tokenStorage.peekToken());
  useEffect(() => tokenStorage.subscribe((token) => setAuthToken(token)), []);
  const isRealtimeConnected = useSelector(selectRealtimeConnected);
  const { data: unreadTotalData } = useUnreadTotalQuery(authToken, { skip: !authToken, pollingInterval: unreadBadgePollingInterval(isRealtimeConnected) });
  const unreadTotal = unreadTotalData && unreadTotalData.status === 'ok' ? unreadTotalData.total : 0;

  const handleTabPress = (route, isFocused) => {
    if (shouldRedirectToFinishAccount(route.name, user)) {
      navigation.navigate('FinishAccount');
      return;
    }
    const pressEvent = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
    if (!isFocused && !pressEvent.defaultPrevented) {
      navigation.navigate(route.name, route.params);
    }
  };

  const handleTabLongPress = (route) => {
    navigation.emit({ type: 'tabLongPress', target: route.key });
  };

  return (
    <View style={[styles.bar, { paddingBottom: bottomSafeHeight }]}>
      {state.routes.map((route, routeIndex) => {
        const isFocused = state.index === routeIndex;
        const { options } = descriptors[route.key];
        const label = options.tabBarLabel || route.name;
        const TabIcon = tabIcons[route.name];
        const tintColor = isFocused ? HappyColor : Graphite;

        return (
          <TouchableOpacity
            key={route.key}
            style={styles.tabItem}
            onPress={() => handleTabPress(route, isFocused)}
            onLongPress={() => handleTabLongPress(route)}
            accessibilityRole="tab"
            accessibilityState={{ selected: isFocused }}
            accessibilityLabel={label}
          >
            <View style={styles.iconSlot}>
              {route.name === 'MyProfile' && user.isLoggedIn ? (
                <View style={[styles.tabAvatarRing, { borderColor: isFocused ? HappyColor : 'transparent' }]}>
                  <Avatar
                    uri={user.profilePhotoUrl}
                    color={user.avatarColor}
                    initial={user.displayName ? user.displayName[0].toUpperCase() : '?'}
                    style={styles.tabAvatar}
                    initialStyle={styles.tabAvatarInitial}
                  />
                </View>
              ) : route.name === 'ChatGroups' ? (
                <View style={styles.chatsIconWrap}>
                  {TabIcon && <TabIcon {...styles.tabIcon} color={tintColor} />}
                  {unreadTotal > 0 && (
                    <View style={styles.tabBadge}>
                      <CustomText style={styles.tabBadgeTxt}>{unreadTotal > 99 ? '99+' : unreadTotal}</CustomText>
                    </View>
                  )}
                </View>
              ) : (
                TabIcon && <TabIcon {...styles.tabIcon} color={tintColor} />
              )}
            </View>
            <CustomText style={[styles.tabLabel, { color: tintColor }]}>{label}</CustomText>
          </TouchableOpacity>
        );
      })}
    </View>
  );
}