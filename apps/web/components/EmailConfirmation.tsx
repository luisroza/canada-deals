"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { AccountApiError, confirmEmail, resendConfirmation } from "../lib/account";

type ConfirmationState = "confirming" | "confirmed" | "already-confirmed" | "invalid" | "error";

export function EmailConfirmation({ userId, code }: { userId?: string; code?: string }) {
  const started = useRef(false);
  const [state, setState] = useState<ConfirmationState>(userId && code ? "confirming" : "invalid");
  const [email, setEmail] = useState("");
  const [resendMessage, setResendMessage] = useState<string | null>(null);
  const [resending, setResending] = useState(false);

  useEffect(() => {
    if (!userId || !code || started.current) return;
    started.current = true;
    confirmEmail(userId, code)
      .then((result) => setState(result?.status === "ALREADY_CONFIRMED" ? "already-confirmed" : "confirmed"))
      .catch((error) => setState(error instanceof AccountApiError && error.status === 400 ? "invalid" : "error"));
  }, [userId, code]);

  async function resend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResending(true);
    try {
      const result = await resendConfirmation(email);
      setResendMessage(result?.message ?? "If an unconfirmed account exists for that address, a confirmation email has been sent.");
    } catch {
      setResendMessage("The request could not be completed. Please try again later.");
    } finally {
      setResending(false);
    }
  }

  return (
    <section className="account-panel" aria-labelledby="confirmation-heading" aria-live="polite">
      <p className="eyebrow">Account security</p>
      <h1 id="confirmation-heading">Confirm your email</h1>
      {state === "confirming" && <p role="status">Confirming your email…</p>}
      {state === "confirmed" && <div className="notice"><strong>Email confirmed.</strong><p>You can now sign in and create target-price alerts.</p></div>}
      {state === "already-confirmed" && <div className="notice"><strong>Email already confirmed.</strong><p>You can sign in.</p></div>}
      {state === "error" && <p className="field-error" role="alert">Confirmation is temporarily unavailable. Please try again.</p>}
      {state === "invalid" && (
        <>
          <p className="field-error" role="alert">This confirmation link is invalid or has expired.</p>
          <form onSubmit={resend}>
            <label htmlFor="confirmation-email">Email</label>
            <input id="confirmation-email" type="email" autoComplete="email" required maxLength={254} value={email} onChange={(event) => setEmail(event.target.value)} />
            <button className="button button-secondary" type="submit" disabled={resending}>{resending ? "Sending…" : "Send a new confirmation email"}</button>
          </form>
          {resendMessage && <p className="notice" role="status">{resendMessage}</p>}
        </>
      )}
      {(state === "confirmed" || state === "already-confirmed") && <p><Link className="button button-primary" href="/account/sign-in">Sign in</Link></p>}
      <p><Link href="/">Return to deals</Link></p>
    </section>
  );
}
