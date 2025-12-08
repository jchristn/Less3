"use client";
import DashboardLayout from "#/components/layout/DashboardLayout";
import withConnectivityValidation from "#/hoc/hoc";
import React from "react";

const Layout = ({ children }: { children: React.ReactNode }) => {
  return <DashboardLayout>{children}</DashboardLayout>;
};

export default withConnectivityValidation(Layout);
