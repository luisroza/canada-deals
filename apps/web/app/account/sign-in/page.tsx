import type { Metadata } from "next";
import { AccountForm } from "../../../components/AccountForm";
import { safeReturnPath } from "../../../lib/account";

export const metadata: Metadata = { title: "Sign in | Canada Deals", robots: { index: false, follow: false } };

export default async function SignInPage({ searchParams }: { searchParams: Promise<{ returnTo?: string }> }) {
  const params = await searchParams;
  return <AccountForm mode="sign-in" returnTo={safeReturnPath(params.returnTo)} />;
}
