import {
  buildSignedS3Headers,
  clearPreferredS3CredentialId,
  getPreferredS3CredentialId,
  selectS3Credential,
  setPreferredS3CredentialId,
} from "#/utils/s3Auth";

describe("s3Auth", () => {
  const originalCrypto = globalThis.crypto;

  beforeEach(() => {
    localStorage.clear();
    clearPreferredS3CredentialId();
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
      url: "http://127.0.0.1:8000/default/?tagging",
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

  it("selectS3Credential prefers the persisted credential id", () => {
    setPreferredS3CredentialId("crd_preferred");

    const selected = selectS3Credential([
      { Id: "crd_other", AccessKey: "other", SecretKey: "other-secret" },
      { Id: "crd_preferred", AccessKey: "preferred", SecretKey: "preferred-secret" },
    ]);

    expect(getPreferredS3CredentialId()).toBe("crd_preferred");
    expect(selected?.AccessKey).toBe("preferred");
  });

  it("selectS3Credential falls back to the default access key", () => {
    const selected = selectS3Credential([
      { Id: "one", AccessKey: "one", SecretKey: "one-secret" },
      { Id: "crd_default", AccessKey: "default", SecretKey: "default-secret" },
    ]);

    expect(selected?.AccessKey).toBe("default");
  });
});
