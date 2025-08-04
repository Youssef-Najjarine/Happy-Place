import React, { useState, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Image, FlatList, useWindowDimensions } from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useSafeAreaPadding } from 'src/hooks/useSafeAreaPadding';
import { HappyColor, White, Black } from 'src/constants/colors';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight, moderateScale } from 'src/utils/scaleLayout';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import CustomText from 'src/components/FontFamilyText';
import SadEmoji from 'assets/images/global/sad-emoji.svg';
import HappyEmoji from 'assets/images/global/happy-emoji.svg';
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
    paddingRight: scaleWidth(20)
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
    width: '100%'
  },
  chatGroupsListContent: {
    width: '100%',
    gap: scaleHeight(12)
  },
  chatGroupCard: {
    height: scaleHeight(163),
    maxHeight: scaleHeight(163),
    borderRadius: scaleWidth(16),
    paddingTop: scaleHeight(16),
    paddingRight: scaleWidth(16),
    baddingBottom: scaleHeight(16),
    paddingLeft: scaleWidth(16),
    justifyContent: 'space-between',
    width: '100%',
    backgroundColor: White
  },
  chatGroupHelpersWrapper: {
    position: 'relative',
    height: scaleWidth(36),
    marginBottom: scaleHeight(10),
    flexDirection: 'row'
  },
  chatGroupHelpersStack: {
    height: scaleHeight(36),
    // width: scaleWidth(36 + 4 * ( 24)),
    position: 'relative',
},
  chatGroupHelperImage: {
    position: 'absolute',
    top: 0,
    left: scaleWidth(20),
    left: scaleWidth(0),
    width: scaleWidth(36),
    height: scaleHeight(36),
    borderWidth: scaleWidth(2),
    borderRadius: scaleWidth(50),
    borderColor: White,
    backgroundColor: White
  },
  extraHelpersCircle: {
    width: scaleWidth(36),
    height: scaleWidth(36),
    borderRadius: scaleWidth(18),
    backgroundColor: Black,
    justifyContent: 'center',
    alignItems: 'center',
    marginLeft: -scaleWidth(10),
    zIndex: 0,
  },
  extraHelpersText: {
    color: White,
    fontSize: scaleFont(14),
    fontWeight: '600',
  },
});

const tabletStyles = StyleSheet.create({});

export default function ChatGroups() {
  const { statusBarHeight, bottomSafeHeight } = useSafeAreaPadding();
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  const navigation = useNavigation();
  const route = useRoute();
  const [showSearching, setShowSearching] = useState(false);
  const [dotCount, setDotCount] = useState(0);
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
      title: "I just got cheated on."
    },
  ]
  const renderHelper = ({ item }) => (
    <View style={styles.helperCard}>
      <Image source={item.image} style={styles.helperImage} />
      <CustomText style={styles.helperName}>{item.name}</CustomText>
    </View>
  );
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
  const handleCancel = () => {
    setShowSearching(false);
  };
  const handleHelpMe = () => {
    setShowSearching(true);
  };
  const handleICanHelp = () => {
    setShowSearching(true);
  };
  const rootStyle = {
    ...styles.root,
    paddingBottom: bottomSafeHeight
  };
  const topNav = {
    ...styles.topNav,
    paddingTop: statusBarHeight
  }
const { width } = useWindowDimensions();
  const isTablet = width >= tabletBreakpoint;
  const getHelperImageStyles = (count = 5) => {
  const imageSize = scaleWidth(36);
  const overlapSpacing = isTablet ? 32.19 : 24;

  const spacing = scaleWidth(overlapSpacing);

  return Array.from({ length: count }).map((_, i) => ({
    ...styles.chatGroupHelperImage,
    left: i * spacing,
    zIndex: i + 1,
  }));
};


  return (
    <View style={rootStyle}>
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
        {helpers.length > 0 ?
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
        : 
          ""
        }
<View style={styles.ChatGroups}>
  <FlatList
    data={chatGroups}
    contentContainerStyle={styles.chatGroupsListContent}
    showsVerticalScrollIndicator={false}
    keyExtractor={(item, index) => index.toString()}
    renderItem={({ item }) => {
      const maxVisible = 5;
      const helperImageStyles = getHelperImageStyles(maxVisible);

      return (
        <View style={styles.chatGroupCard}>
          <View style={styles.chatGroupHelpersWrapper}>
            <View style={styles.chatGroupHelpersStack}>
              {item.helpers.slice(0, maxVisible).map((helper, index) => (
                <Image
                  key={index}
                  source={helper.image}
                  style={helperImageStyles[index]}
                />
              ))}
              {item.helpers.length > maxVisible && (
                <View
                  style={[
                    styles.extraHelpersCircle,
                    {
                      left: helperImageStyles[maxVisible - 1].left + scaleWidth(isTablet ? 32.19 : 24),
                      zIndex: maxVisible + 1,
                    }
                  ]}
                >
                  <CustomText style={styles.extraHelpersText}>
                    +{item.helpers.length - maxVisible}
                  </CustomText>
                </View>
              )}
            </View>
          </View>
          <CustomText style={styles.chatGroupTitle}>{item.title}</CustomText>
        </View>
      );
    }}
  />
</View>

      </View>
    </View>
  );
}