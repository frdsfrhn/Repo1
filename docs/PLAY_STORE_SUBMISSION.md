# Submitting PropertyMate to Google Play

## The realistic timeline

Code readiness isn't the bottleneck — Google's own process is. For a
**new** Play Developer account specifically:

1. **Register** (developer.android.com/console, one-time $25 fee) and pass
   **identity verification** — usually fast, but can take a few days.
2. Upload a **signed release build** to a **closed testing** track.
3. Run that closed test with **at least 20 opted-in testers for 14
   continuous days** — this is a hard Google Play policy gate for new
   accounts (Play Console → Policy → App content will show your exact
   status). Nothing shortens this.
4. Only after that period completes can you promote the build to
   **production**.

So: earliest possible production date is roughly *(account verification
time) + 14 days*, assuming everything else is ready before the closed
test starts. Get the closed test running as early as possible — it's the
long pole.

## 1. Generate a signing keystore (one-time)

From your machine, once `android/` exists (`flutter create ...` per the
main README):

```bash
keytool -genkey -v -keystore ~/propertymate-release.jks \
  -keyalg RSA -keysize 2048 -validity 10000 -alias propertymate
```

You'll be prompted for a password and some identity fields — keep the
password safe, you cannot recover it, and you need the *same* keystore
for every future update to this app (Google Play enforces this).

Create `android/key.properties` (**do not commit this file** — add it to
`.gitignore` if it isn't already covered):

```properties
storePassword=<the password you chose>
keyPassword=<the password you chose>
keyAlias=propertymate
storeFile=/absolute/path/to/propertymate-release.jks
```

In `android/app/build.gradle`, wire it into the release signing config
(exact syntax depends on the Flutter template version `flutter create`
generates — the current template has a commented-out example for this;
uncomment/adapt it to read from `key.properties` as shown above).

## 2. Build the release bundle

```bash
flutter build appbundle --release \
  --dart-define=REVENUECAT_API_KEY_ANDROID=goog_xxx
```

Output: `build/app/outputs/bundle/release/app-release.aab` — this is what
you upload to Play Console (not an APK; Play Console wants an AAB).

## 3. Store listing draft

**App name:** PropertyMate

**Short description** (max 80 characters):
> Lead CRM for property agents — voice notes, AI cleanup, commissions.

**Full description** (draft — edit to your voice):
> PropertyMate is a lightweight CRM built specifically for property
> agents. Track every lead from first contact to closed deal: log
> voice notes on the go and let AI clean up the transcript, calculate
> commission and rental yield automatically, keep a full activity
> history per lead, and jump straight into WhatsApp or Telegram with a
> prospect without leaving the app.
>
> - Lead dashboard with search and sort
> - Voice-to-text activity notes with AI-cleaned transcripts
> - Commission and rental yield auto-calculated per deal
> - One-tap WhatsApp / Telegram from any lead
> - Attach a property one-pager photo so you always know which listing
>   a lead is about
> - Phone-number sign-in, no passwords
> - Free trial, then a simple subscription

**Category:** Business

**Content rating questionnaire:** answer as a business/productivity tool
with no user-generated public content, no ads, no violence/mature content
— this should land in the lowest rating tier on every platform.

## 4. Data Safety form (Play Console → App content → Data safety)

Declare, matching `docs/PRIVACY_POLICY.md` and `firestore.rules`:

| Data type | Collected? | Shared? | Purpose |
|---|---|---|---|
| Name | Yes (prospect name, not the agent's) | No | App functionality |
| Phone number | Yes (agent's, for sign-in; prospects', entered by agent) | No | App functionality, account management |
| Photos | Yes (property one-pager) | No | App functionality |
| Audio (voice) | Yes, transient — deleted after transcription | Yes, to Google Cloud Speech-to-Text and Anthropic for processing only | App functionality |
| App activity | Yes (leads, notes, follow-ups you enter) | No | App functionality |

Mark data as **encrypted in transit** (Firebase does this by default) and
note that **users can request deletion** (Settings → Delete account, and
per-lead deletion) — Play Console has explicit yes/no fields for both.

## 5. Before you start the closed test

- [ ] `AppConstants.privacyPolicyUrl` and `AppConstants.supportEmail`
      point at real, live values (done — see `lib/core/constants.dart`;
      re-check the privacy policy URL still resolves once this branch
      merges, since it currently points at the feature branch).
- [ ] App icon generated from the real logo (done — run
      `dart run flutter_launcher_icons` locally if you haven't since the
      last logo update).
- [ ] Signed AAB builds successfully (§2 above).
- [ ] At least 20 people who'll actually opt in as testers (Play Console
      → Testing → Closed testing → create an email list or share the
      opt-in link) — this is the part most teams underestimate; line
      these people up *before* you start the clock.
- [ ] Screenshots taken from the real running app (phone + optionally
      tablet) — at least 2, Play Console requires a minimum set per
      device type you support.
- [ ] Feature graphic: 1024×500 PNG/JPEG banner for the store listing —
      not generated yet; ask if you'd like one derived from
      `assets/branding/logo-highres.svg`.
