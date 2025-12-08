import { combineReducers, UnknownAction } from "@reduxjs/toolkit";

import { localStorageKeys, paths } from "#/constants/constant";
import { rtkQueryErrorLogger } from "./rtk/rtkApiMiddleware";
import appReducer from "./reducer/appReducer";
import sdkSlice from "./rtk/rtkSdkInstance";

const rootReducer = combineReducers({
  app: appReducer,
  [sdkSlice.reducerPath]: sdkSlice.reducer,
});

export const apiMiddleWares = [rtkQueryErrorLogger, sdkSlice.middleware];

const resettableRootReducer = (
  state: ReturnType<typeof rootReducer> | undefined,
  action: UnknownAction
) => {
  return rootReducer(state, action);
};

export default resettableRootReducer;
