import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import JsonViewerModal from '#/components/json-viewer-modal/JsonViewerModal';
import { copyToClipboard } from '#/utils/clipboardUtils';

jest.mock('#/utils/clipboardUtils', () => ({
  copyToClipboard: jest.fn(),
}));

describe('JsonViewerModal', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (copyToClipboard as jest.Mock).mockResolvedValue(true);
  });

  it('renders a copy icon and copies the formatted JSON payload', async () => {
    render(
      <JsonViewerModal
        open={true}
        title="Bucket JSON"
        data={{ Name: 'default' }}
        onClose={jest.fn()}
        width={760}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Copy JSON' }));

    expect(copyToClipboard).toHaveBeenCalledWith('{\n  "Name": "default"\n}');
  });
});
