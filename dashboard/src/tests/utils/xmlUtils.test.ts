import {
  generateBucketACLXml,
  generateBucketTaggingXml,
  parseBucketACL,
  parseBucketTagging,
} from "#/utils/xmlUtils";

describe("xmlUtils", () => {
  describe("parseBucketTagging", () => {
    it("parses multiple tags into structured array", () => {
      const xml = `
        <Tagging>
          <TagSet>
            <Tag>
              <Key>env</Key>
              <Value>prod</Value>
            </Tag>
            <Tag>
              <Key>team</Key>
              <Value>core &amp; dev</Value>
            </Tag>
          </TagSet>
        </Tagging>
      `;

      const result = parseBucketTagging(xml);
      expect(result).toEqual([
        { Key: "env", Value: "prod" },
        { Key: "team", Value: "core & dev" },
      ]);
    });

    it("returns empty array when TagSet is empty", () => {
      const xml = `
        <Tagging>
          <TagSet />
        </Tagging>
      `;

      expect(parseBucketTagging(xml)).toEqual([]);
    });

    it("throws when Tagging node is missing", () => {
      expect(() => parseBucketTagging("<Invalid></Invalid>")).toThrow(
        "Invalid response format: Tagging not found"
      );
    });
  });

  describe("generateBucketTaggingXml", () => {
    it("generates escaped XML for tags", () => {
      const xml = generateBucketTaggingXml([
        { Key: "env", Value: "prod" },
        { Key: "special", Value: "<value>&\"'" },
      ]);

      expect(xml).toContain("<Key>env</Key>");
      expect(xml).toContain("<Value>prod</Value>");
      expect(xml).toContain("&lt;value&gt;&amp;&quot;&apos;");
    });
  });

  describe("parseBucketACL", () => {
    it("parses owner and single grant", () => {
      const xml = `
        <AccessControlPolicy>
          <Owner>
            <ID>owner-id</ID>
            <DisplayName>Owner Name</DisplayName>
          </Owner>
          <AccessControlList>
            <Grant>
              <Grantee>
                <ID>grantee-id</ID>
                <DisplayName>Grantee Name</DisplayName>
              </Grantee>
              <Permission>FULL_CONTROL</Permission>
            </Grant>
          </AccessControlList>
        </AccessControlPolicy>
      `;

      const result = parseBucketACL(xml);
      expect(result.Owner.ID).toBe("owner-id");
      expect(result.Owner.DisplayName).toBe("Owner Name");
      expect((result.AccessControlList.Grant as any).Permission).toBe(
        "FULL_CONTROL"
      );
    });

    it("parses multiple grants into an array", () => {
      const xml = `
        <AccessControlPolicy>
          <Owner>
            <ID>owner-id</ID>
            <DisplayName>Owner Name</DisplayName>
          </Owner>
          <AccessControlList>
            <Grant>
              <Grantee>
                <ID>one</ID>
                <DisplayName>One</DisplayName>
              </Grantee>
              <Permission>READ</Permission>
            </Grant>
            <Grant>
              <Grantee>
                <ID>two</ID>
                <DisplayName>Two</DisplayName>
                <URI>http://example.com</URI>
              </Grantee>
              <Permission>WRITE</Permission>
            </Grant>
          </AccessControlList>
        </AccessControlPolicy>
      `;

      const result = parseBucketACL(xml);
      const grants = result.AccessControlList.Grant as any[];
      expect(Array.isArray(grants)).toBe(true);
      expect(grants[0].Grantee.ID).toBe("one");
      expect(grants[1].Permission).toBe("WRITE");
    });

    it("throws when AccessControlPolicy node is missing", () => {
      expect(() => parseBucketACL("<Invalid></Invalid>")).toThrow(
        "Invalid response format: AccessControlPolicy not found"
      );
    });
  });

  describe("generateBucketACLXml", () => {
    it("produces xml containing owner and grant data", () => {
      const xml = generateBucketACLXml(
        { ID: "owner-id", DisplayName: "Owner" },
        [
          {
            Grantee: { ID: "grantee-id", DisplayName: "User" },
            Permission: "READ",
          },
        ]
      );

      expect(xml).toContain("<ID>owner-id</ID>");
      expect(xml).toContain("<DisplayName>Owner</DisplayName>");
      expect(xml).toContain("<Permission>READ</Permission>");
    });
  });
});
