import React from 'react';
import { View } from 'react-native';
import RemoteImage from 'src/components/RemoteImage';
import CustomText from 'src/components/FontFamilyText';

export default function Avatar({ uri, color, initial, style, initialStyle }) {
  if (uri) {
    return <RemoteImage uri={uri} style={style} fadeDuration={0} />;
  }
  return (
    <View style={[style, { backgroundColor: color || '#B7B7C9', alignItems: 'center', justifyContent: 'center' }]}>
      <CustomText style={initialStyle}>{initial || '?'}</CustomText>
    </View>
  );
}