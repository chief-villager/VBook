# VBook Frontend

React + Vite + TypeScript SPA for the Bookkeeping API. Deployed **separately** from
the API (this is a monorepo — one git repo, two independent deployables).

## Setup

```bash
cd frontend
npm install
cp .env.example .env   # then set VITE_API_URL to your running API
npm run dev            # http://localhost:5173
```

## Scripts

| Command           | What it does                                   |
| ----------------- | ---------------------------------------------- |
| `npm run dev`     | Vite dev server with HMR                       |
| `npm run build`   | Type-check + production build to `dist/`       |
| `npm run preview` | Serve the built `dist/` locally               |
| `npm run typecheck` | Type-check only, no emit                      |

## How it talks to the API

- `src/lib/api.ts` — the one fetch wrapper. Reads `VITE_API_URL` and attaches
  `Authorization: Bearer <token>` on every authenticated call.
- `src/lib/auth.ts` — in-memory access-token store (not `localStorage`, to reduce
  XSS exposure; see the root `CLAUDE.md` "Authentication" section).

## Deployment

Point your static host (Vercel / Netlify / Cloudflare Pages) at this `frontend/`
directory with build command `npm run build` and output directory `dist`. The API
must allow this app's origin via CORS.
