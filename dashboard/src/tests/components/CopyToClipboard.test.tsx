import { act, fireEvent, render, screen } from '@testing-library/react';
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

  it('briefly shows a copied checkmark after copy succeeds', async () => {
    jest.useFakeTimers();

    try {
      render(<CopyToClipboard text="shared text" ariaLabel="Copy shared text" />);

      const button = screen.getByRole('button', { name: 'Copy shared text' });
      await act(async () => {
        fireEvent.click(button);
        await Promise.resolve();
      });

      expect(button.querySelector('.anticon-check')).toBeInTheDocument();
      expect(button.className).toContain('copied');

      act(() => {
        jest.advanceTimersByTime(2000);
      });

      expect(button.querySelector('.anticon-check')).not.toBeInTheDocument();
      expect(button.className).not.toContain('copied');
    } finally {
      jest.useRealTimers();
    }
  });
});
