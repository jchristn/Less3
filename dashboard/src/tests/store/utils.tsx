import DashboardLayout from "#/components/layout/DashboardLayout";
import LoginLayout from "#/components/layout/LoginLayout";
import resettableRootReducer, { apiMiddleWares } from "#/store/rootReducer";
import { RootState } from "#/store/store";
import { configureStore } from "@reduxjs/toolkit";
import { render } from "@testing-library/react";

import { Provider } from "react-redux";

export const renderWithRedux = (
  ui: React.ReactNode,
  loginLayout?: boolean,
  reduxState?: RootState
) => {
  const reduxStore = reduxState
    ? configureStore({
        reducer: resettableRootReducer,
        preloadedState: reduxState as RootState,
        middleware: (gDM: any) =>
          gDM({
            serializableCheck: false,
          }).concat(apiMiddleWares),
      })
    : configureStore({
        reducer: resettableRootReducer,
        middleware: (gDM: any) =>
          gDM({
            serializableCheck: false,
          }).concat(apiMiddleWares),
      });
  return render(
    <Provider store={reduxStore}>
      {loginLayout ? (
        <LoginLayout>{ui}</LoginLayout>
      ) : (
        <DashboardLayout>{ui}</DashboardLayout>
      )}
    </Provider>
  );
};
