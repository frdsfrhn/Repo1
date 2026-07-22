# Flux Generation Console

A small local web app for text-to-image generation with Flux, using your
[fal.ai](https://fal.ai) account and purchased credit. It's a thin front-end —
all inference runs on fal.ai's hosted Flux models over their API; this app
just gives you a prompt box, parameter controls, a progress view, and a
local gallery/history of what you've generated.

## Features

- Prompt → generate with Flux.1 `dev` (higher quality) or `schnell` (fast)
- Controls for image size, steps, guidance scale, seed, batch count, output format
- Live progress while a job is queued/running on fal.ai
- Every generation is saved locally (image file + metadata: prompt, model,
  seed, params) in `outputs/` and a SQLite index, browsable in the Gallery tab
- "Reuse settings" from any past generation to re-populate the form
- A per-request **safety checker toggle** that maps directly to fal.ai's own
  `enable_safety_checker` parameter on the Flux endpoints

## Safety checker toggle

fal.ai's Flux endpoints run an automatic NSFW/content filter by default. The
Generate tab has a checkbox, on by default, that controls the
`enable_safety_checker` field sent with each request — unchecking it disables
that filter for that request via fal.ai's own documented API parameter (no
bypass or workaround involved). You're responsible for complying with
fal.ai's usage policies and any applicable law for what you generate with it
off — this is your account and your credit.

## Setup

Prerequisites: Python 3.10+, and a fal.ai account with an API key
(https://fal.ai/dashboard/keys) and purchased credit.

1. Copy `.env.example` to `.env` and set `FAL_KEY` to your fal.ai API key.
2. Start the app:
   - Windows: double-click `run.bat` (or run it from a terminal)
   - macOS/Linux: `./run.sh`

   Either script creates a virtualenv, installs dependencies, and starts the
   server at `http://127.0.0.1:8000`, opening it in your browser on Windows.

The API key stays server-side (read from `.env`) and is never sent to the
browser or bundled into the frontend.

## Project layout

```
backend/
  main.py        FastAPI routes (generate, job polling, gallery, settings)
  fal_client.py  fal.ai queue API wrapper (submit / poll / fetch result)
  storage.py     SQLite-backed gallery/history
  config.py      .env + settings.json loading
frontend/
  index.html, app.js, styles.css   single-page UI, no build step
outputs/         generated images + gallery.db (gitignored, created at runtime)
```

## Notes

- Flux's fal.ai endpoints don't take a negative prompt parameter, so there's
  no negative-prompt field in the UI.
- `guidance_scale` only applies to the `dev` model; it's hidden when
  `schnell` is selected.
- Settings (default model/size/safety-checker, output folder) are stored in
  `settings.json` (gitignored, created on first save).
