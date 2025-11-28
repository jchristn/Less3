import { Menu, MenuProps } from "antd";
import React, { useMemo } from "react";
import { MenuItemProps } from "./types";
import Link from "next/link";
import Sider from "antd/es/layout/Sider";
import { usePathname } from "next/navigation";
import { getDashboardPathKey } from "#/utils/appUtils";
import { inherits } from "util";

interface MenuItemsProps extends MenuProps {
  menuItems: MenuItemProps[];
  handleClickMenuItem?: (item: MenuItemProps) => void;
  collapsed: boolean;
}

const MenuItems = ({
  menuItems,
  handleClickMenuItem,
  collapsed: _collapsed,
  ...rest
}: MenuItemsProps) => {
  const pathname = usePathname();
  const { pathKey } = getDashboardPathKey(pathname);
  const serializedMenuItems = useMemo(() => {
    return menuItems.map((item: MenuItemProps) => {
      return {
        ...item,
        label: (
          <Link style={{ color: "inherit" }} href={item.path || ""}>
            {item.label}
          </Link>
        ),
        children: item.children?.map((child: MenuItemProps) => {
          return {
            ...child,
            label: <Link href={child.path || ""}>{child.label}</Link>,
          };
        }),
      };
    });
  }, [menuItems]);

  return (
    <Menu
      mode="inline"
      selectedKeys={[pathKey]}
      items={serializedMenuItems}
      {...rest}
    />
  );
};

export default MenuItems;
