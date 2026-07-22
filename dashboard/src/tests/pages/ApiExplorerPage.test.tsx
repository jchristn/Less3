import { renderWithRedux } from '../store/utils';
import { fireEvent, screen, waitFor } from '@testing-library/react';
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
    await userEvent.click(await screen.findByText('test-bucket (bkt_test)'));

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

  it('saves collections and round-trips explorer environment data', async () => {
    renderWithRedux(<ApiExplorerPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('button', { name: /Save/i }));

    expect(localStorage.getItem('less3_api_explorer_collections')).toContain('s3-list-buckets');
    expect(screen.getByText('Saved Collections')).toBeInTheDocument();
    expect(screen.getByText('List Buckets')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /Export Environment/i }));

    const environmentEditor = screen.getByRole('textbox') as HTMLTextAreaElement;
    expect(environmentEditor.value).toContain('"operationId": "s3-list-buckets"');

    fireEvent.change(environmentEditor, {
      target: {
        value: '{"operationId":"rest-list-tenants","selectedCredentialId":"cred-1","paramValues":{}}',
      },
    });
    await userEvent.click(screen.getByRole('button', { name: /Import Environment/i }));

    await waitFor(() => {
      expect(localStorage.getItem('less3_api_explorer_environment')).toContain('rest-list-tenants');
    });
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
