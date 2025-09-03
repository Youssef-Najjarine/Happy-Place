import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import BackArrow from 'assets/images/global/back-arrow-black-icon.svg';
import DownArrowIcon from 'assets/images/global/arrow-down-icon.svg';
import UpArrowIcon from 'assets/images/addFriends/arrow-up-icon.svg';
import SearchIcon from 'assets/images/global/search-icon.svg';
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

const stylesActive = StyleSheet.create({
  zLift: { zIndex: 1000, elevation: 1000, overflow: 'visible' },
});

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
  addFriendsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
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
  suggestionsAndRequests: {
    marginBottom: scaleHeight(16),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  suggestionsTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  sentRequests: {
    width: scaleWidth(138),
    height: scaleHeight(39),
    borderRadius: scaleWidth(99)
  },
  sentRequestsBtn: {
    borderRadius: scaleWidth(99),
    gap: scaleWidth(8),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  sentRequestsTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  sentRequestArrows: {
    width: scaleWidth(20),
    height: scaleHeight(20)
  },
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(12)
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
    width: scaleWidth(145),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(145),
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(18),
    letterSpacing: scaleLetterSpacing(-0.12),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  cancelRequestBtn: {
    width: scaleWidth(132),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  cancelRequestTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: HappyColor
  },
  addFriendBtn: {
    width: scaleWidth(101),
    height: scaleHeight(42),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  addFriendTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    color: White
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
  addFriendsTxt: {
    fontSize: scaleFont(22),
    lineHeight: scaleLineHeight(33),
    letterSpacing: scaleLetterSpacing(-0.22),
    fontWeight: 600,
    color: Black
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
  suggestionsAndRequests: {
    marginBottom: scaleHeight(21.46),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  suggestionsTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    opacity: 0.6,
    color: Black
  },
  sentRequests: {
    width: scaleWidth(224.435),
    height: scaleHeight(56.336),
    borderRadius: scaleWidth(132.792)
  },
  sentRequestsBtn: {
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
  sentRequestsTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  },  
  friendsBody: {
    flex: 1
  },
  friendsListContent: {
    gap: scaleHeight(16.1)
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
    width: scaleWidth(445.35733),
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: Black
  },
  friendUsername: {
    width: scaleWidth(445.35733),
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    fontWeight: 600,
    fontStyle: 'italic',
    opacity: 0.6,
    color: Black
  },
  cancelRequestBtn: {
    width: scaleWidth(167.192),
    height: scaleHeight(54.144),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9F9F9'
  },
  cancelRequestTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: HappyColor
  },
  addFriendBtn: {
    width: scaleWidth(128.192),
    height: scaleHeight(54.144),
    borderRadius: scaleWidth(132.792),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor
  },
  addFriendTxt: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    fontWeight: 600,
    color: White
  }
});
const SEED_SUGGESTIONS = [
        {
            photo: Image13,
            requestSent: true,
            name: "Jaydon HerWitz Jaydon HerWitz",
            username: "jaydon671 Jaydon HerWitzJaydon HerWitz",
        },
        {
            photo: Image14,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image18,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image19,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image20,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image1,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image2,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image3,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image4,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },   
        {
            photo: Image5,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image6,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image7,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image8,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image9,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image10,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image11,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image12,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image13,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image14,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },                 
];
export default function AddFriends() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const [search, setSearch] = useState('');
  const [sentRequestsTop, setSentRequestsTop] = useState(false);
  const [suggestions, setSuggestions] = useState(() =>
  SEED_SUGGESTIONS.map((s, i) => ({ ...s, id: `s-${i}`, _i: i }))
);
  const friendsRef = useRef(null);
  const listCommonProps = useMemo(
    () => ({ keyboardShouldPersistTaps: 'always'}),
  );
  const friends = {
    suggestions: [
        {
            photo: Image13,
            requestSent: true,
            name: "Jaydon HerWitz Jaydon HerWitz",
            username: "jaydon671 Jaydon HerWitzJaydon HerWitz",
        },
        {
            photo: Image14,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image18,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image19,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image20,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image1,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image2,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image3,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image4,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },   
        {
            photo: Image5,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image6,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image7,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image8,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image9,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image10,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image11,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image12,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image13,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image14,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image15,
            requestSent: false,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image16,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },
        {
            photo: Image17,
            requestSent: true,
            name: "Jaydon HerWitz",
            username: "jaydon671",
        },                 
    ]
  };
  const suggestionsSorted = useMemo(() => {
    const q = search.trim().toLowerCase();
    const filtered = q
      ? suggestions.filter(
          s =>
            s.name.toLowerCase().includes(q) ||
            s.username.toLowerCase().includes(q)
        )
      : suggestions;

    return [...filtered].sort((a, b) => {
      if (a.requestSent !== b.requestSent) {
        // sent requests at bottom by default; at top when toggled
        return sentRequestsTop
          ? (a.requestSent ? -1 : 1)
          : (a.requestSent ? 1 : -1);
      }
      return a._i - b._i; // stable tie-break
    });
  }, [suggestions, search, sentRequestsTop]);
  const friendsListContent = useMemo(() => ({
    ...styles.friendsListContent,
    paddingBottom: bottomSafeHeight
  }), [styles.friendsListContent, bottomSafeHeight]);
  const handleAddFriend = useCallback((id) => {
    setSuggestions(prev => prev.map(p => p.id === id ? { ...p, requestSent: true } : p));
  }, []);
  const handleCancelRequest = useCallback((id) => {
    setSuggestions(prev => prev.map(p => p.id === id ? { ...p, requestSent: false } : p));
  }, []);
  const renderFriend = useCallback(({ item }) => {
    return (
      <View style={styles.friendCard}>
        <View style={styles.friendImageAndName}>
          <View style={styles.friendImage}>
            <Image
              source={item.photo}
              style={styles.friendPhoto}
              accessible
              accessibilityLabel="Add Friends photo"
            />
          </View>
          <View>
            <CustomText style={styles.friendFullName} numberOfLines={1} ellipsizeMode="tail">
              {item.name}
            </CustomText>
            <CustomText style={styles.friendUsername} numberOfLines={1} ellipsizeMode="tail">
              @{item.username}
            </CustomText>
          </View>
        </View>

        <View>
          {item.requestSent ? (
            <TouchableOpacity
              style={styles.cancelRequestBtn}
              onPress={() => handleCancelRequest(item.id)}
            >
              <CustomText style={styles.cancelRequestTxt}>Cancel Request</CustomText>
            </TouchableOpacity>
          ) : (
            <TouchableOpacity
              style={styles.addFriendBtn}
              onPress={() => handleAddFriend(item.id)}
            >
              <CustomText style={styles.addFriendTxt}>Add Friend</CustomText>
            </TouchableOpacity>
          )}
        </View>
      </View>
    );
  }, [styles, handleAddFriend, handleCancelRequest]);
  const rootStyle = {
  ...styles.root,
  paddingTop: statusBarHeight + styles.root.paddingTop
  };
  return (
    <View style={rootStyle}>
        <View style={styles.topNav}>
          <View>
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
                <CustomText style={styles.addFriendsTxt}>Add Friends</CustomText>
              </View>
            </View>
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
            />
            <SearchIcon {...styles.searchIcon} />
          </View>
        </View>
        <View style={styles.suggestionsAndRequests}>
          <View>
            <CustomText style={styles.suggestionsTxt}>Suggestions</CustomText>
          </View>
          <View style={styles.sentRequests}>
            <TouchableOpacity
              style={styles.sentRequestsBtn}
              onPress={() => setSentRequestsTop(v => !v)}
            >
              <CustomText style={styles.sentRequestsTxt}>Sent Requests</CustomText>
              {!sentRequestsTop ? (
                <DownArrowIcon {...styles.sentRequestArrows} />
              ) : (
                <UpArrowIcon {...styles.sentRequestArrows} />
              )}
            </TouchableOpacity>
          </View>
        </View>
        <View style={styles.friendsBody}>
            <FlatList
            ref={friendsRef}
            data={suggestionsSorted}
            contentContainerStyle={friendsListContent}
            showsVerticalScrollIndicator={false}
            keyExtractor={(item) => item.id} 
            renderItem={renderFriend}
            {...listCommonProps}
            />
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