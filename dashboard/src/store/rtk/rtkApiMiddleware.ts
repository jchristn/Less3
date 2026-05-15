import {
  Dispatch,
  isRejectedWithValue,
  Middleware,
  MiddlewareAPI,
  UnknownAction,
} from "@reduxjs/toolkit";
import { message } from "#/utils/message";

const getMessageFromValue = (value: unknown): string | null => {
  if (typeof value === "string" && value.trim()) {
    return value;
  }

  if (!value || typeof value !== "object") {
    return null;
  }

  const record = value as Record<string, unknown>;

  return (
    getMessageFromValue(record.Message) ||
    getMessageFromValue(record.Description) ||
    getMessageFromValue(record.message) ||
    getMessageFromValue(record.error) ||
    getMessageFromValue(record.title) ||
    getMessageFromValue(record.detail) ||
    getMessageFromValue(record.data)
  );
};

export const errorHandler = (err: any, dispatch: Dispatch<UnknownAction>) => {
  const error = err?.payload || {};
  //eslint-disable-next-line no-console
  console.log(error, "chk errorHandler error");
  message.error(getMessageFromValue(error) || "Something went wrong.");
  if (error.Error === "NotAuthorized") {
    message.error("Session expired. Redirecting to login page...");
    setTimeout(() => {
      // dispatch(logOut());
    }, 3000);
  }
};

export const rtkQueryErrorLogger: Middleware =
  (api: MiddlewareAPI) => (next: (action: any) => any) => (action: any) => {
    const { dispatch } = api;
    // RTK Query uses `createAsyncThunk` from redux-toolkit under the hood, so we're able to utilize these matchers!
    if (isRejectedWithValue(action)) {
      errorHandler(action, dispatch);
    }

    return next(action);
  };
