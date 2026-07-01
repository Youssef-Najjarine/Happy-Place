import React from 'react';
import { View } from 'react-native';
import { HappyColor } from 'src/constants/colors';

export default class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch() {
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;
      return <View style={{ flex: 1, backgroundColor: HappyColor }} />;
    }
    return this.props.children;
  }
}