"use client";
import "@ant-design/v5-patch-for-react-19";
import { Inter } from "next/font/google";
import { App, ConfigProvider } from "antd";
import { darkTheme, primaryTheme } from "#/theme/theme";
import { AntdRegistry } from "@ant-design/nextjs-registry";
import { StyleProvider } from "@ant-design/cssinjs";
import StoreProvider from "#/store/StoreProvider";
import "jsoneditor-react/es/editor.min.css";
import "#/assets/css/globals.scss";
import { AppContext } from "#/hooks/appHooks";
import { ThemeEnum } from "#/types/types";
import { useEffect, useState } from "react";
import { localStorageKeys } from "#/constants/constant";
import classNames from "classnames";
import AppMessageBridge from "#/components/antd/AppMessageBridge";

const inter = Inter({ subsets: ["latin"] });

const getThemeFromLocalStorage = () => {
  if (typeof window === "undefined") {
    return ThemeEnum.LIGHT;
  }

  const theme = window.localStorage.getItem(localStorageKeys.theme);
  return theme ? (theme as ThemeEnum) : ThemeEnum.LIGHT;
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [theme, setTheme] = useState<ThemeEnum>(ThemeEnum.LIGHT);

  useEffect(() => {
    setTheme(getThemeFromLocalStorage());
  }, []);

  const handleThemeChange = (theme: ThemeEnum) => {
    window.localStorage.setItem(localStorageKeys.theme, theme);
    setTheme(theme);
  };

  return (
    <html lang="en">
      <head>
        <link rel="icon" href="/assets/logo.png" sizes="any" />
        <link
          rel="apple-touch-icon"
          href="/assets/logo.png"
          type="image/png"
          sizes="32x32"
        />
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link
          rel="preconnect"
          href="https://fonts.gstatic.com"
          crossOrigin="anonymous"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap"
          rel="stylesheet"
        />
      </head>
      <body
        suppressHydrationWarning
        className={classNames(
          inter.className,
          theme === ThemeEnum.DARK ? "theme-dark-mode" : ""
        )}
      >
        <StoreProvider>
          <AppContext.Provider value={{ theme, setTheme: handleThemeChange }}>
            <StyleProvider layer>
              <AntdRegistry>
                <ConfigProvider
                  theme={theme === ThemeEnum.DARK ? darkTheme : primaryTheme}
                >
                  <App message={{ maxCount: 1 }}>
                    <AppMessageBridge>{children}</AppMessageBridge>
                  </App>
                </ConfigProvider>
              </AntdRegistry>
            </StyleProvider>
          </AppContext.Provider>
        </StoreProvider>
      </body>
    </html>
  );
}
