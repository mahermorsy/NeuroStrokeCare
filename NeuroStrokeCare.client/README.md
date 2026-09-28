# NeuroStrokeCare.client

React + TypeScript + Vite frontend for NeuroStrokeCare, styled to match the
approved Dashboard design (calm clinical teal/white palette, Newsreader +
Public Sans typography, restrained Framer Motion transitions).

## Run locally

```bash
npm install
npm run dev
```

The dev server proxies `/api` to `http://localhost:5271` (see
`vite.config.ts`) — update that target if your API runs on a different port.
Open the app, then sign in through `/login`; the Dashboard is the landing
page once authenticated.

**Before it will actually log in**, open `src/context/AuthContext.tsx` and
check the field names in the `login()` function (`data.token`, `data.email`,
etc.) against your `AuthController`'s real login response shape — they're a
reasonable guess, not confirmed against your exact DTO.

## Build

```bash
npm run build
```

Outputs to `dist/`.

## Docker

```bash
docker build -t neurostrokecare-client .
docker run -p 3000:80 neurostrokecare-client
```

In production, nginx (see `nginx.conf`) serves the built app and proxies
`/api/*` to a container named `api` on port 8080 — see
`docker-compose.snippet.yml` for how to wire this up alongside
`NeuroStrokeCare.api` from the solution root.

## Structure

- `src/lib/api.ts` — shared axios instance (relative `/api` base URL, JWT
  attached from `localStorage`, redirects to `/login` on 401).
- `src/context/AuthContext.tsx` — login/logout state.
- `src/components/Layout.tsx` — sidebar nav + page shell.
- `src/pages/Dashboard.tsx` — the approved Dashboard screen.
- `src/pages/ComingSoon.tsx` — placeholder for the other nav sections
  (Patients, Admissions, Wards & Beds, Assessments, Lab Results, Door
  Timing) until each is designed.

## Next screens

Dashboard was designed and approved first. Tell me which screen to design
next (Patients, Admissions, Wards & Beds, …) and I'll mock it up the same
way before building it.
