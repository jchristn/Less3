import moment from 'moment';


export const dateInMonthWordsTimeFormat = 'Do MMM YYYY, HH:mm';

export const formatDateTime = (dateTime: string, format?: string) => {
  try {
    if (dateTime) {
      return moment(dateTime).format(format || dateInMonthWordsTimeFormat);
    }
    return 'Invalid Date';
  } catch (error) {
    //eslint-disable-next-line no-console
    console.log('Error', error);
    return 'Invalid Date';
  }
};

/**
 * Formats a date string to MM/DD/YYYY, HH:mm format
 * @param dateString - The date string to format
 * @returns Formatted date string in MM/DD/YYYY, HH:mm format or '-' if invalid
 */
export const formatDate = (dateString: string): string => {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      month: '2-digit',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  } catch {
    return '-';
  }
};

// Utility function to format seconds as MM:SS
export const formatSecondsForTimer = (seconds: number): string => {
  const minutes = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
};

/**
 * Produces a short human-readable relative time hint for a future or past instant.
 * Examples: "in 42s", "in 5m", "12s ago", "expired".
 * @param dateString - ISO timestamp to compare against now.
 * @param nowMs - Optional reference time in ms (defaults to Date.now()).
 * @returns Relative time string, or '-' when the input is empty/invalid.
 */
export const formatRelativeToNow = (dateString: string, nowMs: number = Date.now()): string => {
  if (!dateString) return '-';

  const target = new Date(dateString).getTime();
  if (Number.isNaN(target)) return '-';

  const diffMs = target - nowMs;
  const past = diffMs < 0;
  const absSeconds = Math.floor(Math.abs(diffMs) / 1000);

  const formatMagnitude = (): string => {
    if (absSeconds < 60) return `${absSeconds}s`;
    if (absSeconds < 3600) return `${Math.floor(absSeconds / 60)}m`;
    if (absSeconds < 86400) return `${Math.floor(absSeconds / 3600)}h`;
    return `${Math.floor(absSeconds / 86400)}d`;
  };

  if (past) {
    return absSeconds === 0 ? 'just now' : `${formatMagnitude()} ago`;
  }

  return `in ${formatMagnitude()}`;
};