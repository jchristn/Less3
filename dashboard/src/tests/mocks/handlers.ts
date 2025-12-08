import { http, HttpResponse } from "msw";
import { mockServerURL } from "./server";

export const handlers = [
  http.head(`${mockServerURL}/`, () => {
    return HttpResponse.json({});
  }),
];

// Success handler for connectivity validation
export const successHandlers = [
  http.head(`${mockServerURL}/`, () => {
    return new HttpResponse(null, { status: 200 });
  }),
];

// Error handler for connectivity validation failure
export const errorHandlers = [
  http.head(`${mockServerURL}/`, () => {
    return new HttpResponse(null, { status: 500 });
  }),
];

// Network error handler
export const networkErrorHandlers = [
  http.head(`${mockServerURL}/`, () => {
    return HttpResponse.error();
  }),
];
