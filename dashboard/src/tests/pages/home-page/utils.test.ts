import { FileExtractionType, FILE_EXTRACTION_OPTIONS, FILE_TYPE_ACCEPT_MAP } from "#/page/home-page/components/utils";

describe("home-page utils", () => {
  describe("FileExtractionType enum", () => {
    it("should have all expected extraction types", () => {
      expect(FileExtractionType.EXCEL).toBe("excel");
      expect(FileExtractionType.HTML).toBe("html");
      expect(FileExtractionType.MARKDOWN).toBe("markdown");
      expect(FileExtractionType.OCR).toBe("ocr");
      expect(FileExtractionType.PDF).toBe("pdf");
      expect(FileExtractionType.PNG).toBe("png");
      expect(FileExtractionType.PPT).toBe("ppt");
      expect(FileExtractionType.RTF).toBe("rtf");
      expect(FileExtractionType.TEXT).toBe("text");
      expect(FileExtractionType.WORD_DOC).toBe("word_doc");
    });
  });

  describe("FILE_EXTRACTION_OPTIONS", () => {
    it("should contain all extraction types", () => {
      expect(FILE_EXTRACTION_OPTIONS).toHaveLength(10);
      expect(FILE_EXTRACTION_OPTIONS.find((opt) => opt.value === FileExtractionType.PDF)).toEqual({
        value: FileExtractionType.PDF,
        label: "PDF",
      });
    });

    it("should have correct labels", () => {
      const pdfOption = FILE_EXTRACTION_OPTIONS.find((opt) => opt.value === FileExtractionType.PDF);
      expect(pdfOption?.label).toBe("PDF");
    });
  });

  describe("FILE_TYPE_ACCEPT_MAP", () => {
    it("should map Excel to correct file extensions", () => {
      expect(FILE_TYPE_ACCEPT_MAP[FileExtractionType.EXCEL]).toBe(".xlsx,.xls");
    });

    it("should map PDF to correct file extension", () => {
      expect(FILE_TYPE_ACCEPT_MAP[FileExtractionType.PDF]).toBe(".pdf");
    });

    it("should map OCR to accept all files", () => {
      expect(FILE_TYPE_ACCEPT_MAP[FileExtractionType.OCR]).toBe("*");
    });

    it("should map Word Doc to correct file extensions", () => {
      expect(FILE_TYPE_ACCEPT_MAP[FileExtractionType.WORD_DOC]).toBe(".docx,.doc");
    });

    it("should have mappings for all extraction types", () => {
      Object.values(FileExtractionType).forEach((type) => {
        expect(FILE_TYPE_ACCEPT_MAP[type]).toBeDefined();
      });
    });
  });
});

