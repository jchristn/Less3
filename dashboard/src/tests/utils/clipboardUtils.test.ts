import { copyToClipboard } from '#/utils/clipboardUtils';

describe('clipboardUtils', () => {
  const originalClipboardDescriptor = Object.getOwnPropertyDescriptor(navigator, 'clipboard');
  const originalSecureContextDescriptor = Object.getOwnPropertyDescriptor(window, 'isSecureContext');
  const originalExecCommand = document.execCommand;

  const setClipboard = (writeText: jest.Mock) => {
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText },
    });
  };

  const setSecureContext = (isSecureContext: boolean) => {
    Object.defineProperty(window, 'isSecureContext', {
      configurable: true,
      value: isSecureContext,
    });
  };

  afterEach(() => {
    jest.restoreAllMocks();

    if (originalClipboardDescriptor) {
      Object.defineProperty(navigator, 'clipboard', originalClipboardDescriptor);
    } else {
      delete (navigator as Navigator & { clipboard?: Clipboard }).clipboard;
    }

    if (originalSecureContextDescriptor) {
      Object.defineProperty(window, 'isSecureContext', originalSecureContextDescriptor);
    } else {
      delete (window as Window & { isSecureContext?: boolean }).isSecureContext;
    }

    document.execCommand = originalExecCommand;
  });

  it('uses navigator.clipboard in secure contexts such as HTTPS or localhost', async () => {
    const writeText = jest.fn().mockResolvedValue(undefined);
    const execCommand = jest.fn().mockReturnValue(true);
    setClipboard(writeText);
    setSecureContext(true);
    document.execCommand = execCommand;

    await expect(copyToClipboard('secure text')).resolves.toBe(true);

    expect(writeText).toHaveBeenCalledWith('secure text');
    expect(execCommand).not.toHaveBeenCalled();
  });

  it('falls back to execCommand in insecure HTTP contexts such as non-localhost', async () => {
    const writeText = jest.fn().mockResolvedValue(undefined);
    const execCommand = jest.fn().mockReturnValue(true);
    setClipboard(writeText);
    setSecureContext(false);
    document.execCommand = execCommand;

    await expect(copyToClipboard('plain http text')).resolves.toBe(true);

    expect(writeText).not.toHaveBeenCalled();
    expect(execCommand).toHaveBeenCalledWith('copy');
  });

  it('falls back to execCommand when the modern clipboard API rejects', async () => {
    const writeText = jest.fn().mockRejectedValue(new Error('denied'));
    const execCommand = jest.fn().mockReturnValue(true);
    setClipboard(writeText);
    setSecureContext(true);
    document.execCommand = execCommand;

    await expect(copyToClipboard('fallback text')).resolves.toBe(true);

    expect(writeText).toHaveBeenCalledWith('fallback text');
    expect(execCommand).toHaveBeenCalledWith('copy');
  });
});
