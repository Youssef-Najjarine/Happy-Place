import { useWindowDimensions } from 'react-native';
import { tabletBreakpoint } from 'src/constants/breakpoints';

export const useResponsiveStyles = (phoneStyles, tabletStyles) => {
  const { width } = useWindowDimensions();
  return width < tabletBreakpoint ? phoneStyles : tabletStyles;
};