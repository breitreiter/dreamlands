# UI Tech Stack Decisions

## Stack

- **Framework:** React + TypeScript
- **Component Library:** shadcn/ui (copies actual source files into your project — no black box)
- **Styling:** Tailwind CSS (comes with shadcn; `tailwind.config` is your semantic definition layer)
- **Build Tool:** Vite (flat project, no Next.js or heavy framework needed)
- **Backend:** C# Azure Functions, called via plain `fetch`

## Why This Stack

The goal was balancing Claude Code fluency with Joseph's ability to reason about the architecture. React + TypeScript has the deepest training data representation, so Claude Code produces idiomatic output. TypeScript's type system (interfaces, generics, type narrowing) maps well to C# experience. React's component + hooks model rhymes with MVVM: `useState`/`useReducer` ≈ observable properties, `useEffect` ≈ property-changed handlers.

shadcn over MUI because shadcn components are visible source files you can read and modify. MUI buries behavior behind theme objects, `sx` overrides, and will silently accumulate three different styling approaches in the same project.

## What You Need to Learn

One concept: **React re-renders components when state changes, and data flows top-down.** Understanding this lets you catch ~90% of architectural mistakes Claude Code might make (unnecessary state, prop drilling that should be context, effects that fire in loops). Spend an afternoon with the React docs on state and effects.

## Figma → Code Pipeline

- **Figma API works on free tier** (personal access token from account settings). MCP server works too.
- **Code Connect does NOT work on free tier** — requires Organization or Enterprise plan. Not needed anyway.
- **Figma UI Kit:** Use the [Obra shadcn/ui community edition](https://www.figma.com/community/file/1514746685758799870/obra-shadcn-ui-kit-community-edition) — free, MIT licensed, every shadcn component as composable Figma components.
- **Primary code gen method:** Screenshot/export designs → feed to Claude Code. Shockingly effective. Use MCP server to pull precise measurements and color values as a supplement.

### Figma API Scraper Approach

Build a lightweight scraper that walks the Figma component instance tree and emits hierarchy + variant selections + text overrides. Keep Figma component names aligned with shadcn names (`Button`, `Card`, `Dialog`, `Select`, etc.) and use Figma variants for shadcn variant props (`variant=destructive`, `size=sm`).

Output something like:

```
Card > CardHeader > CardTitle("Game Setup") + CardDescription("Configure your session")
Card > CardContent > Select(placeholder="Choose biome") + Button(variant="primary", "Start")
```

Claude Code turns that into working React trivially. No sophisticated parser needed.

## What We Explicitly Decided Against

| Option | Reason |
|--------|--------|
| Blazor | The "C# on the web" trap — you'd understand it but Claude Code produces mediocre Blazor |
| MUI | Enormous, opinionated, styling approaches silently diverge, hard to reason about when it breaks |
| Vue 3 | Reasonable fallback if React doesn't click, but Claude Code is slightly less fluent |
| Figma-to-code plugins (Locofy, Anima) | Produce architecturally terrible code (absolute positioning, no component reuse). Claude Code is better |
| Complex framework (Next.js, Redux) | Unnecessary for a game UI talking to Azure Function endpoints |