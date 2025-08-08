import React from 'react';
import { TextInput, StyleSheet } from 'react-native';

const weightMap = {
  100: 'Thin',
  200: 'ExtraLight',
  300: 'Light',
  400: 'Regular',
  500: 'Medium',
  600: 'SemiBold',
  700: 'Bold',
  800: 'ExtraBold',
  900: 'Black',
};

export default function CustomTextInput({ style = {}, ...rest }) {
  const flat = StyleSheet.flatten(style) || {};

  const fontWeight = flat.fontWeight || 400;
  const fontStyle = flat.fontStyle || 'normal';
  const baseName = weightMap[fontWeight] || 'Regular';
  const italicSuffix = fontStyle === 'italic' ? 'Italic' : '';
  const fontFamily = `Urbanist-${baseName}${italicSuffix}`;

  const mergedStyle = {
    ...flat,
    fontFamily,
    fontWeight: undefined,
    fontStyle: undefined,
  };

  return <TextInput style={mergedStyle} {...rest} />;
}
