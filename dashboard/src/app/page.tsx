import { Metadata } from "next";
import LoginPage from "#/page/login/LoginPage";

export const metadata: Metadata = {
  title: "Less3 | Home",
  description: "Less3 | Home",
};

export default function Home() {
  return <LoginPage />;
}
