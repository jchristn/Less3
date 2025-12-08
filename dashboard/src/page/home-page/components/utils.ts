export enum FileExtractionType {
  EXCEL = "excel",
  HTML = "html",
  MARKDOWN = "markdown",
  OCR = "ocr",
  PDF = "pdf",
  PNG = "png",
  PPT = "ppt",
  RTF = "rtf",
  TEXT = "text",
  WORD_DOC = "word_doc",
}

export const FILE_EXTRACTION_OPTIONS = [
  { value: FileExtractionType.EXCEL, label: "Excel" },
  { value: FileExtractionType.HTML, label: "HTML" },
  { value: FileExtractionType.MARKDOWN, label: "Markdown" },
  { value: FileExtractionType.OCR, label: "OCR" },
  { value: FileExtractionType.PDF, label: "PDF" },
  { value: FileExtractionType.PNG, label: "PNG" },
  { value: FileExtractionType.PPT, label: "PPT" },
  { value: FileExtractionType.RTF, label: "RTF" },
  { value: FileExtractionType.TEXT, label: "Text" },
  { value: FileExtractionType.WORD_DOC, label: "Word Doc" },
];

export const FILE_TYPE_ACCEPT_MAP: Record<FileExtractionType, string> = {
  [FileExtractionType.EXCEL]: ".xlsx,.xls",
  [FileExtractionType.HTML]: ".html,.htm",
  [FileExtractionType.MARKDOWN]: ".md,.markdown",
  [FileExtractionType.OCR]: "*", // Allow any file type for OCR
  [FileExtractionType.PDF]: ".pdf",
  [FileExtractionType.PNG]: ".png",
  [FileExtractionType.PPT]: ".pptx,.ppt",
  [FileExtractionType.RTF]: ".rtf",
  [FileExtractionType.TEXT]: ".txt",
  [FileExtractionType.WORD_DOC]: ".docx,.doc",
};
