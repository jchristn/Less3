import { ThemeEnum } from "#/types/types";
import { createContext, useContext } from "react";

export const AppContext = createContext({
  theme: ThemeEnum.LIGHT,
  setTheme: (theme: ThemeEnum) => {},
});

export const useAppContext = () => {
  return useContext(AppContext);
};
