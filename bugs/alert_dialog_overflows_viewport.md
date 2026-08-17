---
kind: bug
title: "A tall AlertDialog overflows the viewport with no scroll, putting its only dismiss button out of reach"
state: open
created: 2026-08-17
severity: high
status: open — soft-locks the arrival dialog when enough deliveries land at once
touches:
  files:
    - ui/web/src/components/ui/alert-dialog.tsx
    - ui/web/src/screens/Explore.tsx
  features: [ui, dialogs, deliveries]
provenance:
  author: claude
  found_via: production play — arrived at Fenwick holding three contracts for it
---

# A tall dialog cannot be closed

## What happens

Arrive somewhere with enough deliveries resolving at once and the journey's-end
dialog grows taller than the window. The "Continue" button is below the fold, the
dialog does not scroll, and the page behind it cannot scroll it either — so there
is no way to dismiss it. Reported with three contracts all bound for Fenwick, in a
narrow window.

Nothing is corrupted; the deliveries and payouts already applied server-side. The
run is simply unplayable until the window is made taller or the tab is refreshed.

## Why it happens

`AlertDialogContent` (`ui/web/src/components/ui/alert-dialog.tsx:59`) is positioned
`fixed top-[50%] left-[50%] translate-x-[-50%] translate-y-[-50%]` with a width cap
but **no height cap and no overflow rule**. Content taller than the viewport
therefore centres and bleeds off both the top and the bottom edges. Because it is
`fixed`, the document scrollbar does not reach it.

The width cap is the reason the height is unbounded in practice: `max-w-3xl`
constrains the columns, so more content can only grow downward.

## Why three deliveries was the trigger

`Explore.tsx:807` splits into two columns only at `md:` — 
`hasJourney && hasDeliveries ? "md:grid-cols-2" : "grid-cols-1"`. Below that
breakpoint the journey summary and every delivery stack in a single column. Each
delivery renders a name, a flavor paragraph, and a payout line
(`Explore.tsx:849-863`), so three of them plus a travails list clears a short
window easily. The user's "narrow, two-column layout" reading is close: it is
narrow enough that the two columns have collapsed into one, which is what makes it
tall.

## Scope

This is in the shared component, so it is **not specific to deliveries**. Every
`AlertDialog` in the app inherits it — `Explore.tsx`, `Inventory.tsx`,
`Market.tsx`. The arrival dialog is just the one with unbounded content, and it is
also the worst case because its action button is the only way out.

## Fix options

1. **Cap and scroll the shared component** — add `max-h-[calc(100dvh-2rem)]` and
   `overflow-y-auto` to `AlertDialogContent`. One line, fixes every dialog at once.
   `dvh` rather than `vh` so mobile browser chrome is accounted for.
2. **Cap and scroll the body only**, keeping header and footer pinned — nicer,
   since "Continue" stays visible while the deliveries scroll, but it needs the
   grid wrapped in its own scroll container at each call site rather than one
   change in the component.

Recommend 1 as the immediate fix since it un-sticks every dialog, with 2 as a
follow-up for the arrival dialog specifically if the scrolled-away button proves
awkward.

## Where the fix lives

Rolled into [[mobile_layout]] rather than fixed standalone — the single-column
collapse that makes the dialog tall is the same defect that plan already exists to
address, and fixing the height cap without the column behaviour would only move the
seam. See its "Confirmed broken — the journey's-end dialog" section for the
sequenced fix, including whether the delivery list wants its own scroll region and
whether two columns should kick in earlier than `md:`.

The one-line height cap is called out there as jumping the plan's ordering, since
it is a live soft-lock.
