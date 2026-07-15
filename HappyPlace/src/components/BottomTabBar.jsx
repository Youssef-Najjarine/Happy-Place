import React from 'react';
import { View, TouchableOpacity, StyleSheet, Platform } from 'react-native';
import { useSelector } from 'react-redux';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
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

  const handleTabPress = (route, isFocused) => {
    if (user.isAnonymous && (route.name === 'MyProfile' || route.name === 'MyFriends')) {
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