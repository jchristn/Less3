import { theme, ThemeConfig } from 'antd';

export const Less3Theme = {
  primary: '#22AF79', //95DB7B
  primaryDark: '#2dc48a', //95DB7B
  primaryRed: '#d9383a',
  secondaryBlue: '#b1e5ff',
  secondaryYellow: '#ffe362',
  borderGray: '#C1C1C1',
  borderSecondary: '#D1D1D1',
  white: '#ffffff',
  fontFamily: '"Inter", "serif"',
  colorBgContainerDisabled: '#E9E9E9',
  textDisabled: '#bbbbbb',
  subHeadingColor: '#666666',
};

export const primaryTheme: ThemeConfig = {
  cssVar: true,
  algorithm: theme.defaultAlgorithm,
  token: {
    colorPrimary: Less3Theme.primary,
    fontFamily: Less3Theme.fontFamily,
    colorBorder: Less3Theme.borderGray,
    colorTextDisabled: Less3Theme.textDisabled,
    colorBgContainerDisabled: Less3Theme.colorBgContainerDisabled,
    colorBorderSecondary: Less3Theme.borderSecondary,
  },
  components: {
    Message: {
      fontSize: 30,
    },
    Tabs: {
      cardBg: '#F2F2F2',
      titleFontSize: 12,
    },
    Typography: {
      fontWeightStrong: 400,
    },
    Layout: {
      fontFamily: Less3Theme.fontFamily,
    },
    Menu: {},
    Card: {
      colorBorder: Less3Theme.borderGray,
    },
    Button: {
      borderRadius: 5,
      borderRadiusLG: 5,
      primaryColor: Less3Theme.white,
      defaultColor: '#333333',
      colorLink: Less3Theme.primary,
      colorLinkHover: Less3Theme.primary,
    },
    Table: {
      headerBg: '#ffffff',
      padding: 18,
      borderColor: '#d1d5db',
    },
    Collapse: {
      headerBg: Less3Theme.white,
    },
    Input: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
    },
    Select: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
      optionSelectedColor: Less3Theme.white,
      optionSelectedBg: Less3Theme.primary,
    },
    Pagination: {
      fontFamily: Less3Theme.fontFamily,
    },
    Form: {
      labelColor: Less3Theme.subHeadingColor,
      colorBorder: 'none',
      verticalLabelPadding: 0,
    },
  },
};

export const darkTheme: ThemeConfig = {
  cssVar: true,
  algorithm: theme.darkAlgorithm,
  token: {
    colorBgBase: '#151515',
    colorPrimary: Less3Theme.primaryDark,
    fontFamily: Less3Theme.fontFamily,
    colorBorder: '#555',
    colorTextDisabled: Less3Theme.textDisabled,
    colorBgContainerDisabled: Less3Theme.colorBgContainerDisabled,
    colorBorderSecondary: '#444444',
  },
  components: {
    Message: {
      fontSize: 30,
    },
    Tabs: {
      cardBg: '#F2F2F2',
      titleFontSize: 12,
    },
    Typography: {
      fontWeightStrong: 400,
    },
    Layout: {
      fontFamily: Less3Theme.fontFamily,
    },
    Menu: {},
    Card: {
      colorBorder: '#555555 !important',
    },
    Button: {
      borderRadius: 5,
      borderRadiusLG: 5,
      primaryColor: Less3Theme.white,
      defaultColor: '#dddddd',
      colorLink: Less3Theme.primary,
      colorLinkHover: Less3Theme.primary,
      colorBgContainerDisabled: '#333333',
    },
    Table: {
      padding: 18,
      borderColor: '#d1d5db',
    },
    Collapse: {
      headerBg: Less3Theme.white,
    },
    Input: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
    },
    Select: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
      optionSelectedColor: Less3Theme.white,
      optionSelectedBg: Less3Theme.primary,
    },
    Pagination: {
      fontFamily: Less3Theme.fontFamily,
    },
    Form: {
      labelColor: '#aaa',
      colorBorder: 'none',
      verticalLabelPadding: 0,
    },
  },
};
