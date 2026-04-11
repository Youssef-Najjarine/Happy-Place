import { useSafeAreaInsets } from 'react-native-safe-area-context';

export function useSafeAreaPadding() {
  const insets = useSafeAreaInsets();
  const statusBarHeight = insets.top;   // Dynamic top padding (status bar/notch)
  const bottomSafeHeight = insets.bottom;  // Dynamic bottom padding (home indicator/nav bar)

  return { statusBarHeight, bottomSafeHeight };
}