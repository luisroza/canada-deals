# Canada Deals — Text Wireframes

**Status:** APPROVED — Human UX Checkpoint completed
**Purpose:** Make the approved UX behavior concrete without selecting frontend technology or creating application code.

## Conventions

- `[CTA]` is an actionable control.
- `(status)` is a visible state or label.
- `{data}` is evidence that must come from the product/data layer.
- `→` indicates navigation or a state transition.
- Every layout below has desktop and mobile behavior.

## 1. Homepage — desktop

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Logo Canada Deals     [ Search a product or model...                 ]  Deals │
│                                                             Saved  Alerts  Me  │
├─────────────────────────────────────────────────────────────────────────────┤
│ Find a price you can trust                                                   │
│ Compare current prices with evidence, freshness, and safe product matching. │
│ [ Sony WH-1000XM5 or Makita drill                                      🔍 ]  │
│                                                                             │
│ CURRENT PRICE  •  EVIDENCE  •  FRESHNESS  •  SAFE COMPARISON                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ Deals with strong evidence                              [View all deals] │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐          │
│ │ image        │ │ image        │ │ image        │ │ image        │          │
│ │ product      │ │ product      │ │ product      │ │ product      │          │
│ │ $X CAD       │ │ $X CAD       │ │ $X CAD       │ │ $X CAD       │          │
│ │ Checked ...  │ │ Checked ...  │ │ Checked ...  │ │ Checked ...  │          │
│ │ Same product │ │ Same product │ │ Price only   │ │ Partial hist │          │
│ │ [Details]    │ │ [Details]    │ │ [Details]    │ │ [Details]    │          │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘          │
├─────────────────────────────────────────────────────────────────────────────┤
│ How it works: Discover → Verify → Compare                                    │
│ Save products and set a target-price email alert when you are ready.         │
└─────────────────────────────────────────────────────────────────────────────┘
```

Purpose: begin with search and establish the trust model before the feed. The first useful deal must expose current price, freshness, and evidence state. Cards link to Product Pages; no retailer CTA is required on the homepage.

## 2. Homepage — mobile

```text
┌──────────────────────────────┐
│ ☰  Canada Deals          ◯   │
│ [ Search a product...     🔍 ]│
├──────────────────────────────┤
│ Find a price you can trust   │
│ Current price • Evidence     │
│ Freshness • Safe comparison  │
├──────────────────────────────┤
│ Deals with strong evidence   │
│ ┌──────────────────────────┐ │
│ │ image  Product title     │ │
│ │        $X CAD             │ │
│ │        Checked today     │ │
│ │        Same product      │ │
│ │        [View details]    │ │
│ └──────────────────────────┘ │
│ [View all deals]             │
├──────────────────────────────┤
│ Home  Deals  Search  Saved Me│
└──────────────────────────────┘
```

The search, first card, and primary evidence remain above explanatory content. Bottom navigation does not replace visible page headings or focus order.

## 3. Deals feed — desktop

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Deals                                                     [search field]     │
├───────────────┬─────────────────────────────────────────────────────────────┤
│ Filters       │  Deals for you to verify             Sort: [Most recently checked]│
│ Category      │  124 results   [Clear all]                                   │
│ Retailer      │ ┌─────────────────────────────────────────────────────────┐ │
│ Price         │ │ image │ Product title · retailer                        │ │
│ Freshness     │ │       │ $X CAD   $Y observed reference                  │ │
│ Match         │ │       │ Checked 2h ago · Same product confirmed · [Save] [Details]  │ │
│ Availability  │ └─────────────────────────────────────────────────────────┘ │
│ [Apply]       │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ ...                                                     │ │
└───────────────┴─────────────────────────────────────────────────────────────┘
```

Filters are reversible and show active counts. The initial default sort is Most recently checked. Other available choices may include Largest supported savings and Lowest current price; Best evidence is a later experiment, not the initial default. Discount claims never outrank clear evidence by default.

## 4. Deals feed — mobile

```text
┌──────────────────────────────┐
│ Deals                         │
│ [Search]  [Filters (3)]      │
│ 124 results   [Most recently checked]│
├──────────────────────────────┤
│ ┌──────────────────────────┐ │
│ │ image   Product title    │ │
│ │         $X CAD           │ │
│ │         Checked 2h ago   │ │
│ │         Same product     │ │
│ │         [Details]   ⋯    │ │
│ └──────────────────────────┘ │
│ ...                          │
│ [Load more]                  │
└──────────────────────────────┘
```

The filter sheet includes Apply, Clear all, and a result count. It does not close when a user changes one filter unless the product explicitly preserves that choice.

## 5. Search results — desktop and mobile

Desktop uses a heading with the exact query, autocomplete suggestions before submit, and a results list that reuses Deal Card evidence. Mobile places the query field at the top, then result count and a filter button.

