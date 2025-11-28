import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface AppState {
  endpoint: string | null;
}

const initialState: AppState = {
  endpoint: null,
};

const appSlice = createSlice({
  name: "app",
  initialState,
  reducers: {
    setEndpoint: (state: AppState, action: PayloadAction<string | null>) => {
      state.endpoint = action.payload;
    },
    resetEndpoint: (state: AppState) => {
      state.endpoint = null;
    },
  },
});

export const { setEndpoint, resetEndpoint } = appSlice.actions;
export default appSlice.reducer;
