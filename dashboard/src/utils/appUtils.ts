import { IdRegex } from "#/constants/regex";

export const getDashboardPathKey = (path: string) => {
  const pathArray = path.split("/");
  let pathKey = pathArray?.length > 1 ? pathArray[pathArray.length - 1] : "";
  let patentPathKey =
    pathArray?.length > 1 ? pathArray[pathArray.length - 2] : "";
  pathKey = IdRegex.test(pathKey) ? "dashboard" : pathKey;
  patentPathKey = IdRegex.test(patentPathKey) ? "" : patentPathKey;
  patentPathKey = patentPathKey === "dashboard" ? "" : patentPathKey;
  return { pathKey, patentPathKey };
};

export function transformToOptions<
  T extends { Id?: string; name?: string; Name?: string }
>(
  data?: T[] | null,
  labelField: keyof T = "name" // Field to use for label in options
) {
  return (
    data?.map((item: T) => ({
      value: item.Id || item.Name || (item[labelField] as string) || '',
      label: (item[labelField] as string) || item.Name || item.Id || '',
    })) || []
  );
}