```text
Query: Sony WH-1000XM5
┌─────────────────────────────────────────────────────────┐
│ Products (8)                                             │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ Product title · model identifier                       │ │
│ │ $X CAD · Current price · Checked today · Same product confirmed │ │
│ │ [Open Product Page]                                    │ │
│ └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

No results state: repeat the query, offer a category suggestion, and invite a broader product name. Do not turn an unverified similar product into a result labeled as the requested product.

## 6. Deal Card detail anatomy

```text
┌──────────────────────────────────────────────────────────┐
│ [Product image]  Retailer · Category                     │
│ Product title and model/variant                           │
│ $X CAD                                                    │
│ $Y below observed reference  (only when supported)        │
│ Checked 2 hours ago  ·  Same product confirmed            │
│ Evidence: observed history                                │
│ [View Product]  [Save]  [⋯ Report stale or wrong]         │
└──────────────────────────────────────────────────────────┘
```

For price-only cards, replace the reference line with “No verified reference.” For stale cards, add “May be stale” and “Check retailer” as the next action. For uncertain-match cards, use “Review before comparing” and never display a savings percentage.

## 7. Product Page — desktop

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Breadcrumb: Home / Electronics / Headphones                                 │
│ Product title                                               [Save] [Alert]  │
├───────────────────────────────┬─────────────────────────────────────────────┤
│ image/gallery                  │ $X CAD                                     │
│                               │ Retailer: Best Buy Canada                  │
│                               │ Checked 2 hours ago · Same product confirmed  │
│                               │ Reference: observed history                 │
│                               │ [View at retailer]                          │
│                               │ Affiliate disclosure                        │
├───────────────────────────────┴─────────────────────────────────────────────┤
│ Retailer comparison                                                         │
│ Retailer          Price     Checked       Availability       Evidence  Action│
│ Best Buy          $X        2h ago        See retailer        High    [View] │
│ Amazon.ca         $Y        4h ago        See retailer        High    [View] │
│ Home Depot        —         —             Not available       —       —      │
├─────────────────────────────────────────────────────────────────────────────┤
│ Price history                                                               │
│ Current price: $499 CAD. Lowest observed since tracking began: $449 CAD.   │
│ Tracking since: March 2026.                                                 │
│ [chart]  Reliable history · Coverage: last 12 months                        │
├─────────────────────────────────────────────────────────────────────────────┤
│ Evidence and product details                  [Report stale or wrong]       │
└─────────────────────────────────────────────────────────────────────────────┘
```

The CTA opens the retailer and states that it leaves Canada Deals. Affiliate disclosure is adjacent to the retailer action, not buried in the footer. Conceptual copy: “We may earn a commission if you buy through this link.” Final legal/compliance wording remains subject to later review.

## 8. Product Page — mobile

```text
┌──────────────────────────────┐
│ ← Product                    │
│ [image]                      │
│ Product title                │
│ $X CAD                       │
│ Best Buy · Checked 2h ago    │
│ Same product confirmed · Observed hist. │
│ [View at retailer]           │
│ Affiliate disclosure         │
│ [Save]  [Target price]       │
├──────────────────────────────┤
│ Retailer offers              │
│ Best Buy  $X  High  [View]   │
│ Amazon    $Y  High  [View]   │
│ [Review possible matches]    │
├──────────────────────────────┤
│ Price history                │
│ [chart or honest unavailable]│
│ Evidence and details         │
│ [Report stale or wrong]      │
├──────────────────────────────┤
│ [View at retailer]           │ sticky, never covers content
└──────────────────────────────┘
```

The sticky CTA is approved for mobile. It appears only after the original retailer CTA leaves the viewport and contains only the primary retailer handoff, such as “View at Best Buy.” Save and Target Price remain normal page actions. It must not obscure price, evidence, content, or focus targets; it remains keyboard accessible and keeps affiliate-disclosure expectations clear.

## 9. Price-history states

Reliable:

```text
Price history · Reliable · Last 12 months
[chart with labeled dates and CAD values]
Current price: $Y   Lowest observed since tracking began: $X   Coverage: 92%
Tracking since: March 2026
```

Partial:

```text
Price history · Partial coverage
[chart for the observed period]
We have gaps in the observation period, so this is not an all-time-low claim.
```

Unavailable:

```text
Price history unavailable
We do not have enough verified history for this product yet.
Current price and retailer evidence are still available.
```

The unavailable state has no empty chart axes.

## 10. Comparison states

Same product confirmed: show offers in the primary comparison with the match label and observation time.

Uncertain match review:

```text
Possible related listing — review before comparing
The model, size, or bundle may differ. We have not included it in the price comparison.
[Inspect listing]
```

No safe comparison:

```text
No safe comparison available
We found no other listing we can confidently identify as the same product.
[Continue to retailer] [Report a missing match]
```

## 11. Save and target-price flow

```text
[Save] → signed in: Saved ✓
       → signed out: “Create an account to save this product”
                      [Continue] [Cancel]

[Target price]
Product: {title}
Notify me when price is at or below [$____ CAD]
Email: [____________]
[Create alert] [Cancel]
→ confirmation: “Alert set for {title} at $X CAD. We will email you when evidence meets this condition.”
```

Validation names the product, target, and trigger. Do not suggest a weekly digest in this P1 flow; that capability is P2.

## 12. Report stale/wrong flow

```text
Report an issue with this listing
What is wrong?
( ) Price changed   ( ) Wrong product/variant
( ) Offer expired   ( ) Retailer page unavailable
( ) Other
Optional note: [____________________________]
Source: {retailer URL/title, automatically attached}
[Send report] [Cancel]
→ “Thanks. Your report was attached to this listing for review.”
```

Errors are inline, specific, and announced. The dialog remains usable by keyboard and screen reader.

## 13. Empty, loading, and error states

Loading uses content-shaped skeletons with a short announcement. Empty feed explains the active filters and offers Clear filters. Search error preserves the query and offers Retry. Data unavailable states distinguish “not collected” from “temporarily unavailable.”

```text
No deals match these filters.
[Clear all filters] [Browse all deals]
```

## 14. Accessibility annotations

- Every screen has one clear `h1` and logical heading order.
- Product images have meaningful alt text; decorative images are ignored.
- Price, evidence, freshness, and confidence are text, not color-only badges.
- Cards expose one coherent link target plus separately labeled actions.
- Dialogs trap focus, restore focus to the invoking control, and provide Escape/Cancel.
- Charts provide a text summary and tabular or list alternative.
- Mobile filter sheets announce open/closed state and result count.
