import { DEFAULT_THEME, ThemeFactory, } from "ui";

const roundButtons = {
	btnBorderRadiusLarge: '8px',
	btnBorderRadiusMedium: '8px',
	btnBorderRadiusSmall: '8px',
};

const roundSwitcher = {
	switcherButtonBorderRadiusMedium: '2px',
	switcherButtonPaddingXMedium: '30px',
	switcherButtonLineHeightMedium: '22px',

	switcherLabelGapMedium: '24px',
};

const darkPopups = {
	textColorDefault: '#fff',
	bgDefault: '#333333cc',
};

const reviewReplyTextarea = {
	textareaBorderWidth: '0px',
	textareaBorderColorFocus: 'transparent',
	textareaWidth: '100px',
	textareaMinHeight: '20px',
};

export default ThemeFactory.create({
	...roundButtons,
	...roundSwitcher
}, DEFAULT_THEME);

export const textareaHidden = ThemeFactory.create({
	...reviewReplyTextarea,
}, DEFAULT_THEME);

//currently it only applies a dark background and white text in popup(tooltip), styles copied from dark theme
export const darkFlat = ThemeFactory.create({
	...darkPopups,
	...roundButtons,
}, DEFAULT_THEME);
