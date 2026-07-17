# Installing PropertyMate on iPhone via GitHub Actions + Sideloadly

Your MacBook Pro (Monterey, capped at Xcode 14.2) can't talk to a phone on a
much newer iOS — Xcode 14.2 only understands device protocols up to iOS
16.2. This workflow sidesteps that: GitHub's cloud Macs (running modern
Xcode) build an **unsigned** `.ipa`, and a free tool called **Sideloadly**
signs + installs it on your phone using your free Apple ID, entirely on
your own Mac (your Apple ID never touches GitHub).

## 1. One-time: add the Firebase config as a GitHub secret

The `.ipa` build needs `GoogleService-Info.plist`, which is gitignored
(machine-specific, never committed). On your Mac, get its contents:

```bash
cat ios/Runner/GoogleService-Info.plist | base64 | pbcopy
```

This copies the base64-encoded file to your clipboard. Then, in a browser:

1. Go to the repo on GitHub → **Settings → Secrets and variables →
   Actions → New repository secret**.
2. Name: `GOOGLE_SERVICE_INFO_PLIST_BASE64`
3. Value: paste (Cmd+V) the clipboard contents.
4. Save.

(This is a one-time setup — the secret persists for future builds.)

## 2. Trigger the build

1. On GitHub, go to the repo → **Actions** tab → **"iOS unsigned build
   (for local sideloading)"** workflow (in the left sidebar).
2. Click **"Run workflow"** → select the `claude/flutter-firebase-mobile-app-h7fh9p`
   branch → **Run workflow**.
3. Wait for it to finish (typically 10-20 minutes on a macOS runner).
4. Once green, click into the run → under **Artifacts**, download
   `PropertyMate-unsigned-ipa` (a zip containing `PropertyMate-unsigned.ipa`).

## 3. Install Sideloadly on your Mac

Download from [sideloadly.io](https://sideloadly.io) (free) and install it
like any other Mac app.

## 4. Sideload onto your iPhone

1. Connect your iPhone via USB, unlock it, trust the computer if asked.
2. Open Sideloadly — it should detect your phone.
3. Drag `PropertyMate-unsigned.ipa` into Sideloadly's window (or use the
   folder icon to browse to it).
4. Enter your Apple ID email. Sideloadly will ask for an
   **app-specific password** (not your normal Apple ID password) —
   generate one at [appleid.apple.com](https://appleid.apple.com) under
   **Sign-In and Security → App-Specific Passwords**.
5. Click **Start**. Sideloadly signs the app with a free 7-day certificate
   (same limit as local Xcode signing) and installs it directly.
6. On the phone, the first launch may need one-time trust:
   **Settings → General → VPN & Device Management** → tap the developer
   profile → **Trust**.

## Re-signing after 7 days

Free Apple ID certificates expire weekly. Re-run Sideloadly with the same
`.ipa` (no need to rebuild via GitHub Actions unless the code changed) to
re-sign and reinstall — takes under a minute.

## When the code changes

Re-run the GitHub Actions workflow (step 2) to get a fresh `.ipa`, then
sideload it again (step 4).
