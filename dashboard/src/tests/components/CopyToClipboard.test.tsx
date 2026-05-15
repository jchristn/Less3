import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import { copyToClipboard } from '#/utils/clipboardUtils';

jest.mock('#/utils/clipboardUtils', () => ({
  copyToClipboard: jest.fn(),
}));

describe('CopyToClipboard', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (copyToClipboard as jest.Mock).mockResolvedValue(true);
  });

  it('copies the provided text through the shared clipboard utility', async () => {
    render(<CopyToClipboard text="shared text" ariaLabel="Copy shared text" />);

    await userEvent.click(screen.getByRole('button', { name: 'Copy shared text' }));

    expect(copyToClipboard).toHaveBeenCalledWith('shared text');
  });
});
