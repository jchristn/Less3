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