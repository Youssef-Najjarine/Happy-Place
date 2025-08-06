import React, { useState, useEffect, useRef } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions, Pressable } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
import EllipsisIcon from 'assets/images/global/three-dots-icon.svg';
import EditIcon from 'assets/images/global/edit-icon.svg';
import MembersIcon from 'assets/images/global/members-icon.svg';
import PendingMembersCircle from 'assets/images/global/pending-members-circle.svg';
import TrashIcon from 'assets/images/global/trash-outline-icon.svg';
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
    width: '100%'
  },
  topNav: {
    width: '100%',
    height: scaleHeight(158),
    paddingBottom: scaleHeight(16),
    borderBottomLeftRadius: scaleWidth(24),
    borderBottomRightRadius: scaleWidth(24),
    backgroundColor: White,
    justifyContent: 'space-between',
    marginBottom: scaleHeight(20)
  },
  login: {
    width: '100%',
    height: scaleHeight(44),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: scaleWidth(20),
    paddingLeft: scaleWidth(20),
    backgroundColor: '#F9F9F9'
  },
  unlockAllFeatures: {
    color: Black,
    fontSize: scaleFont(14),
    fontWeight: 600,
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14)
  },
  loginView: {
    width: scaleWidth(62),
    height: scaleHeight(32),
  },
  loginBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HappyColor,
    borderBlockColor: HappyColor,
    borderRadius: scaleWidth(99)
  },
  loginBtnTxt: {
    color: White,
    fontSize: scaleFont(16),
    fontWeight: 600,
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  searchingView: {
    height: scaleHeight(42),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: scaleWidth(20),
    paddingLeft: scaleWidth(20)
  },
  searching: {
    width: scaleWidth(87),
    height: scaleHeight(29),
    borderRadius: scaleWidth(99),
    backgroundColor: 'rgba(237, 83, 112, 0.20)',
    justifyContent: 'center',
    alignItems: 'center'
  },
  searchingTxt: {
    color: Black,
    fontSize: scaleFont(14),
    fontWeight: 600,
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    minWidth: scaleWidth(71)
  },
  cancelView: {
    width: scaleWidth(81),
    height: scaleHeight(42),
    backgroundColor: '#F9F9F9',
    borderRadius: scaleWidth(99)

  },
  cancelBtn: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
  },
  cancelTxt: {
    color: Black,
    fontWeight: 600,
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  helpView: {
    height: scaleHeight(41),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingRight: scaleWidth(20),
    paddingLeft: scaleWidth(20)
  },
  faceEmojis: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    resizeMode: 'contain'
  },
  helpMeBtn: {
    width: scaleWidth(160),
    height: scaleHeight(41),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidth(6),
    borderWidth: scaleWidth(1.5),
    borderColor: Black,
    borderRadius: scaleWidth(99),
    backgroundColor: White
  },
  helpMeTxt: {
    color: Black,
    fontWeight: 600,
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14)
  },
  iCanHelpBtn: {
    width: scaleWidth(167),
    height: scaleHeight(41),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: scaleWidth(6),
    borderWidth: 0,
    borderRadius: scaleWidth(99),
    backgroundColor: HappyColor
  },
  iCanHelpMeTxt: {
    color: White,
    fontWeight: 600,
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14)
  },
  mainContent: {
    paddingLeft: scaleWidth(20),
    paddingRight: scaleWidth(20),
    flex: 1
  },
  helpers: {
    width: '100%',
    height: scaleHeight(115),
    marginBottom: scaleHeight(16)
  },
  availableHelpersTxt: {
    color: Black,
    fontWeight: 600,
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16)
  },
  helpersListContent: {
    paddingTop: scaleHeight(12),
    gap: scaleWidth(16)
  },
  helperCard: {
    alignItems: 'center',
    width: scaleWidth(50),
    gap: scaleHeight(8)

  },
  helperImage: {
    width: scaleWidth(50),
    height: scaleHeight(50),
    borderRadius: scaleWidth(50),
    resizeMode: 'cover'
  },
  helperName: {
    color: Black,
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    height: scaleHeight(21)
  },
  ChatGroups: {
    width: '100%',
    flex: 1
  },
  chatGroupsListContent: {
    width: '100%',
    gap: scaleHeight(12)
  },
  chatGroupCard: {
    height: scaleHeight(163),
    maxHeight: scaleHeight(163),
    borderRadius: scaleWidth(16),
    paddingVertical: scaleHeight(16),
    paddingHorizontal: scaleWidth(16),
    justifyContent: 'space-between',
    width: '100%',
    backgroundColor: White
  },
  chatGroupCardJoinedBorder: {
    borderWidth: scaleWidth(1),
    borderColor: HappyColor
  },
  chatGroupHelperImage: {
    width: scaleWidth(36),
    height: scaleHeight(36),
    borderWidth: scaleWidth(2),
    borderRadius: scaleWidth(50),
    position: 'absolute',
    top: 0,
    borderColor: White,
    backgroundColor: White
  },
  extraHelpersCircle: {
    width: scaleWidth(36),
    height: scaleWidth(36),
    borderRadius: scaleWidth(50),
    borderWidth: scaleWidth(2),
    borderColor: White,
    backgroundColor: Black,
    justifyContent: 'center',
    alignItems: 'center',
    marginLeft: -scaleWidth(2),
    zIndex: 0,
  },
  extraHelpersText: {
    fontSize: scaleFont(12),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: White,
    fontWeight: 600
  },
  chatPhotosHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    width: '100%'
  },
  joinedEllipsisView: {
    width: scaleWidth(108),
    gap: scaleWidth(8),
    flexDirection: 'row',
    justifyContent: 'flex-end',
    alignItems: 'center',
  },
  joined: {
    width: scaleWidth(64),
    height: scaleHeight(29),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(237, 83, 112, 0.20)'
  },
  joinedLabel: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: '#232323'
  },
  ellipsisBackground: {
    width: scaleWidth(36),
    height: scaleHeight(36),
    borderRadius: scaleWidth(99),
    backgroundColor: '#F9F9F9',
    justifyContent: 'center',
    alignItems: 'center'
  },
  ellipsis: {
    width: scaleWidth(28),
    height: scaleHeight(28),
  },
  chatGroupDropdown: {
    position: 'absolute',
    top: scaleHeight(37),
    right: scaleWidth(30),
    width: scaleWidth(180),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1),
    shadowRadius: scaleWidth(15),
    shadowOffset: {
      width: scaleWidth(8),
      height: scaleHeight(8),
    },
    position: 'absolute',
    borderColor: 'rgba(238, 238, 238, 0.40)',
    backgroundColor: White,
    shadowColor: 'rgba(83, 26, 255, 0.1)',
    shadowOpacity: 1,
    elevation: 12,
    zIndex: 999
  },
  dropdownIcons: {
    width: scaleWidth(24),
    height: scaleHeight(24),
  },
  editMemberOptions: {
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(10),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  editMemberOptionsBorderBottom: {
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
    alignItems: 'center',

  },
  dropdownBlackTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: Black,
    fontWeight: 500,
  },
  dropdownRedTxt: {
    fontSize: scaleFont(16),
    lineHeight: scaleLineHeight(24),
    letterSpacing: scaleLetterSpacing(-0.16),
    color: HappyColor,
    fontWeight: 500
  },
  chatGroupTitleView: {
    width: '100%'
  },
  chatGroupTitle: {
    fontSize: scaleFont(20),
    lineHeight: scaleLineHeight(30),
    letterSpacing: scaleLetterSpacing(-0.2),
    color: Black,
    fontWeight: 600
  },
  chatGroupBtnView: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  groupChatLeaveChatBtn: {
    width: scaleWidth(128),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: 'rgba(237, 83, 112, 0.10)'
  },
  groupChatLeaveChatTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    color: HappyColor,
    fontWeight: 600
  },
  groupChatHappyBtn: {
    width: scaleWidth(167),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: 'none',
    backgroundColor: HappyColor
  },
  groupChatHappyTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: White
  },
  groupChatSpectateBtn: {
    width: scaleWidth(128),
    height: scaleHeight(41),
    borderRadius: scaleWidth(99),
    borderWidth: scaleWidth(1.5),
    justifyContent: 'center',
    alignItems: 'center',
    borderColor: HappyColor,
    backgroundColor: White
  },
  groupChatSpectateTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  }
});

