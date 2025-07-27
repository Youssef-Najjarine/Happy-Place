import { useWindowDimensions } from 'react-native';

const BREAKPOINT = 600;

export const useResponsiveStyles = (phoneStyles, tabletStyles) => {
  const { width } = useWindowDimensions();
  return width < BREAKPOINT ? phoneStyles : tabletStyles;
};