import { setupServer } from "msw/node";
import { RequestHandler } from "msw";

export const mockServerURL = "http://localhost:3000";

export const getServer = (handlers: Array<RequestHandler>) => {
  return setupServer(...handlers);
};
