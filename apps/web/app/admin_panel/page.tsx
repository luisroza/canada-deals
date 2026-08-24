import type { Metadata } from "next";
import { AdminPanel } from "../../components/AdminPanel";

export const metadata: Metadata = {
  title: "Administration | GreatDeals.ca",
  robots: { index: false, follow: false, noarchive: true, nosnippet: true },
};

export default function AdminPanelPage() {
  return <AdminPanel />;
}
