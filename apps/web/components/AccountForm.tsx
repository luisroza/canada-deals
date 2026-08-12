"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { register, resendConfirmation, safeReturnPath, signIn } from "../lib/account";

export function AccountForm({ mode, returnTo }: { mode: "register" | "sign-in"; returnTo: string }) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const destination = safeReturnPath(returnTo);
  const isRegister = mode === "register";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);
    setMessage(null);
    try {
      const result = isRegister ? await register(email, password) : await signIn(email, password);
      if (result?.isAuthenticated) {
        router.push(destination);
        router.refresh();
      } else {
        setMessage(result?.message ?? "Check your email before signing in.");
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "The account request could not be completed.");
    } finally {
      setPending(false);
    }
  }

  async function resend() {
    setPending(true);
    setError(null);
    try {
      const result = await resendConfirmation(email);
      setMessage(result?.message ?? "If an unconfirmed account exists for that address, a confirmation email has been sent.");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "The confirmation request could not be completed.");
    } finally {
      setPending(false);
    }
  }

  const alternatePath = `${isRegister ? "/account/sign-in" : "/account/register"}?returnTo=${encodeURIComponent(destination)}`;

  return (
    <section className="account-panel" aria-labelledby="account-heading">
      <p className="eyebrow">Save your product context</p>
      <h1 id="account-heading">{isRegister ? "Create an account" : "Sign in"}</h1>
      <p className="lede">{isRegister ? "Create the minimum account needed to keep saved products." : "Sign in to access your saved products."}</p>
      <form onSubmit={submit} noValidate>
        <label htmlFor="account-email">Email</label>
        <input id="account-email" name="email" type="email" autoComplete="email" required maxLength={254} value={email} onChange={(event) => setEmail(event.target.value)} />
        <label htmlFor="account-password">Password</label>
        <input id="account-password" name="password" type="password" autoComplete={isRegister ? "new-password" : "current-password"} required minLength={isRegister ? 10 : undefined} maxLength={128} value={password} onChange={(event) => setPassword(event.target.value)} aria-describedby={isRegister ? "password-hint" : undefined} />
        {isRegister && <p id="password-hint" className="field-hint">At least 10 characters with upper case, lower case, and a number.</p>}
        {error && <p className="field-error" role="alert">{error}</p>}
        {message && <p className="notice" role="status">{message}</p>}
        <button className="button button-primary" type="submit" disabled={pending}>{pending ? "Please wait…" : isRegister ? "Create account" : "Sign in"}</button>
      </form>
      {isRegister && message && <button className="button button-secondary" type="button" onClick={resend} disabled={pending}>Resend confirmation email</button>}
      <p className="account-alternate">{isRegister ? "Already have an account?" : "Need an account?"} <Link href={alternatePath}>{isRegister ? "Sign in" : "Create one"}</Link></p>
      <p><Link href={destination}>Continue without signing in</Link></p>
    </section>
  );
}
