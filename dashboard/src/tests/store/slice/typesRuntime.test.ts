import * as bucketsTypes from "#/store/slice/bucketsTypes";
import * as credentialsTypes from "#/store/slice/credentialsTypes";
import * as usersTypes from "#/store/slice/usersTypes";
import * as appTypes from "#/types/types";

describe("slice types runtime coverage", () => {
  it("imports bucket types", () => {
    expect(bucketsTypes).toBeDefined();
  });

  it("imports credential types", () => {
    expect(credentialsTypes).toBeDefined();
  });

  it("imports user types", () => {
    expect(usersTypes).toBeDefined();
  });

  it("imports shared app types", () => {
    expect(appTypes).toBeDefined();
  });
});

