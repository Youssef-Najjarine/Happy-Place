/**
 * Utilities for scaling typography properties based on Figma design dimensions.
 * @module scaleFonts
 */
import { Dimensions, PixelRatio } from 'react-native';
import { tabletBreakpoint } from 'src/constants/breakpoints';
import { design_phone_width, design_phone_height, design_tablet_width, design_tablet_height } from 'src/constants/designDimensions';

// Cache dimensions to avoid repeated calls
let dimensions = Dimensions.get('window');
Dimensions.addEventListener('change', ({ window }) => {
  dimensions = window;
});

// Scaling constraints
const MIN_SCALE = 0.8; // Prevent overly small fonts
const MAX_SCALE = 1.5; // Prevent overly large fonts
const SUPPORT_ACCESSIBILITY = true; // Toggle for PixelRatio.getFontScale()

/**
 * Get the appropriate design dimension based on device type.
 * @param {'width' | 'height'} dimension - Dimension to retrieve
 * @returns {number} Design dimension for current device
 */
const getDesignDimension = (dimension) => {
  const { width } = dimensions;
  const isTablet = width >= tabletBreakpoint;
  if (dimension === 'width') {
    return isTablet ? design_tablet_width : design_phone_width;
  }
  return isTablet ? design_tablet_height : design_phone_height;
};

/**
 * Scales a font size based on device width relative to Figma design.
 * @param {number} size - Font size in Figma pixels
 * @returns {number} Scaled font size rounded to 2 decimals
 */
const scaleFont = (size) => {
  if (typeof size !== 'number' || size < 0 || !Number.isFinite(size)) {
    console.warn(`scaleFont: Invalid size ${size}, returning 0`);
    return 0;
  }
  const { width } = dimensions;
  const designWidth = getDesignDimension('width');
  if (designWidth === 0) {
    console.warn('scaleFont: Design width is 0, returning size');
    return Number(size.toFixed(2));
  }
  let scale = width / designWidth;
  scale = SUPPORT_ACCESSIBILITY ? scale * PixelRatio.getFontScale() : scale;
  scale = Math.min(Math.max(scale, MIN_SCALE), MAX_SCALE);
  return Number((size * scale).toFixed(2));
};

/**
 * Scales a line height based on device height relative to Figma design.
 * @param {number} size - Line height in Figma pixels
 * @returns {number} Scaled line height rounded to 2 decimals
 */
const scaleLineHeight = (size) => {
  if (typeof size !== 'number' || size < 0 || !Number.isFinite(size)) {
    console.warn(`scaleLineHeight: Invalid size ${size}, returning 0`);
    return 0;
  }
  const { height } = dimensions;
  const designHeight = getDesignDimension('height');
  if (designHeight === 0) {
    console.warn('scaleLineHeight: Design height is 0, returning size');
    return Number(size.toFixed(2));
  }
  let scale = height / designHeight;
  scale = SUPPORT_ACCESSIBILITY ? scale * PixelRatio.getFontScale() : scale;
  scale = Math.min(Math.max(scale, MIN_SCALE), MAX_SCALE);
  return Number((size * scale).toFixed(2));
};

/**
 * Scales letter spacing based on device width relative to Figma design.
 * @param {number} spacing - Letter spacing in Figma pixels
 * @returns {number} Scaled letter spacing rounded to 2 decimals
 */
const scaleLetterSpacing = (spacing) => {
  if (typeof spacing !== 'number' || !Number.isFinite(spacing)) {
    console.warn(`scaleLetterSpacing: Invalid spacing ${spacing}, returning 0`);
    return 0;
  }
  const { width } = dimensions;
  const designWidth = getDesignDimension('width');
  if (designWidth === 0) {
    console.warn('scaleLetterSpacing: Design width is 0, returning spacing');
    return Number(spacing.toFixed(2));
  }
  let scale = width / designWidth;
  scale = SUPPORT_ACCESSIBILITY ? scale * PixelRatio.getFontScale() : scale;
  scale = Math.min(Math.max(scale, MIN_SCALE), MAX_SCALE);
  return Number((spacing * scale).toFixed(2));
};

export { scaleFont, scaleLineHeight, scaleLetterSpacing };