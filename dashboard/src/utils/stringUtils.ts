import { message } from "antd";
import { v4 } from "uuid";

export const toTitleCase = (str: string): string => {
  return str
    .toLowerCase()
    .replace(/-/g, " ") // Replace hyphens with spaces
    .split(" ")
    .map((word: string) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(" ");
};

export const getFirstLetterOfTheWord = (value: string) => {
  return (value?.substring(0, 1) || "").toUpperCase();
};

export const uuid = () => {
  return v4();
};

export const decodePayload = (payload: string) => {
  try {
    const decodedPayload = atob(payload);
    return JSON.parse(decodedPayload);
  } catch (error) {
    // eslint-disable-next-line no-console
    console.error(error);
    message.error("Failed to decode payload.");
    return payload;
  }
};
