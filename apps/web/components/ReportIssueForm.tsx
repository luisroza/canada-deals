"use client";

import { FormEvent, useEffect, useRef, useState } from "react";

const reasons = [
  ["PRICE_CHANGED", "Price changed"],
  ["WRONG_PRODUCT", "Wrong product"],
  ["WRONG_VARIANT", "Wrong variant"],
  ["OFFER_EXPIRED", "Offer expired"],
  ["RETAILER_PAGE_UNAVAILABLE", "Retailer page unavailable"],
  ["OTHER", "Other"],
] as const;

type ReportIssueFormProps = {
  listingId: string;
  listingLabel: string;
};

export function ReportIssueForm({ listingId, listingLabel }: ReportIssueFormProps) {
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [note, setNote] = useState("");
  const [pending, setPending] = useState(false);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [submissionError, setSubmissionError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const firstReasonRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open && !confirmation) firstReasonRef.current?.focus();
  }, [open, confirmation]);

  function closeForm() {
    setOpen(false);
    setReason("");
    setNote("");
    setFieldError(null);
    setSubmissionError(null);
    setConfirmation(null);
    requestAnimationFrame(() => triggerRef.current?.focus());
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!reason) {
      setFieldError("Choose what is wrong.");
      firstReasonRef.current?.focus();
      return;
    }

    setPending(true);
    setFieldError(null);
    setSubmissionError(null);

    try {
      const response = await fetch(`/api/v1/listings/${encodeURIComponent(listingId)}/reports`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason, note: note.trim() || null }),
      });
      const body = await response.json().catch(() => null) as { message?: string } | null;
      if (!response.ok) throw new Error("Report submission failed.");

      setConfirmation(body?.message ?? "Thanks. Your report was attached to this listing for review.");
    } catch {
      setSubmissionError("We could not send your report. Your selections are still here; please try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="report-entry" aria-labelledby="report-entry-heading">
      <h3 id="report-entry-heading">Is something wrong?</h3>
      <p className="product-meta">Send a review signal if this price or listing no longer looks right.</p>
      <button
        ref={triggerRef}
        className="button button-secondary"
        type="button"
        aria-expanded={open}
        aria-controls="listing-report-form"
        onClick={() => {
          setOpen(true);
          setConfirmation(null);
        }}
      >
        Report stale or wrong
      </button>

      {open && (
        <div id="listing-report-form" className="report-form-panel">
          {confirmation ? (
            <div className="report-success" role="status">
              <p>{confirmation}</p>
              <p className="product-meta">A report is a review signal and does not automatically change the listing.</p>
              <button className="button button-secondary" type="button" onClick={closeForm}>Done</button>
            </div>
          ) : (
            <form onSubmit={submit} noValidate>
              <fieldset aria-describedby={fieldError ? "report-reason-error" : undefined}>
                <legend>What is wrong?</legend>
                <div className="report-options">
                  {reasons.map(([value, label], index) => (
                    <label className="report-option" key={value}>
                      <input
                        ref={index === 0 ? firstReasonRef : undefined}
                        type="radio"
                        name="report-reason"
                        value={value}
                        checked={reason === value}
                        onChange={() => {
                          setReason(value);
                          setFieldError(null);
                        }}
                      />
                      <span>{label}</span>
                    </label>
                  ))}
                </div>
              </fieldset>
              {fieldError && <p id="report-reason-error" className="field-error" role="alert">{fieldError}</p>}

              <label className="report-note-label" htmlFor="report-note">Optional note</label>
              <textarea
                id="report-note"
                maxLength={500}
                rows={4}
                value={note}
                onChange={(event) => setNote(event.target.value)}
                aria-describedby="report-note-hint"
              />
              <p id="report-note-hint" className="field-hint">Plain text, up to 500 characters. Do not include personal information.</p>
              <p className="field-hint">Attached to: {listingLabel}</p>

              {submissionError && <p className="field-error" role="alert">{submissionError}</p>}
              <div className="report-actions">
                <button className="button button-primary" type="submit" disabled={pending}>
                  {pending ? "Sending report…" : "Send report"}
                </button>
                <button className="button button-secondary" type="button" disabled={pending} onClick={closeForm}>Cancel</button>
              </div>
            </form>
          )}
        </div>
      )}
    </section>
  );
}
