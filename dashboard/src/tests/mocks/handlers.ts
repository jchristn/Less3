import { http, HttpResponse } from "msw";
import { mockServerURL } from "./server";

export const handlers = [
  http.get(`${mockServerURL}/admin/users`, () => {
    return HttpResponse.json([]);
  }),
];

// Success handler for connectivity validation
export const successHandlers = [
  http.get(`${mockServerURL}/admin/users`, () => {
    return HttpResponse.json([]);
  }),
];

// Error handler for connectivity validation failure
export const errorHandlers = [
  http.get(`${mockServerURL}/admin/users`, () => {
    return new HttpResponse(null, { status: 500 });
  }),
];

// Network error handler
export const networkErrorHandlers = [
  http.get(`${mockServerURL}/admin/users`, () => {
    return HttpResponse.error();
  }),
];
