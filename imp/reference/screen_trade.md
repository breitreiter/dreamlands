---
kind: reference
title: Trade Screen (Buy/Sell)
created: 2026-02-21
updated: 2026-02-21
status: current
touches:
  files:
    - ui/web/src/screens/Market.tsx
  features: [ui, trade, market]
provenance:
  author: migration:M-001
subject: Buy and Sell tab layouts — affordances, constraints, Skyrim-style quantity prompts
---

# Trade

## Buy
- available space in pack. highlight if full
- available space in scrip, highlight if full
- list of items, grouped by type
- per-item price
- quantity available
- buy button with skyrim affordance (one at a time if <5 available, count prompt if >4)
- disable buy button if can't afford or no space in pack

## Sell
- list of items, grouped by type
- merge pack and scrip inventory
- per-item price
- quantity in inventory
- for weapons/armor, equipped status
- tooltip for any bonus effects
- sell button with skyrim affordance
