# UI Component Library

shadcn/ui provides the base component set. Components are copied into
`src/components/ui/` (no runtime dep), customized to use our color palette.

**Status: Phase 1-4 complete.** Button + AlertDialog deployed across all screens.

## Current Problem

7 distinct ad-hoc button patterns across 10 screens. No shared Button component.
Inconsistencies include: inline `rgba()` styles vs Tailwind classes, varying
disabled states (`opacity-40` vs `opacity-50` vs `disabled:bg-btn`), mismatched
sizing, and TabButton using inline `borderRadius` while everything else uses
`rounded-lg`. Every new screen reinvents button styling.

## Phase 1: Scaffolding

Set up shadcn/ui infrastructure. No visual changes yet.

1. **Path aliases** -- add `@/` alias to tsconfig.json and vite.config.ts
2. **Install shadcn** -- `npx shadcn@latest init` (creates components.json,
   installs tw-animate-css and class-variance-authority)
3. **Theme mapping** -- remap shadcn's default CSS variables to our existing
   palette in App.css. Key mappings:
   - `--primary` -> our `--color-action` (#d0925d)
   - `--primary-foreground` -> our `--color-contrast` (#1e1e1e)
   - `--secondary` -> our `--color-btn` (#0d0d0dcc)
   - `--secondary-foreground` -> our `--color-action` (#d0925d)
   - `--destructive` -> our `--color-negative` (#ff6b6b)
   - `--muted` / `--muted-foreground` -> our dim/muted colors
   - `--accent` -> our `--color-accent` (#d0bd62)
   - `--background` -> our `--color-page`
   - `--border` -> our `--color-edge`
   - `--ring` -> our `--color-action`
4. **Typography guard** -- ensure shadcn components inherit our 20px base
   font size and don't introduce text-sm/text-xs. Override in component
   source if needed.

## Phase 2: Button Component

Add `shadcn Button` and define our variant set.

1. `npx shadcn@latest add button`
2. Customize `ui/web/src/components/ui/button.tsx` variants:

| Variant       | Use case                           | Style                                           |
|---------------|------------------------------------|-------------------------------------------------|
| `default`     | Primary actions (New Game, etc.)   | `bg-action text-contrast hover:bg-action-hover` |
| `secondary`   | Standard buttons (Inn, Market)     | `bg-btn text-action hover:bg-btn-hover`         |
| `destructive` | Dangerous actions (discard confirm)| `bg-btn text-negative hover:bg-btn-hover`       |
| `ghost`       | Text-only (credits, subtle links)  | `text-muted hover:text-dim`                     |
| `link`        | Encounter choices, continue        | `text-action hover:text-action-hover`           |

3. Add size variants:

| Size      | Use case             | Style                     |
|-----------|----------------------|---------------------------|
| `default` | Standard buttons     | `px-4 py-2`              |
| `sm`      | Compact (market qty) | `px-3 py-1`              |
| `lg`      | Full-width primary   | `w-full px-8 py-3`       |
| `icon`    | Icon-only buttons    | `w-10 h-10`              |
| `icon-sm` | Small icon buttons   | `w-9 h-9`               |
| `icon-lg` | Service buttons      | `w-11 h-11`             |

All variants get: `rounded-lg`, `disabled:opacity-50`, `transition-colors`.

## Phase 3: AlertDialog Component

Add `shadcn AlertDialog` for the discard confirmation (the original trigger
for this work).

1. `npx shadcn@latest add alert-dialog`
2. Style the dialog to match parchment/dark theme
3. Replace the inline confirm pattern in Inventory.tsx with:
   ```tsx
   <AlertDialog>
     <AlertDialogTrigger asChild>
       <Button variant="secondary" size="icon">...</Button>
     </AlertDialogTrigger>
     <AlertDialogContent>
       <AlertDialogTitle>Destroy {item.name}?</AlertDialogTitle>
       <AlertDialogDescription>This cannot be undone.</AlertDialogDescription>
       <AlertDialogFooter>
         <AlertDialogCancel>Keep</AlertDialogCancel>
         <AlertDialogAction onClick={...}>Destroy</AlertDialogAction>
       </AlertDialogFooter>
     </AlertDialogContent>
   </AlertDialog>
   ```
4. Remove `confirmingDiscard` state, `ConfirmBtn`, and the red-highlight
   card logic from Inventory.tsx.

## Phase 4: Migrate Screens

Replace inline button patterns screen by screen. Order by complexity
(simplest first):

1. **GameOver.tsx** -- 1 button (primary)
2. **Splash.tsx** -- 2 primary + 1 ghost
3. **Camp.tsx** -- 1 link button
4. **Inn.tsx** -- ~5 secondary buttons + 1 link
5. **Bank.tsx** -- 2 secondary buttons
6. **Encounter.tsx** -- choice buttons (link variant)
7. **Explore.tsx** -- icon buttons + dungeon entry (destructive)
8. **Market.tsx** -- mixed secondary/icon/sm buttons
9. **Inventory.tsx** -- icon buttons + alert dialog (Phase 3)
10. **TopBar.tsx** -- back button (secondary)
11. **MaskedIcon.tsx** -- move TabButton out, keep or convert to shadcn Tabs

Each screen migration: swap `<button className="...">` for
`<Button variant="..." size="...">`, delete the inline classes,
verify visually.

## Phase 5: Cleanup

1. Delete `ActionBtn`, `ConfirmBtn` from Inventory.tsx
2. Evaluate whether `TabButton` should become a shadcn Tabs component
   or remain custom (it has specific icon-color logic)
3. Remove any orphaned inline style patterns
4. Update `project/screens/styles.md` to reference the Button component
   variants instead of raw class strings

## What We're NOT Doing

- Not adding every shadcn component -- only Button and AlertDialog for now.
  Add others (Card, Dialog, Select, etc.) as needed per screen.
- Not changing the visual design -- same colors, same typography, same layout.
  This is a consolidation, not a redesign.
- Not introducing dark mode toggle -- single theme.
- Not adding class-variance-authority beyond what shadcn generates.

## Risk Notes

- **Tailwind v4**: shadcn supports it as of Feb 2025. Uses `@theme inline`
  and CSS variables. Our existing `@theme` block will need the shadcn
  variables added alongside our custom ones.
- **Font size**: shadcn components default to browser text sizing. Our
  `html { font-size: 20px }` should carry through, but verify buttons
  don't import text-sm/text-xs utilities.
- **tw-animate-css**: replaces the old tailwindcss-animate. Shadcn init
  will add it. Minimal footprint, just CSS animations.
