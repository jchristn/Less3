import {
  buildSignedS3Headers,
  clearPreferredS3CredentialGuid,
  getPreferredS3CredentialGuid,
  selectS3Credential,
  setPreferredS3CredentialGuid,
} from "#/utils/s3Auth";

describe("s3Auth", () => {
  const originalCrypto = globalThis.crypto;

  beforeEach(() => {
    localStorage.clear();
    clearPreferredS3CredentialGuid();
  });

  afterEach(() => {
    Object.defineProperty(globalThis, "crypto", {
      configurable: true,
      value: originalCrypto,
    });
  });

  it("buildSignedS3Headers creates SigV4 headers", async () => {
    const headers = await buildSignedS3Headers({
      method: "GET",
      url: "http://localhost:8000/default/?tagging",
      accessKey: "default",
      secretKey: "default",
      timestamp: new Date("2026-05-14T12:34:56.000Z"),
    });

    expect(headers.Authorization).toContain(
      "Credential=default/20260514/us-west-1/s3/aws4_request"
    );
    expect(headers.Authorization).toContain("SignedHeaders=host;x-amz-content-sha256;x-amz-date");
    expect(headers.Authorization).toMatch(/Signature=[a-f0-9]{64}$/);
    expect(headers["x-amz-content-sha256"]).toBe(
      "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    );
    expect(headers["x-amz-date"]).toBe("20260514T123456Z");
  });

  it("buildSignedS3Headers falls back when subtle crypto is unavailable", async () => {
    Object.defineProperty(globalThis, "crypto", {
      configurable: true,
      value: {},
    });

    const headers = await buildSignedS3Headers({
      method: "GET",
      url: "http://public.less3.example:8000/default/?tagging",
      accessKey: "default",
      secretKey: "default",
      timestamp: new Date("2026-05-14T12:34:56.000Z"),
    });

    expect(headers.Authorization).toContain(
      "Credential=default/20260514/us-west-1/s3/aws4_request"
    );
    expect(headers.Authorization).toContain("SignedHeaders=host;x-amz-content-sha256;x-amz-date");
    expect(headers.Authorization).toMatch(/Signature=[a-f0-9]{64}$/);
    expect(headers["x-amz-content-sha256"]).toBe(
      "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    );
    expect(headers["x-amz-date"]).toBe("20260514T123456Z");
  });

  it("selectS3Credential prefers the persisted credential guid", () => {
    setPreferredS3CredentialGuid("preferred-guid");

    const selected = selectS3Credential([
      { GUID: "other-guid", AccessKey: "other", SecretKey: "other-secret" },
      { GUID: "preferred-guid", AccessKey: "preferred", SecretKey: "preferred-secret" },
    ]);

    expect(getPreferredS3CredentialGuid()).toBe("preferred-guid");
    expect(selected?.AccessKey).toBe("preferred");
  });

  it("selectS3Credential falls back to the default access key", () => {
    const selected = selectS3Credential([
      { GUID: "one", AccessKey: "one", SecretKey: "one-secret" },
      { GUID: "default-guid", AccessKey: "default", SecretKey: "default-secret" },
    ]);

    expect(selected?.AccessKey).toBe("default");
  });
});
