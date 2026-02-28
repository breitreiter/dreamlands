# UI Style Rules

These are hard rules. Follow them exactly.

## Typography

There are three typefaces, each used at exactly one size and weight. No exceptions.

| Typeface            | Weight  | Size | Use                        | Tailwind                             |
|---------------------|---------|------|----------------------------|--------------------------------------|
| Cinzel Decorative   | Regular | 32px | Page/section headers only  | `font-header text-[32px]`            |
| Alegreya Sans       | Regular | 20px | All other text             | (default — `html { font-size: 20px }`) |
| Sue Ellen Francisco | Regular | 20px | Handwritten/ledger accents | `font-hand`                          |

- **Do not** use multiple sizes of any typeface. No `text-xs`, `text-sm`, `text-lg` overrides.
- Use `font-bold` sparingly for emphasis (section subheadings, totals). Never use other weights.

## Colors

### Backgrounds

| Name          | Hex       | Tailwind        | Use                              |
|---------------|-----------|-----------------|----------------------------------|
| BG-Primary    | #232d46   | `bg-page`       | Main page background             |
| BG-Grid       | #262626   | `bg-panel`      | Panel/card backgrounds           |
| BG-Grid-Alt   | #191919   | `bg-panel-alt`  | Alternate panels, footers        |
| Parchment     | #d4c9a8   | `bg-parchment`  | Mechanics panel, Cash Book       |
| Button        | #0d0d0dcc | `bg-btn`        | Button/card background (80% opacity) |
| Button-Hover  | #292929   | `bg-btn-hover`  | Button hover state               |

### Text

| Name          | Hex       | Tailwind             | Use                              |
|---------------|-----------|----------------------|----------------------------------|
| Text-Primary  | #f3f3f3   | `text-primary`       | Default text on dark backgrounds |
| Text-Contrast | #1e1e1e   | `text-contrast`      | Text on parchment backgrounds    |
| Text-Accent   | #d0bd62   | `text-accent`        | Page headers, gold highlights    |
| Text-Dim      | #aca377   | `text-dim`           | Secondary/muted text (warm)      |
| Text-Muted    | #8b8b8b   | `text-muted`         | Tertiary/disabled text (gray)    |
| Parchment-Text| #3a3520   | `text-parchment-text`| Text on parchment (dark brown)   |

### Interactive Elements

| Name             | Hex       | Tailwind            | Use                          |
|------------------|-----------|---------------------|------------------------------|
| Action-Primary   | #d0925d   | `text-action` / `bg-action`   | **ALL** clickable elements |
| Action-Hover     | #daa878   | `text-action-hover` / `bg-action-hover` | Hover state      |
| Action-Secondary | #b29073   | `text-action-dim`   | Dimmed/inactive interactive  |

**Rule: Every clickable element uses #D0925D.** Buttons, tabs, links, close buttons — all of them.
Icon-based buttons use `bg-action` background. Text-based interactive elements use `text-action`.

### Semantic

| Name           | Hex       | Tailwind        | Use                    |
|----------------|-----------|-----------------|------------------------|
| Negative       | #ff6b6b   | `text-negative` | Damage, conditions     |
| Positive       | #6bffae   | `text-positive` | Healing, success       |
| Stat-Health    | #800000   | —               | Health bar gradient start |
| Stat-Spirits   | #460AA7   | —               | Spirits bar gradient start |

## Layout Rules

- The **Mechanics panel** uses parchment background (`bg-parchment text-contrast`) and is fixed at **420px** wide.
- Other panels share remaining space equally (`flex-1`).
- **Tab selectors** are **48px** tall (`h-12`) and include an icon + label.

## Icons

Source SVGs: `assets/icons/`. Served at `/world/assets/icons/<name>.svg`.

Pattern: `<img src="/world/assets/icons/<name>.svg" className="w-5 h-5" />`
