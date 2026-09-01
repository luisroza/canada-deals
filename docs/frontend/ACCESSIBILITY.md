# Frontend accessibility

Email-confirmation state changes use an `aria-live` region, temporary failures and invalid links use alert semantics, every resend input has a visible label, pending buttons are disabled with explicit text, and success offers a normal keyboard-accessible sign-in link. The flow does not rely on colour alone.

The current slices implement the approved accessibility direction:

- skip link, landmarks, one clear `h1`, and logical headings;
- labeled search form and semantic links/buttons;
- visible focus indicators and keyboard-compatible navigation;
- evidence, freshness, and match state expressed as text plus semantic styling;
- responsive mobile layout without essential hover-only content or horizontal comparison tables;
- 44px-intent controls and stacked mobile offer cards;
- honest unavailable, stale, empty, and error states.
- keyboard-complete inline report form with a labeled reason group, optional-note guidance, focused validation, disabled pending actions, error alerts, success announcement, and focus restoration.
- labeled email/password forms with correct autocomplete intent, visible password guidance, pending state, generic announced authentication errors, and retained field values after failure;
- Save/Saved/Remove labels and `aria-pressed` state so persistence is never color-only;
- signed-out Save explanation receives keyboard focus when opened and Cancel restores focus to the invoking control;
- `/saved` has one page heading, semantic Product headings, textual evidence/freshness/history states, announced loading/errors, and actionable signed-out/empty states.
- Target Price has a programmatic label and CAD context, decimal/range guidance, explicit consent checkbox, disabled pending state, announced errors/success, keyboard-accessible edit/remove actions, and focus movement to the edit heading.
- Alert state is conveyed in text (`Active target`, `No active target-price alert`, confirmation requirement), never by color alone; mobile controls stack without horizontal overflow.
- Discovery controls have explicit labels, a polite result-count status, an `aria-expanded` filter disclosure, keyboard-operable active-filter removal/clear links, useful zero-result recovery, and textual supported-reference/savings context.
- Mobile discovery filters use a labelled `role="dialog"` sheet with focus placed on the filter heading and restored to the Filters trigger when closed; the viewport regression test confirms no horizontal overflow.
- Product history presents its factual text summary before the chart and never relies on color to communicate `RELIABLE`, `PARTIAL`, or `UNAVAILABLE`.
- The 30/90-day controls are keyboard-operable links with programmatic `aria-current` selection and restorable URL state.
- The SVG has a programmatic title/description, actual point details, and dashed gap styling; an expandable semantic table is the complete non-hover text equivalent.
- `UNAVAILABLE` has no empty SVG/zero line, technical failure is announced separately with `role="alert"`, and the 390px viewport regression confirms the history panel remains contained.
- The streamed history fallback uses `aria-busy` plus a polite status while keeping current price/freshness and the selected range available.
- Homepage store banners derive their accessible name from the complete visible copy; active outbound banners expose new-tab context separately through an accessible description. Deal Card and Offer Page retailer actions use `target="_blank"`, `noopener`, a visible arrow, and an accessible new-tab description. Every card-level Wishlist state begins with its exact visible label (`Save`, `Saved`, `Wait`, loading, or unavailable) before adding Product context, satisfying WCAG 2.2 Label in Name without changing keyboard, focus, handoff, or pressed-state behavior.

Reduced-motion tokens, password recovery, and broader account management remain future work. Target Price uses an inline region/form rather than a dialog, so no modal semantics are required.
