# Agents Guide (Condensed)
Purpose: give the coding AI the smallest set of non‑negotiable rules. Everything else is optional polish.

## 1. Core Rules
1. In‑memory only. No REST/DB/localStorage/service workers. Refresh = reset.
2. Keep changes surgical: touch areas marked with `TODO(ai)` or clearly related code.
3. Business / data logic lives in hooks or `lib/` utilities; components stay declarative.
4. Reuse existing UI primitives & tokens before adding new variants.
5. Avoid new global singletons (use the provided `memory` store + existing providers).
6. Landing page (`src/pages/index.tsx`) is an OPEN EXTENSION SURFACE – you may replace it entirely.

## 2. In‑Memory Data Pattern
```ts
import { memory } from '@/lib/memory-store'
interface Item { id: string; name: string }
const items = memory.ensure<Item[]>('items', () => [])
items.push({ id: crypto.randomUUID(), name: 'Example' })
```
Guidelines: define types, expose via hooks (`useItems()`), simulate async with `react-query + setTimeout` only if needed, never assume persistence.

## 3. Annotation System (MUST FOLLOW)
| Marker     | Meaning                     | Your Action                                    |
| ---------- | --------------------------- | ---------------------------------------------- |
| `TODO(ai)` | Safe extension point / task | Implement or remove when fulfilled.            |
| `NOTE(ai)` | Rationale / constraint      | Preserve; only change with new justified NOTE. |

If you deviate from a constraint: add `NOTE(ai): justified exception – <reason>` directly above the change.

### How To Handle TODO/NOTE Markers (Per File Only)
Do NOT scan the entire repo for all TODOs (overloads reasoning). Before editing a file:

1. Grep that single file for markers:
```bash
grep -n "TODO(ai)" path/to/file || true
grep -n "NOTE(ai)" path/to/file || true
```
2. Address or implement the `TODO(ai)` items you touch.
3. Preserve `NOTE(ai)` rationale unless you provide a new justified NOTE.
4. After fulfilling a TODO, remove it (keep diffs minimal otherwise).
5. If a needed clarification is missing, add one targeted `TODO(ai): clarify <short description>` and continue.

## 4. Minimal Implementation Guidelines
1. Provide skeleton placeholders only where async simulation is present.
2. Keep accessibility: label interactive elements (`aria-label` if needed) and preserve focus states.
3. Prefer composition (small focused components) over sprawling props.
4. Add routes by extending `App.tsx` route list; avoid global rewrites.
5. For new feature state: `memory.ensure()` + a hook wrapper (e.g. `useTasks`).
6. When using components from `@/components/ui`, only reference those that exist in that folder.

## 5. Adding a Feature (Quick Path)
1. Define a type (or reuse) in a hook or a `types.ts` (if created).
2. Initialize collection with `memory.ensure()`.
3. Create `useX()` + `useCreateX()` / `useUpdateX()` hooks.
4. Build UI component(s) in a domain folder (e.g. `components/tasks/`).
5. Wire a route or place component on the landing page.
6. Add empty / loading / error states.
7. Remove satisfied `TODO(ai)` markers.

## 6. Dependency Discipline
Don't add new dependencies. Solve using native or existing code.

## 7. Security & Safety (Within Scope)
No secrets, no eval, no persistence. Treat all data as non-sensitive ephemeral state.

## 8. When Unsure
Add a precise `TODO(ai): clarify <issue>` near the code and proceed with a reasonable minimal assumption.

# IMPORTANT NOTES
** 1. App Title ** 
   - Make sure to update the app name in `src/pages/_layout.tsx` file
   - Make sure NOT to repeat the app name in the main content since its already in the page header
