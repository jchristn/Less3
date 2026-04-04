/**
 * Copies text to clipboard with fallback for non-secure contexts (HTTP on non-localhost).
 * navigator.clipboard.writeText requires a secure context (HTTPS or localhost),
 * so we fall back to a textarea + execCommand('copy') approach for plain HTTP.
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  // Try the modern Clipboard API first (works on HTTPS and localhost)
  if (navigator.clipboard && window.isSecureContext) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Fall through to legacy approach
    }
  }

  // Legacy fallback for non-secure contexts (HTTP on non-localhost)
  try {
    const textarea = document.createElement('textarea');
    textarea.value = text;

    // Prevent scrolling and make invisible
    textarea.style.position = 'fixed';
    textarea.style.left = '0';
    textarea.style.top = '0';
    textarea.style.width = '2em';
    textarea.style.height = '2em';
    textarea.style.padding = '0';
    textarea.style.border = 'none';
    textarea.style.outline = 'none';
    textarea.style.boxShadow = 'none';
    textarea.style.background = 'transparent';
    textarea.style.opacity = '0';

    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();

    // Select all text for iOS compatibility
    textarea.setSelectionRange(0, textarea.value.length);

    const success = document.execCommand('copy');
    document.body.removeChild(textarea);
    return success;
  } catch {
    return false;
  }
}
