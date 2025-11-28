import { GUIDRegex } from "#/constants/regex";

export const getDashboardPathKey = (path: string) => {
  const pathArray = path.split("/");
  let pathKey = pathArray?.length > 1 ? pathArray[pathArray.length - 1] : "";
  let patentPathKey =
    pathArray?.length > 1 ? pathArray[pathArray.length - 2] : "";
  pathKey = GUIDRegex.test(pathKey) ? "dashboard" : pathKey;
  patentPathKey = GUIDRegex.test(patentPathKey) ? "" : patentPathKey;
  patentPathKey = patentPathKey === "dashboard" ? "" : patentPathKey;
  return { pathKey, patentPathKey };
};

export function transformToOptions<
  T extends { GUID: string; name?: string; Name?: string }
>(
  data?: T[] | null,
  labelField: keyof T = "name" // Field to use for label in options
) {
  return (
    data?.map((item: T) => ({
      value: item.GUID,
      label: (item[labelField] as string) || item.Name || item.GUID,
    })) || []
  );
}
