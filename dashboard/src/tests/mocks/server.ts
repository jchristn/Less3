import { setupServer } from "msw/node";
import { RequestHandler } from "msw";

export const mockServerURL = "http://127.0.0.1:8000";

export const getServer = (handlers: Array<RequestHandler>) => {
  return setupServer(...handlers);
};
