import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import { copyToClipboard } from '#/utils/clipboardUtils';

jest.mock('#/utils/clipboardUtils', () => ({
  copyToClipboard: jest.fn(),
}));

describe('TextWithCopy', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (copyToClipboard as jest.Mock).mockResolvedValue(true);
  });

  it('renders the text and the shared copy control', () => {
    render(<TextWithCopy text="Test text" />);

    expect(screen.getByText('Test text')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Copy text' })).toBeInTheDocument();
  });

  it('renders with a custom className', () => {
    const { container } = render(<TextWithCopy text="Test" className="custom-class" />);

    expect(container.firstChild).toHaveClass('custom-class');
  });

  it('copies the provided text when clicked', async () => {
    render(<TextWithCopy text="Text to copy" />);

    await userEvent.click(screen.getByRole('button', { name: 'Copy text' }));

    expect(copyToClipboard).toHaveBeenCalledWith('Text to copy');
  });
});