const tabletStyles = StyleSheet.create({});

export default function ChatGroups() {
  const [activeDropdownIndex, setActiveDropdownIndex] = useState(null);
  const [showSearching, setShowSearching] = useState(false);
  const [dotCount, setDotCount] = useState(0);
  const ellipsisRefs = useRef([]);
  const chatGroupsRef = useRef(null);
  const route = useRoute();
  const navigation = useNavigation();
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const helpers = [
    {
      image: Image1,
      name: 'Jaydon'
    },
    {
      image: Image2,
      name: 'Julia'
    },
    {
      image: Image3,
      name: 'Mike'
    }, 
    {
      image: Image4,
      name: 'Mia'
    },
    {
      image: Image5,
      name: 'Demi'
    },
    {
      image: Image6,
      name: 'Brian'
    },
    {
      image: Image7,
      name: 'Jeffry Michael'
    }
  ]
  const chatGroups = [
    {
      helpers: [
        {
          image: Image7
        },
        {
          image: Image8
        },
        {
          image: Image9
        }, 
        {
          image: Image10
        },
        {
          image: Image11
        },
        {
          image: Image12
        },
        {
          image: Image13
        }
      ],
      joined: true,
      owner: true,
      pendingMembers: true,
      title: "Happy in Paddleboarding 🔥"
    },
    {
      helpers: [
        {
          image: Image12
        },
        {
          image: Image13
        },
        {
          image: Image14
        }, 
        {
          image: Image15
        }
      ],
      joined: false,
      owner: false,
      pendingMembers: false,
      title: "I’m depressed!"
    },
    {
      helpers: [
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        }
      ],
      joined: false,
      owner: false,
      pendingMembers: false,
      title: "I failed my Final Exams."
    },
    {
      helpers: [
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        },
        {
          image: Image16
        },
        {
          image: Image17
        },
        {
          image: Image18
        }, 
        {
          image: Image19
        },
        {
          image: Image20
        },
        {
          image: Image1
        },
        {
          image: Image2
        },
        {
          image: Image2
        },
        {
          image: Image3
        },
        {
          image: Image4
        },
        {
          image: Image5
        },
        {
          image: Image6
        }
      ],
      joined: false,
      owner: false,
      pendingMembers: false,
      title: "I just got cheated on."
    }
  ]
  const handleScroll = () => {
    if (activeDropdownIndex !== null) {
      setActiveDropdownIndex(null);
    }
  };
  const handleEllipsisPress = (index) => {
    if (activeDropdownIndex === index) {
      setActiveDropdownIndex(null);
    } else {
      const buttonRef = ellipsisRefs.current[index];
      if (buttonRef && buttonRef.current) {
        setActiveDropdownIndex(index);
      } else {
        console.log("Ref not ready for index:", index);
      }
    }
  };
  const renderHelper = ({ item }) => (
    <View style={styles.helperCard}>
      <Image source={item.image} style={styles.helperImage} />
      <CustomText style={styles.helperName}>{item.name}</CustomText>
    </View>
  );
  const handleCancel = () => {
    setShowSearching(false);
  };
  const handleHelpMe = () => {
    setShowSearching(true);
  };
  const handleICanHelp = () => {
    setShowSearching(true);
  };
  useEffect(() => {
    ellipsisRefs.current = Array(chatGroups.length)
      .fill()
      .map((_, i) => ellipsisRefs.current[i] ?? React.createRef());
  }, [chatGroups]);
  useEffect(() => {
    if (route.params?.startSearching) {
      setShowSearching(true);
    }
  }, [route.params]);
  useEffect(() => {
    if (!showSearching) return;

    const interval = setInterval(() => {
      setDotCount((prev) => (prev + 1) % 4);
    }, 500);

    return () => clearInterval(interval);
  }, [showSearching]);
  const topNav = {
    ...styles.topNav,
    paddingTop: statusBarHeight
  }
  const { width } = useWindowDimensions();
  const isTablet = width >= tabletBreakpoint;
  const getHelperImageStyles = (count = 5) => {
    const overlapSpacing = isTablet ? 32.19 : 24;

    const spacing = scaleWidth(overlapSpacing);

    return Array.from({ length: count }).map((_, i) => ({
      ...styles.chatGroupHelperImage,
      left: i * spacing,
      zIndex: i + 1,
    }));
  };

  return (
    <View style={styles.root}>
      <View style={topNav}>
        <View style={styles.login}>
          <View>
            <CustomText style={styles.unlockAllFeatures}>
              Unlock all features!
            </CustomText>
          </View>
          <View style={styles.loginView}>
            <TouchableOpacity style={styles.loginBtn} onPress={() => navigation.navigate('LoginOptions')}>
              <CustomText style={styles.loginBtnTxt}>Login</CustomText>
            </TouchableOpacity>
          </View>
        </View>
        {showSearching ?
            <View style={styles.searchingView}>
              <View style={styles.searching}>
                <CustomText style={styles.searchingTxt}>
                  {`Searching${'.'.repeat(dotCount)}`}
                </CustomText>
              </View>
              <View style={styles.cancelView}>
                <TouchableOpacity style={styles.cancelBtn} onPress={handleCancel}>
                  <CustomText style={styles.cancelTxt}>Cancel</CustomText>
                </TouchableOpacity>
              </View>
            </View>
        :
            <View style={styles.helpView}>
              <TouchableOpacity style={styles.helpMeBtn} onPress={handleHelpMe}>
                <SadEmoji {...styles.faceEmojis}/>
                <CustomText style={styles.helpMeTxt}>HELP ME</CustomText>
              </TouchableOpacity>
              <TouchableOpacity style={styles.iCanHelpBtn} onPress={handleICanHelp}>
                <HappyEmoji {...styles.faceEmojis}/>
                <CustomText style={styles.iCanHelpMeTxt}>I CAN HELP</CustomText>
              </TouchableOpacity>
            </View>
        }
      </View>
      <View style={styles.mainContent}>
        {helpers.length > 0 && (
          <View style={styles.helpers}>
            <CustomText style={styles.availableHelpersTxt}>Available Helpers</CustomText>
            <FlatList
              data={helpers}
              showsHorizontalScrollIndicator={false}
              contentContainerStyle={styles.helpersListContent}
              keyExtractor={(item, index) => index.toString()}
              renderItem={renderHelper}
              horizontal
            />
          </View>
        )}
        <View style={styles.ChatGroups}>
          <FlatList
            ref={chatGroupsRef}
            data={chatGroups}
            contentContainerStyle={styles.chatGroupsListContent}
            showsVerticalScrollIndicator={false}
            keyExtractor={(item, index) => index.toString()}
            onScroll={handleScroll}
            scrollEventThrottle={16}
            renderItem={({ item, index }) => {
              const maxVisible = 5;
              const helperImageStyles = getHelperImageStyles(maxVisible);
              return (
                <View style={[styles.chatGroupCard, item.joined ? styles.chatGroupCardJoinedBorder : null]} >
                  <View style={styles.chatPhotosHeader}>
                    <View style={styles.chatGroupHelpersWrapper}>
                      <View style={styles.chatGroupHelpersStack}>
                        {item.helpers.slice(0, maxVisible).map((helper, i) => (
                          <Image key={i} source={helper.image} style={helperImageStyles[i]} />
                        ))}
                        {item.helpers.length > maxVisible && (
                          <View
                            style={[
                              styles.extraHelpersCircle,
                              {
                                left: helperImageStyles[maxVisible - 1].left + scaleWidth(isTablet ? 32.19 : 24),
                                zIndex: maxVisible + 1,
                              },
                            ]}
                          >
                            <CustomText style={styles.extraHelpersText}>
                              +{item.helpers.length - maxVisible}
                            </CustomText>
                          </View>
                        )}
                      </View>
                    </View>
                    <View style={styles.joinedEllipsisView}>
                      {item.joined && (
                        <View style={styles.joined}>
                          <CustomText style={styles.joinedLabel}>Joined</CustomText>
                        </View>
                      )}
                      <TouchableOpacity
                        ref={ellipsisRefs.current[index]}
                        style={styles.ellipsisBackground}
                        onPress={() => handleEllipsisPress(index)}
                      >
                        <EllipsisIcon {...styles.ellipsis} />
                      </TouchableOpacity>
                    </View>
                  </View>
                  <View style={styles.chatGroupTitleView}>
                    <CustomText style={styles.chatGroupTitle} numberOfLines={1} ellipsizeMode="tail">{item.title}</CustomText>
                  </View>
                  {item.joined  ?
                    <View style={styles.chatGroupBtnView}>
                      <TouchableOpacity style={styles.groupChatLeaveChatBtn}>
                        <CustomText style={styles.groupChatLeaveChatTxt}>Leave Chat</CustomText>
                      </TouchableOpacity>
                    <TouchableOpacity style={styles.groupChatHappyBtn}>
                        <CustomText style={styles.groupChatHappyTxt}>View Chat</CustomText>
                      </TouchableOpacity>                      
                    </View>                 
                  :
                    <View style={styles.chatGroupBtnView}>
                      <TouchableOpacity style={styles.groupChatSpectateBtn}>
                        <CustomText style={styles.groupChatSpectateTxt}>Spectate</CustomText>
                      </TouchableOpacity>
                      <TouchableOpacity style={styles.groupChatHappyBtn}>
                        <CustomText style={styles.groupChatHappyTxt}>Request Join</CustomText>
                      </TouchableOpacity>
                    </View>  
                  }
                  {activeDropdownIndex === index && (
                    <Pressable
                      onPress={(e) => e.stopPropagation()} 
                      style={styles.chatGroupDropdown}
                    >
                      {item.owner && (
                        <TouchableOpacity
                          onPress={() => { setActiveDropdownIndex(null); }}
                          style={[styles.editMemberOptions, styles.editMemberOptionsBorderBottom]}
                        >
                          <CustomText style={styles.dropdownBlackTxt}>Edit name</CustomText>
                          <EditIcon {...styles.dropdownIcons} />
                        </TouchableOpacity>
                      )}
                      <TouchableOpacity
                        onPress={() => { setActiveDropdownIndex(null); }}
                        style={[styles.editMemberOptions, item.owner ? styles.editMemberOptionsBorderBottom : null]}
                      >
                        <CustomText style={styles.dropdownBlackTxt}>Members</CustomText>
                        <MembersIcon {...styles.dropdownIcons} />
                        {(item.pendingMembers && item.owner) && (
                          <PendingMembersCircle {...styles.pendingMembersCircle}/>
                        )}
                      </TouchableOpacity>
                      {item.owner && (
                        <TouchableOpacity
                          onPress={() => { setActiveDropdownIndex(null); }}
                          style={styles.deleteOption}
                        >
                          <CustomText style={styles.dropdownRedTxt}>Delete group</CustomText>
                          <TrashIcon {...styles.dropdownIcons} />
                        </TouchableOpacity>
                      )}
                    </Pressable>
                  )}
                </View>
              );
            }}
          />
        </View>
      </View>
      <LinearGradient
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