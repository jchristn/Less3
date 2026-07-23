export interface CurlCommandOptions {
  method?: string;
  url: string;
  headers?: Record<string, string | undefined>;
  body?: string | null;
}

export const quoteCurlArgument = (value: string): string =>
  `"${value
    .replace(/\\/g, '\\\\')
    .replace(/"/g, '\\"')
    .replace(/\r\n/g, '\n')
    .replace(/\r/g, '\n')
    .replace(/\n/g, '\\n')}"`;

export const buildPortableCurlCommand = ({
  method = 'GET',
  url,
  headers,
  body,
}: CurlCommandOptions): string => {
  const parts = ['curl', '-X', method.toUpperCase(), quoteCurlArgument(url)];

  Object.entries(headers || {}).forEach(([name, value]) => {
    const trimmedName = name.trim();
    if (!trimmedName || !value) {
      return;
    }

    parts.push('-H', quoteCurlArgument(`${trimmedName}: ${value}`));
  });

  if (body) {
    parts.push('--data-raw', quoteCurlArgument(body));
  }

  return parts.join(' ');
};
