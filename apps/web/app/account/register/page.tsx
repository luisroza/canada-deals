import type { Metadata } from "next";
import { AccountForm } from "../../../components/AccountForm";
import { safeReturnPath } from "../../../lib/account";

export const metadata: Metadata = { title: "Create account | Deal North", robots: { index: false, follow: false } };

export default async function RegisterPage({ searchParams }: { searchParams: Promise<{ returnTo?: string }> }) {
  const params = await searchParams;
  return <AccountForm mode="register" returnTo={safeReturnPath(params.returnTo)} />;
}
