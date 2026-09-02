# React + TypeScript + Vite Template (Minimal & Unopinionated)

Lightweight starter for building React web applications with **TypeScript**, **Vite**, **Tailwind v4**, and **shadcn/ui**. This variant deliberately avoids prescribing global state, data‑fetching, or persistence patterns so you can introduce only what your project actually needs.

## Design Goals

1. Minimal: Only core build + UI tooling; no data/store library bundled.
2. Familiar: Conventional file layout; easy to navigate/extend.
3. Flexible: Add React Query, Redux, Zustand, Jotai, TanStack Table, etc. only if/when required.
4. Accessible: UI layer based on Radix + shadcn/ui primitives (extend, don't fork).
5. Fast: Vite dev server + modern Tailwind pipeline.
6. Typed: TypeScript strict settings (see `tsconfig.json`).
7. Adaptable: Clean extension points; comments use simple `TODO:` markers.

---

## Quick Start

```bash
# Install dependencies
pnpm install   # or yarn / npm

# Start dev server
pnpm dev

# Type check
pnpm exec tsc --noEmit

# Lint
pnpm lint

# Build
pnpm build

# Preview
pnpm preview
```

Open http://localhost:5173 (default Vite port).

---

## Project Structure

```
src/
  App.tsx                 # App entry – mounts routing + providers (kept lean)
  main.tsx                # Vite bootstrap
  pages/                  # Route-level screens
    _layout.tsx           # Shared shell (header/nav/slots) – extend here
    index.tsx             # Example landing page (skeleton placeholder)
    not-found.tsx
  components/
    system/               # Optional app-wide providers (currently pass-through)
    ui/                   # shadcn/ui components (add via generator)
  lib/
    utils.ts              # Place general helpers here
  hooks/                  # Reusable custom hooks
```

`TODO:` comments indicate spots you can safely replace or extend.

---

## Choosing State & Data Libraries

This template ships with none. Common add-ons (install only what you need):

| Need                             | Consider                                   |
| -------------------------------- | ------------------------------------------ |
| Server data fetching/caching     | TanStack Query (@tanstack/react-query)     |
| Local global state               | Jotai                                      |
| URL-based routing (already here) | react-router-dom                           |
| Tables / data grids              | TanStack Table                             |
| Charts                           | Recharts / d3                              |

Keep initial complexity low—start with React state + custom hooks; introduce a library only after repeated patterns emerge.

---

## Theming & Design

Tailwind + shadcn/ui supply the base system. If you want dynamic theme switching you can add a theme provider (e.g. `next-themes`). The current template keeps styling static to stay neutral. Use semantic classes (`bg-background`, `text-muted-foreground`) instead of raw colors.

---

## Accessibility

Ensure all interactive elements have discernible labels; maintain visible focus styles; test with keyboard navigation early.

---

## Adding UI Components (shadcn/ui)

```bash
# Add a new component interactively (example)
pnpm dlx shadcn@latest add button
```

Guidelines:
* Generate into `components/ui/`.
* Prefer wrapping vs modifying generated source directly.
* Remove unused components to keep bundle small.

---

## Routing

React Router is set up in `App.tsx`. Add a file in `src/pages/` and register a `<Route>` under the layout. Adjust `_layout.tsx` for shared navigation or chrome.

---

## Patterns (Optional)

Start simple:
* Local component state
* Lift state up when needed
* Extract reusable hooks in `hooks/`

Only introduce a global store or cache once you can articulate the problem it solves (duplication, prop drilling, cache invalidation, etc.).

---

## Implementation Tips

* Keep early commits small & focused.
* Co-locate feature components with related hooks/utilities when cohesive.
* Provide loading / empty / error states near the UI that needs them.
* Mark follow-ups with `TODO:` notations.

---

## Testing (Optional)

Recommended stack:
* Vitest
* @testing-library/react
* @testing-library/jest-dom

Example install:
```
pnpm add -D vitest @testing-library/react @testing-library/jest-dom
```

---

## Production Considerations (Not Included)

Add as needed:
* Auth / session handling
* API layer / data fetching strategy
* Error boundaries & logging
* Service worker / PWA features
* Performance instrumentation

---

## Scripts Summary

| Script        | Purpose                   |
| ------------- | ------------------------- |
| dev           | Start Vite dev server     |
| build         | Type check + build        |
| lint          | Lint sources              |
| preview       | Preview production build  |
| ui:add        | Add shadcn/ui component   |

---

## License

Adapt freely inside your organization. Attribution appreciated but not required.

---

## Changelog Guidance

Summarize PR changes using conventional commit style, e.g.:
```
feat(tasks): add task list + create form
 - add Task type
 - create/useTasks hook with basic CRUD
 - add empty & loading states
```

---

## Final Notes

Add dependencies intentionally; justify large ones in PR description if non-trivial.

Happy building.

