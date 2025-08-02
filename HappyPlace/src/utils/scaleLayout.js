import { Dimensions } from 'react-native';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import { 
  design_phone_width, 
  design_phone_height, 
  design_tablet_width, 
  design_tablet_height 
} from 'src/constants/designDimensions';
const scaleWidthPercent = (pixelWidth, parentPixelWidth = null) => {
  const { width } = Dimensions.get('window');
  const isTablet = width >= tabletBreakpoint;
  const designWidth = isTablet ? design_tablet_width : design_phone_width;
  const referenceWidth = parentPixelWidth || designWidth;
  const percent = (pixelWidth / referenceWidth) * 100;
  return `${percent.toFixed(2)}%`;
};

const scaleHeightPercent = (pixelHeight, parentPixelHeight = null) => {
  const { height, width } = Dimensions.get('window');
  const isTablet = width >= tabletBreakpoint;
  const designHeight = isTablet ? design_tablet_height : design_phone_height;
  const referenceHeight = parentPixelHeight || designHeight;
  const percent = (pixelHeight / referenceHeight) * 100;
  return `${percent.toFixed(2)}%`;
};

export { scaleWidthPercent, scaleHeightPercent };