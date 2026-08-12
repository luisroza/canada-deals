import type { Metadata } from "next";
import { EmailConfirmation } from "../../../components/EmailConfirmation";

export const metadata: Metadata = { title: "Confirm email | Canada Deals", robots: { index: false, follow: false } };

export default async function ConfirmEmailPage({ searchParams }: { searchParams: Promise<{ userId?: string; code?: string }> }) {
  const params = await searchParams;
  return <EmailConfirmation userId={params.userId} code={params.code} />;
}
