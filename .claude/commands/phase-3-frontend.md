---
title: Phase 3 — Frontend
description: React 18 + TypeScript SPA for WordBuddy.UI
status: draft
---

## Context

Separate repository: <https://github.com/hunglu/WordBuddy.UI>
Local path: D:\MyProgramming\microservices\WordBuddy\sources\WordBuddy.UI\
Stack: React 18, Vite, TypeScript strict, TailwindCSS, Framer Motion,
Zustand, TanStack Query, React Hook Form + Zod, React Router v6.
Target audience: children and adults — UI must be friendly, large text,
colorful, smooth animations.

## Requirements

- [ ] Vite + React 18 + TypeScript project initialized
- [ ] TailwindCSS, Framer Motion, Zustand, TanStack Query configured
- [ ] Folder structure: api/, components/, pages/, store/, hooks/, types/, utils/
- [ ] Typed API client with JWT interceptor and 401 redirect
- [ ] TypeScript types matching all backend DTOs
- [ ] Zustand auth store with localStorage persistence
- [ ] Public routes: /login, /register
- [ ] Protected routes: /, /lessons, /lessons/:id, /progress
- [ ] ProtectedRoute component with token expiry check
- [ ] AppLayout with sidebar (large icons + labels, child-friendly)
- [ ] LoginPage + RegisterPage with React Hook Form + Zod validation
- [ ] LessonsPage with type + level filters, animated card grid
- [ ] LessonDetailPage rendering Vocabulary / Grammar / DailyPhrase content
- [ ] ProgressPage with stats + completed lesson list
- [ ] CLAUDE.md written for frontend conventions
- [ ] README.md updated
- [ ] Pushed to github.com/hunglu/WordBuddy.UI

## Implementation Plan

### Step 1 — Project init

```batch
npm create vite@latest . -- --template react-ts
npm install
npm install react-router-dom axios zustand @tanstack/react-query
npm install react-hook-form zod @hookform/resolvers
npm install framer-motion
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

Configure vite.config.ts: proxy /api → <https://localhost:5001>.
Create .env.development: VITE_API_URL=<https://localhost:5001>.

### Step 2 — Types

In src/types/ create interfaces matching backend DTOs:
User, Lesson, LessonDetail, VocabularyItem, GrammarRule, DailyPhrase,
MediaAsset, LearnerProgress.
Enums: AgeGroup, Level, LessonType.

### Step 3 — API client

src/api/client.ts: Axios instance, JWT interceptor from Zustand store,
401 → clear store + redirect /login.
src/api/auth.ts: loginUser, registerUser.
src/api/lessons.ts: getLessons(filters?), getLessonDetail(id).
src/api/progress.ts: recordProgress, getUserProgress.

### Step 4 — Auth store

src/store/authStore.ts: Zustand store, token + user state,
login/logout actions, persist to localStorage.

### Step 5 — Routing + layouts

App.tsx: React Router v6 with PublicLayout and AppLayout.
ProtectedRoute: checks auth store, redirects if no token or expired.
AppLayout: sidebar with 📚 Lessons, ✅ My Progress, 👤 Profile.
Animate page transitions with Framer Motion AnimatePresence.

### Step 6 — Auth pages

LoginPage: email + password, Zod validation, error display, link to /register.
RegisterPage: display name, email, password, confirm password,
AgeGroup toggle (Child / Adult as large toggle buttons, not dropdown).
Both: Framer Motion entrance animation.

### Step 7 — Lessons pages

LessonsPage: filter tabs (Vocabulary | Grammar | Daily Phrases),
level pills (Beginner | Intermediate | Advanced),
responsive card grid, stagger entrance animation.
Card colors: Vocabulary = blue, Grammar = green, Daily Phrases = orange.
LessonDetailPage: conditional rendering by lesson type,
VocabularyList with audio button, GrammarRuleList with example chips,
DailyPhraseList with audio/video buttons.
"Mark as Complete" button → POST /api/progress.

### Step 8 — Progress page

Stats row: total completed, average score, streak (UI only).
Completed lesson cards with score badge.
Empty state with CTA to /lessons.
Count-up animation on stats.

### Step 9 — CLAUDE.md + README

CLAUDE.md: stack, folder structure, conventions
(TanStack Query for server state, no raw useEffect for fetching,
no any, Zustand for client state only, Framer Motion for all animations).
README.md: what this repo is, link to backend, stack badges,
folder structure, getting started, routes table, env vars table.

### Step 10 — Commit + push

```batch
git init
git remote add origin https://github.com/hunglu/WordBuddy.UI.git
git add .
git commit -m "feat: phase 3 — react frontend with auth, lessons, progress"
git branch -M main
git push -u origin main
```

## Verification

- `npm run dev` → opens localhost:5173
- Login with seeded admin credentials → redirected to dashboard
- /lessons shows 3 seeded lessons with filter working
- /lessons/:id shows lesson content
- /progress shows empty state with CTA
- All routes redirect unauthenticated users to /login
