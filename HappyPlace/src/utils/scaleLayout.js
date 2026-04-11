import { Dimensions, PixelRatio } from 'react-native';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import {
  design_phone_width,
  design_phone_height,
  design_tablet_width,
  design_tablet_height,
} from 'src/constants/designDimensions';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');
const isTablet = screenWidth >= tabletBreakpoint;

const designWidth = isTablet ? design_tablet_width : design_phone_width;
const designHeight = isTablet ? design_tablet_height : design_phone_height;

const roundTo = (value, decimals = 2) =>
  Number(Number(value).toFixed(decimals));

const scaleWidth = (size) => roundTo((screenWidth / designWidth) * size);
const scaleHeight = (size) => roundTo((screenHeight / designHeight) * size);
const moderateScale = (size, factor = 0.5) =>
  roundTo(size + (scaleWidth(size) - size) * factor);

export { scaleWidth, scaleHeight, moderateScale };
