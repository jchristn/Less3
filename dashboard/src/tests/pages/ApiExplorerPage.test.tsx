import { renderWithRedux } from '../store/utils';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ApiExplorerPage from '#/page/api-explorer/ApiExplorerPage';

jest.mock('#/store/slice/credentialsSlice', () => ({
  useGetCredentialsQuery: () => ({
    data: [
      {
        Id: 'cred-1',
        Description: 'Primary Key',
        AccessKey: 'AK123',
        SecretKey: 'SK123',
      },
    ],
  }),
  useGetCredentialByIdQuery: () => ({
    data: {
      Id: 'cred-1',
      Description: 'Primary Key',
      AccessKey: 'AK123',
      SecretKey: 'SK123',
    },
  }),
}));

jest.mock('#/store/slice/bucketsSlice', () => ({
  useGetBucketsQuery: () => ({
    data: [
      {
        Id: 'bkt_test',
        Name: 'test-bucket',
      },
    ],
  }),
}));

jest.mock('#/store/slice/usersSlice', () => ({
  useGetUsersQuery: () => ({
    data: [
      {
        Id: 'usr_test',
        Name: 'Test User',
        Email: 'test@example.com',
      },
    ],
  }),
}));

describe('ApiExplorerPage', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    global.fetch = jest.fn();
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it('renders request, response, and recent requests in a single-column order without environment controls', () => {
    localStorage.setItem('less3_api_explorer_recent', JSON.stringify([
      {
        operationId: 's3-list-buckets',
        method: 'GET',
        url: 'http://127.0.0.1:8000/',
        apiType: 's3',
        statusCode: 200,
        timestamp: '2026-05-15T12:00:00.000Z',
        body: '',
        credentialId: '__none__',
      },
    ]));

    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    const cardTitles = Array.from(document.querySelectorAll('.ant-card-head-title')).map((element) =>
      element.textContent?.replace(/\s+/g, ' ').trim()
    );

    expect(cardTitles.slice(0, 3)).toEqual(['Request', 'Response', 'Recent Requests']);
    expect(cardTitles).not.toContain('Environment');
    expect(screen.queryByRole('button', { name: /Export Environment/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Import Environment/i })).not.toBeInTheDocument();
  });

  it('shows an always-available credential picker with a no credential option', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    expect(
      screen.getByText(
        'The selected credential will be used for this S3 request. Choose "No credential" to send it unsigned.'
      )
    ).toBeInTheDocument();

    const credentialSelect = screen.getAllByRole('combobox')[2];
    await userEvent.click(credentialSelect);

    expect((await screen.findAllByText('No credential')).length).toBeGreaterThan(1);
    expect(await screen.findByText('Primary Key (AK123)')).toBeInTheDocument();
  });

  it('keeps the credential picker visible when switching to admin operations', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    const filterSelect = screen.getAllByRole('combobox')[0];
    await userEvent.click(filterSelect);
    await userEvent.click(await screen.findByText('Admin API'));

    await waitFor(() => {
      expect(
        screen.getByText('Credential selection is preserved for S3 requests. Admin and REST requests use the saved dashboard API key.')
      ).toBeInTheDocument();
    });

    expect(screen.getByText('S3 Credential')).toBeInTheDocument();
  });

  it('shows REST operations with resource dropdown injection', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    const filterSelect = screen.getAllByRole('combobox')[0];
    await userEvent.click(filterSelect);
    await userEvent.click(await screen.findByText('REST API'));

    const operationSelect = screen.getAllByRole('combobox')[1];
    await userEvent.click(operationSelect);
    await userEvent.click(await screen.findByText('[Buckets] GET - Get Bucket'));

    await userEvent.click(screen.getAllByRole('combobox')[3]);
    expect((await screen.findAllByText('bkt_test')).length).toBeGreaterThan(0);
    await userEvent.click(await screen.findByText('test-bucket'));

    await waitFor(() => {
      expect(screen.getByText(/\/api\/v1\/buckets\/bkt_test/)).toBeInTheDocument();
    });
  });

  it('lets the user pretty-print a JSON response body', async () => {
    (global.fetch as jest.Mock).mockResolvedValue({
      status: 200,
      statusText: 'OK',
      headers: new Headers({ 'content-type': 'application/json' }),
      text: jest.fn().mockResolvedValue('{"name":"Less3","version":1}'),
    });

    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('button', { name: /Send/i }));

    expect(await screen.findByRole('button', { name: 'Pretty Print' })).toBeInTheDocument();
    expect(screen.getByText('{"name":"Less3","version":1}')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Pretty Print' }));

    await waitFor(() => {
      expect(
        screen.getByText((_, element) => element?.textContent === '{\n  "name": "Less3",\n  "version": 1\n}')
      ).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: 'Show Raw' })).toBeInTheDocument();
  });

  it('saves collections', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('button', { name: /Save/i }));

    expect(localStorage.getItem('less3_api_explorer_collections')).toContain('s3-list-buckets');
    expect(screen.getByText('Saved Collections')).toBeInTheDocument();
    expect(screen.getByText('List Buckets')).toBeInTheDocument();
  }, 15000);

  it('disables send when required parameters are missing', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    const filterSelect = screen.getAllByRole('combobox')[0];
    await userEvent.click(filterSelect);
    await userEvent.click(await screen.findByText('Admin API'));

    const operationSelect = screen.getAllByRole('combobox')[1];
    await userEvent.click(operationSelect);
    await userEvent.click(await screen.findByText(/Get User/i));

    expect(screen.getByRole('button', { name: /Send/i })).toBeDisabled();
  });
});
