import React from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { useResponsiveStyles } from 'src/utils/useResponsiveStyles';
import { HappyColor, White, Black, VeryLightGray, SoftGray, VeryLightLavenderTint, VividBlueViolet, TranslucentBlack } from 'src/constants/colors';
import { scaleFont, scaleLineHeight, scaleLetterSpacing } from 'src/utils/scaleFonts';
import { scaleWidth, scaleHeight } from 'src/utils/scaleLayout';
import CustomText from 'src/components/FontFamilyText';
import CustomTextInput from 'src/components/FontFamilyTextInput';
import SearchIcon from 'assets/images/global/search-icon.svg';
import SortIcon from 'assets/images/chatGroups/sort-icon.svg';
import DownArrowIcon from 'assets/images/global/arrow-down-icon.svg';

const phoneStyles = StyleSheet.create({
  topNavIcons: {
    width: scaleWidth(20),
    height: scaleHeight(20),
    resizeMode: 'contain'
  },
  searchAndSortRow: {
    paddingHorizontal: scaleWidth(20),
    height: scaleHeight(39),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  search: {
    width: scaleWidth(200),
    height: '100%'
  },
  searchIcon: {
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
    backgroundColor: VeryLightGray,
    color: Black
  },
  sort: {
    width: scaleWidth(127),
    height: '100%'
  },
  sortBtn: {
    borderRadius: scaleWidth(99),
    paddingVertical: scaleHeight(9),
    paddingHorizontal: scaleWidth(10),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  sortTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 600,
    color: Black
  },
  sortByDropdown: {
    top: scaleHeight(-32),
    right: scaleWidth(20),
    width: scaleWidth(127),
    borderRadius: scaleWidth(16),
    borderWidth: scaleWidth(1.341),
    shadowRadius: scaleWidth(15),
    shadowOffset: {
      width: scaleWidth(8),
      height: scaleHeight(8)
    },
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    shadowColor: VeryLightLavenderTint,
    shadowOpacity: 1,
    elevation: 12,
    zIndex: 2000
  },
  sortByOptions: {
    paddingHorizontal: scaleWidth(16),
    paddingVertical: scaleHeight(8),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  sortByDropdownTxt: {
    fontSize: scaleFont(14),
    lineHeight: scaleLineHeight(21),
    letterSpacing: scaleLetterSpacing(-0.14),
    fontWeight: 500
  },
  sortByNotSelectedTxt: {
    color: Black
  },
  sortBySelectedTxt: {
    color: HappyColor
  },
  sortByOptionsBorderBottom: {
    borderBottomWidth: scaleHeight(0.671),
    borderBottomColor: TranslucentBlack
  }
});
const tabletStyles = StyleSheet.create({
  topNavIcons: {
    width: scaleWidth(24),
    height: scaleHeight(24),
    resizeMode: 'contain'
  },
  searchAndSortRow: {
    paddingHorizontal: scaleWidth(24),
    height: scaleHeight(47),
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  search: {
    width: scaleWidth(532),
    height: '100%'
  },
  searchIcon: {
    top: scaleHeight(10),
    left: scaleWidth(12),
    position: 'absolute'
  },
  searchInput: {
    borderRadius: scaleWidth(132.792),
    paddingLeft: scaleWidth(44),
    paddingVertical: scaleHeight(10),
    paddingRight: scaleWidth(12),
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500,
    width: '100%',
    height: '100%',
    backgroundColor: VeryLightGray,
    color: Black
  },
  sort: {
    width: scaleWidth(152),
    height: '100%'
  },
  sortBtn: {
    borderRadius: scaleWidth(132.792),
    paddingVertical: scaleHeight(11.5),
    paddingHorizontal: scaleWidth(12),
    width: '100%',
    height: '100%',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: VeryLightGray
  },
  sortTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 600,
    color: Black
  },
  sortByDropdown: {
    top: scaleHeight(-42.57),
    right: scaleWidth(24),
    width: scaleWidth(152),
    borderRadius: scaleWidth(21.461),
    borderWidth: scaleWidth(1.341),
    shadowColor: VividBlueViolet,
    shadowOpacity: 0.10,
    shadowOffset: {
      width: scaleWidth(10.731),
      height: scaleHeight(10.731),
    },
    shadowRadius: scaleWidth(40.24),
    elevation: 16,
    position: 'absolute',
    borderColor: SoftGray,
    backgroundColor: White,
    zIndex: 2000
  },
  sortByOptions: {
    paddingHorizontal: scaleWidth(21.461),
    paddingVertical: scaleHeight(8),
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  sortByDropdownTxt: {
    fontSize: scaleFont(18),
    lineHeight: scaleLineHeight(27),
    letterSpacing: scaleLetterSpacing(-0.18),
    fontWeight: 500
  },
  sortByNotSelectedTxt: {
    color: Black
  },
  sortBySelectedTxt: {
    color: HappyColor
  },
  sortByOptionsBorderBottom: {
    borderBottomWidth: scaleHeight(0.671),
    borderBottomColor: TranslucentBlack
  }
});

export function SearchAndSortChatGroupsBar({ search, onChangeSearch, onSearchFocus, sortBy, onSortPress, sortBtnRef }) {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  return (
    <View style={styles.searchAndSortRow}>
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
          onChangeText={onChangeSearch}
          onFocus={onSearchFocus}
          onTouchStart={onSearchFocus}
        />
        <SearchIcon {...StyleSheet.flatten([styles.topNavIcons, styles.searchIcon])} />
      </View>
      <View style={styles.sort}>
        <TouchableOpacity
          style={styles.sortBtn}
          ref={sortBtnRef}
          onPressIn={onSortPress}
        >
          <SortIcon {...styles.topNavIcons} />
          <CustomText style={styles.sortTxt} numberOfLines={1}>{sortBy}</CustomText>
          <DownArrowIcon {...styles.topNavIcons} />
        </TouchableOpacity>
      </View>
    </View>
  );
}

export function SearchAndSortChatGroupsDropdown({ sortOptions, sortBy, onSelectOption, onClose, dropdownRef, onLayout }) {
  const styles = useResponsiveStyles(phoneStyles, tabletStyles);
  return (
    <View
      ref={dropdownRef}
      onLayout={onLayout}
      style={styles.sortByDropdown}
    >
      {sortOptions.map((opt, idx) => (
        <TouchableOpacity
          key={opt}
          onPressIn={() => onSelectOption(opt)}
          onPressOut={onClose}
          style={[styles.sortByOptions, idx < sortOptions.length - 1 && styles.sortByOptionsBorderBottom]}
        >
          <CustomText
            style={[
              styles.sortByDropdownTxt,
              sortBy === opt ? styles.sortBySelectedTxt : styles.sortByNotSelectedTxt,
            ]}
          >
            {opt}
          </CustomText>
        </TouchableOpacity>
      ))}
    </View>
  );
}