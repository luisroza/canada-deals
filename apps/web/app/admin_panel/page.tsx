import type { Metadata } from "next";
import { AdminPanel } from "../../components/AdminPanel";

export const metadata: Metadata = {
  title: "Administration | Deal North",
  robots: { index: false, follow: false, noarchive: true, nosnippet: true },
};

export default function AdminPanelPage() {
  return <AdminPanel />;
}
