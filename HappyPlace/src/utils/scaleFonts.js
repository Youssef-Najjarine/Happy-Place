import { Dimensions } from 'react-native';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import { 
  design_phone_width, 
  design_phone_height, 
  design_tablet_width, 
  design_tablet_height 
} from 'src/constants/designDimensions';

const scaleFont = (size) => {
  const { width } = Dimensions.get('window');
  const isTablet = width >= tabletBreakpoint;
  const designWidth = isTablet ? design_tablet_width : design_phone_width;
  const scale = width / designWidth;
  return Number((size * scale).toFixed(2));
};

const scaleHeight = (size) => {
  const { height, width } = Dimensions.get('window');
  const isTablet = width >= tabletBreakpoint;
  const designHeight = isTablet ? design_tablet_height : design_phone_height;
  const scale = height / designHeight;
  return Number((size * scale).toFixed(2));
};

const scaleLetterSpacing = (spacing) => {
  const { width } = Dimensions.get('window');
  const isTablet = width >= tabletBreakpoint;
  const designWidth = isTablet ? design_tablet_width : design_phone_width;
  const scale = width / designWidth;
  return Number((spacing * scale).toFixed(2));
};

export { scaleFont, scaleHeight, scaleLetterSpacing };