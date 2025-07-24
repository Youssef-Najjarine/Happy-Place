import React from 'react';
import { Text } from 'react-native';

const CustomText = (props) => {
  const { style = {}, ...rest } = props;
  const fontWeight = style.fontWeight || 400;
  const fontStyle = style.fontStyle || 'normal';

  // Map weight to font file name (based on your font files)
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

  const baseName = weightMap[fontWeight] || 'Regular'; // Fallback to 400
  const italicSuffix = fontStyle === 'italic' ? 'Italic' : '';
  const fontFamily = `Urbanist-${baseName}${italicSuffix}`;

  // Merge styles, removing fontWeight/fontStyle to avoid conflicts
  const mergedStyle = {
    ...style,
    fontFamily,
    fontWeight: undefined, // Let fontFamily handle weight
    fontStyle: undefined,  // Let fontFamily handle style
  };
  return <Text style={mergedStyle} {...rest} />;
};

export default CustomText;