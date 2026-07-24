import { configureStore } from '@reduxjs/toolkit';
import { localStorageKeys } from '#/constants/constant';
import { requestHistorySliceApi } from '#/store/slice/requestHistorySlice';

describe('requestHistorySlice endpoints', () => {
  const originalFetch = global.fetch;

  const makeStore = () =>
    configureStore({
      reducer: {
        [requestHistorySliceApi.reducerPath]: requestHistorySliceApi.reducer,
      },
      middleware: (gDM) => gDM().concat(requestHistorySliceApi.middleware),
    });

  beforeEach(() => {
    localStorage.setItem(localStorageKeys.adminApiKey, 'less3admin');
  });

  afterEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    global.fetch = originalFetch as any;
  });

  it('loads the newest request history page by default', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: 'OK',
      headers: new Headers({ 'content-type': 'application/json' }),
      text: async () =>
        JSON.stringify([
          {
            Id: 'req_latest',
            HttpMethod: 'GET',
            RequestUrl: '/',
            SourceIp: '127.0.0.1',
            StatusCode: 200,
            Success: true,
            DurationMs: 2,
            RequestType: 'ListBuckets',
            UserId: 'usr_default_admin',
            AccessKey: 'default',
            RequestContentType: '',
            RequestBodyLength: 0,
            ResponseContentType: 'application/xml',
            ResponseBodyLength: 128,
            RequestBody: null,
            ResponseBody: null,
            CreatedUtc: '2026-07-24T19:33:08.833406Z',
          },
        ]),
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(requestHistorySliceApi.endpoints.getRequestHistory.initiate());
    const result = await promise.unwrap();

    expect(result).toHaveLength(1);
    expect(global.fetch).toHaveBeenCalledWith(
      'http://127.0.0.1:8000/admin/requesthistory?limit=1000&offset=0&sortField=createdUtc&sortDirection=desc',
      expect.objectContaining({
        method: 'GET',
        cache: 'no-store',
        headers: expect.objectContaining({
          'x-api-key': 'less3admin',
        }),
      })
    );

    promise.unsubscribe?.();
  });
});
