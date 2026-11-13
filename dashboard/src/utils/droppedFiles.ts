/**
 * A file collected from a drag-and-drop, with its path relative to the drop root
 * (e.g. "Monique/01 - Initial Documents/contract.pdf").
 */
export interface DroppedFile {
  file: File;
  relativePath: string;
}

/**
 * Result of collecting a drop: the files to upload, plus the relative paths of
 * directories that contained no files (so they can be created as folder markers).
 */
export interface DroppedItems {
  files: DroppedFile[];
  emptyDirectories: string[];
}

const readAllEntries = (reader: FileSystemDirectoryReader): Promise<FileSystemEntry[]> =>
  new Promise((resolve, reject) => {
    const all: FileSystemEntry[] = [];
    // readEntries returns results in batches (Chrome caps each at 100); call until empty.
    const readBatch = () => {
      reader.readEntries((batch) => {
        if (batch.length === 0) {
          resolve(all);
          return;
        }
        all.push(...batch);
        readBatch();
      }, reject);
    };
    readBatch();
  });

const entryToFile = (entry: FileSystemFileEntry): Promise<File> =>
  new Promise((resolve, reject) => entry.file(resolve, reject));

const walkEntry = async (entry: FileSystemEntry, parentPath: string, result: DroppedItems): Promise<void> => {
  const path = parentPath ? `${parentPath}/${entry.name}` : entry.name;

  if (entry.isFile) {
    const file = await entryToFile(entry as FileSystemFileEntry);
    // Skip macOS Finder metadata.
    if (file.name === '.DS_Store') return;
    result.files.push({ file, relativePath: path });
    return;
  }

  if (entry.isDirectory) {
    const children = await readAllEntries((entry as FileSystemDirectoryEntry).createReader());
    const before = result.files.length + result.emptyDirectories.length;
    for (const child of children) {
      await walkEntry(child, path, result);
    }
    if (result.files.length + result.emptyDirectories.length === before) {
      result.emptyDirectories.push(path);
    }
  }
};

/**
 * Collects every file from a drop event, recursing into dropped directories.
 * Must be called synchronously inside the drop handler (before any await),
 * because DataTransfer items become inaccessible once the event returns.
 */
export const collectDroppedItems = (dataTransfer: DataTransfer): Promise<DroppedItems> => {
  const result: DroppedItems = { files: [], emptyDirectories: [] };

  const items = Array.from(dataTransfer.items || []).filter((item) => item.kind === 'file');
  const entries = items
    .map((item) => (typeof item.webkitGetAsEntry === 'function' ? item.webkitGetAsEntry() : null))
    .filter((entry): entry is FileSystemEntry => entry !== null);

  // Fallback for browsers without the entries API: plain files only.
  if (entries.length === 0) {
    const files = Array.from(dataTransfer.files).map((file) => ({ file, relativePath: file.name }));
    return Promise.resolve({ files, emptyDirectories: [] });
  }

  return (async () => {
    for (const entry of entries) {
      await walkEntry(entry, '', result);
    }
    return result;
  })();
};
