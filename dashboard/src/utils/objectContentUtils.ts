export type ObjectContentKind = 'json' | 'xml' | 'csv' | 'markdown' | 'text' | null;

const JSON_EXTENSIONS = new Set(['json', 'map']);
const XML_EXTENSIONS = new Set(['xml', 'svg', 'xsl', 'xslt']);
const CSV_EXTENSIONS = new Set(['csv', 'tsv']);
const MARKDOWN_EXTENSIONS = new Set(['md', 'markdown', 'mdx']);
const TEXT_EXTENSIONS = new Set([
  'txt',
  'log',
  'ini',
  'cfg',
  'conf',
  'yml',
  'yaml',
  'js',
  'jsx',
  'ts',
  'tsx',
  'css',
  'scss',
  'sass',
  'less',
  'html',
  'htm',
  'sql',
  'ps1',
  'sh',
  'bat',
]);

export const getObjectExtension = (key: string): string => {
  const filename = key.split('/').pop() || key;
  const dotIndex = filename.lastIndexOf('.');

  if (dotIndex === -1 || dotIndex === filename.length - 1) {
    return '';
  }

  return filename.slice(dotIndex + 1).toLowerCase();
};

export const getObjectContentKind = (contentType?: string, key: string = ''): ObjectContentKind => {
  if (key === '..' || key.endsWith('/')) {
    return null;
  }

  const normalizedContentType = (contentType || '').toLowerCase();
  const extension = getObjectExtension(key);

  if (
    normalizedContentType.includes('json') ||
    JSON_EXTENSIONS.has(extension)
  ) {
    return 'json';
  }

  if (
    normalizedContentType.includes('xml') ||
    XML_EXTENSIONS.has(extension)
  ) {
    return 'xml';
  }

  if (
    normalizedContentType.includes('csv') ||
    normalizedContentType.includes('tab-separated-values') ||
    CSV_EXTENSIONS.has(extension)
  ) {
    return 'csv';
  }

  if (
    normalizedContentType.includes('markdown') ||
    MARKDOWN_EXTENSIONS.has(extension)
  ) {
    return 'markdown';
  }

  if (
    normalizedContentType.startsWith('text/') ||
    normalizedContentType.includes('javascript') ||
    normalizedContentType.includes('yaml') ||
    TEXT_EXTENSIONS.has(extension)
  ) {
    return 'text';
  }

  return null;
};

export const isTextObjectContent = (contentType?: string, key: string = ''): boolean =>
  getObjectContentKind(contentType, key) !== null;

export const inferTextObjectContentType = (contentType?: string, key: string = ''): string => {
  const normalizedContentType = (contentType || '').trim();
  if (normalizedContentType) {
    return normalizedContentType;
  }

  const kind = getObjectContentKind(contentType, key);

  switch (kind) {
    case 'json':
      return 'application/json';
    case 'xml':
      return 'application/xml';
    case 'csv':
      return 'text/csv';
    case 'markdown':
      return 'text/markdown';
    case 'text':
    default:
      return 'text/plain';
  }
};

export const formatJsonContent = (value: string): string => {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
};

export const tryPrettyPrintJsonContent = (value: string): string | null => {
  if (!value.trim()) {
    return null;
  }

  try {
    JSON.parse(value);
    return formatJsonContent(value);
  } catch {
    return null;
  }
};

export const formatXmlContent = (value: string): string => {
  const xml = value.replace(/>\s*</g, '><').trim();
  const tokens = xml.replace(/(>)(<)(\/*)/g, '$1\n$2$3').split('\n');
  let indentLevel = 0;

  return tokens
    .map((token) => {
      if (/^<\/.+>/.test(token)) {
        indentLevel = Math.max(indentLevel - 1, 0);
      }

      const formatted = `${'  '.repeat(indentLevel)}${token}`;

      if (/^<[^!?/][^>]*[^/]?>$/.test(token)) {
        indentLevel += 1;
      }

      return formatted;
    })
    .join('\n');
};

export const tryPrettyPrintXmlContent = (value: string): string | null => {
  const trimmed = value.trim();

  if (!trimmed.startsWith('<') || !trimmed.endsWith('>')) {
    return null;
  }

  return formatXmlContent(value);
};

export const getPrettyPrintedTextContent = (value: string, contentType?: string): string | null => {
  if (!value.trim()) {
    return null;
  }

  const normalizedContentType = (contentType || '').toLowerCase();

  if (normalizedContentType.includes('json')) {
    return tryPrettyPrintJsonContent(value);
  }

  if (normalizedContentType.includes('xml') || normalizedContentType.includes('html')) {
    return tryPrettyPrintXmlContent(value);
  }

  return tryPrettyPrintJsonContent(value) || tryPrettyPrintXmlContent(value);
};

const parseCsvLine = (line: string): string[] => {
  const values: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let index = 0; index < line.length; index += 1) {
    const char = line[index];
    const nextChar = line[index + 1];

    if (char === '"') {
      if (inQuotes && nextChar === '"') {
        current += '"';
        index += 1;
      } else {
        inQuotes = !inQuotes;
      }
      continue;
    }

    if (!inQuotes && (char === ',' || char === '\t')) {
      values.push(current);
      current = '';
      continue;
    }

    current += char;
  }

  values.push(current);
  return values;
};

export const parseCsvContent = (value: string): string[][] => {
  const lines = value
    .split(/\r?\n/)
    .map((line) => line.trimEnd())
    .filter((line) => line.length > 0);

  return lines.map(parseCsvLine);
};

export const isTabularCsv = (rows: string[][]): boolean => {
  if (rows.length < 2 || rows[0].length < 2) {
    return false;
  }

  const expectedLength = rows[0].length;
  return rows.every((row) => row.length === expectedLength);
};
