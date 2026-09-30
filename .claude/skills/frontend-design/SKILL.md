---
name: frontend-design
description: Designs and builds WordBuddy.UI screens and components that look and feel like the rest of the app — kid-friendly, bright, rounded, sky/emerald/amber palette, Tailwind utilities plus CSS Modules, Framer Motion animation, TanStack Query loading/error/empty states. Use whenever the user asks to create, redesign, restyle, or polish a page, component, layout, card, form, or empty/error state in WordBuddy.UI, or says "make it look nicer", "design a screen for X", "UI for feature Y". Not for backend work (use develop-webapi).
---

# WordBuddy frontend design

Make new UI indistinguishable from the existing pages (`WordBuddy.UI/src/pages/LessonsPage.tsx` is
the reference). Hard rules come from `WordBuddy.UI/CLAUDE.md` — Tailwind utilities + CSS Modules, Framer Motion only,
TanStack Query only, strict TS without `enum`. This skill adds the *visual* language.

## 1. Before designing

1. Read the feature's living spec (`docs/features/<feature>.md`) or `plan.md` — know the audience.
   **Child vs. adult matters:** child screens get bigger type, bigger targets, more emoji/colour,
   fewer words; adult screens may be denser.
2. Open 1–2 existing pages closest to what you're building and reuse their structure.
3. Check `src/components/` for something reusable before writing a new component.

## 2. Visual language (design tokens as Tailwind classes)

| Token | Use | Classes |
|---|---|---|
| Primary | headings, primary buttons, active tabs | `text-sky-900` headings, `bg-sky-500`/`bg-sky-600` buttons, `text-white` |
| Secondary text | body, hints, loading | `text-sky-600` / `text-sky-700` |
| Surface | cards, inactive pills | `bg-white`, `shadow-sm`, border `border-sky-200` |
| Vocabulary | type accent | `bg-sky-100 text-sky-800 border-sky-300` |
| Grammar / success | type accent, correct answer | `bg-emerald-100 text-emerald-800 border-emerald-300`, `bg-emerald-500` |
| Daily phrase / highlight | type accent, streaks | `bg-amber-100 text-amber-800 border-amber-300` |
| Error / wrong answer | errors only | `text-rose-600`, `bg-rose-100` |
| Shape | everything is soft | cards `rounded-3xl border-2 p-5`, inputs/buttons `rounded-xl`, pills/tabs `rounded-full` |
| Type scale | page title / card title / body / meta | `text-3xl font-extrabold` / `text-xl font-bold` / `text-base`–`text-lg` / `text-sm font-semibold` |
| Layout | grids and spacing | `grid gap-4 sm:grid-cols-2 lg:grid-cols-3`, sections spaced `mt-4`/`mt-6`/`mt-8` |

Never introduce a new colour family, font, or radius without asking Sam. Emoji are the icon set
(🔤 📐 💬 ⭐ 🔥) — no icon library.

## 3. Motion (Framer Motion only)

- Page entrance: `<motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>`.
- Lists/grids: stagger with `delay: index * 0.05`, `y: 16`, `duration: 0.25`.
- Feedback (correct/wrong, score reveal): `scale` pop or shake via `animate`; keep < 400 ms.
- Reusable variants must be typed: `import { type Variants } from 'framer-motion'`.
- No CSS `animation`/keyframes. Respect `useReducedMotion()` for anything beyond a fade.

## 4. Every data-driven screen has four states

```tsx
{isLoading && <p className="mt-8 text-lg text-sky-600">Loading …</p>}
{isError && <p className="mt-8 text-lg text-rose-600">Couldn't load … right now. Please try again in a moment.</p>}
{data && data.length === 0 && <p className="mt-8 text-lg text-sky-600">No … yet.</p>}
{data && data.length > 0 && /* content */}
```

Copy is friendly and plain — no stack traces, status codes, or "null". A down service must never
blank the page.

## 5. Accessibility & responsiveness

- Real `<button type="button">` / `<Link>`; never clickable `<div>`s.
- Touch targets ≥ 44px (`px-4 py-2` minimum; child screens `px-5 py-3 text-lg`).
- Visible focus: add `focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-sky-300`.
- Contrast: text on `*-100` backgrounds uses the `*-800` shade.
- Form fields: `<label>` tied to input, errors from Zod shown under the field in `text-rose-600 text-sm`.
- Mobile-first: works at 360px wide, then `sm:`/`lg:` enhancements.

## 6. Checklist before finishing

- [ ] Tailwind utilities or a `Component.module.css` (with `@reference` to `index.css`); colours only from theme tokens; no `style={{}}` except Framer Motion props.
- [ ] Loading / error / empty / content all handled.
- [ ] Child vs. adult presentation considered and stated in your summary.
- [ ] Keyboard-navigable, visible focus, ≥ 44px targets.
- [ ] `npm run build` and `npm run lint` pass in `WordBuddy.UI`.
- [ ] If the change alters behaviour of a feature, note it for `docs/features/<feature>.md`.
