import React from 'react';
import { Image } from 'react-native';
import baseService from 'services/baseService';

export default function RemoteImage({ uri, ...props }) {
    if (!uri) return null;
    return <Image source={{ uri: baseService.getMediaUrl(uri) }} {...props} />;
}