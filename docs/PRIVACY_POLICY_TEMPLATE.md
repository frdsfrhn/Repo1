# Privacy Policy — template

**This is a starting draft, not legal advice.** Have a lawyer familiar
with Malaysia's Personal Data Protection Act (PDPA) — and, if you'll have
users outside Malaysia, GDPR/CCPA — review it before publishing. Host the
final version at a stable public URL and put that URL in
`lib/core/constants.dart` (`AppConstants.privacyPolicyUrl`) and in both
app store listings, per the MVP checklist's App Store requirements.

---

## Privacy Policy for [App Name]

**Last updated:** [date]

[App Name] ("the app") is operated by [Your Name / Company Name]
("we", "us"). This policy explains what data we collect from you as a
property agent using the app, why, and how you can control it.

### 1. Information we collect

- **Your account:** your phone number, used to sign you in.
- **Your clients' data, which you enter:** prospect names, phone numbers,
  property names and addresses, deal financials (commission percentage,
  deal value, expected rental yield), and follow-up dates.
- **Voice recordings:** if you record a voice note about a lead, the
  audio is uploaded and transcribed automatically. **The raw audio is
  deleted as soon as a transcript is produced** — typically within a few
  seconds to a couple of minutes — and only the text transcript is kept
  afterward.
- **Transcripts:** both the original (speech-to-text) transcript and an
  AI-cleaned version, kept side by side so you can compare them.
- **Subscription/purchase data:** handled by our payments provider,
  RevenueCat, and the app stores (Apple/Google) — we receive your
  subscription status but not your payment details.

### 2. How we use this information

- To operate the app's core features: your lead dashboard, activity log,
  and follow-up tracking.
- To transcribe and clean up your voice notes using third-party AI
  services (see §4).
- To manage your subscription and free trial.
- We do not sell your data or your clients' data to third parties.

### 3. Who can see your data

Your leads, notes, and recordings are visible only to your account. We
enforce this at the database level (Firestore security rules), not just
in the app's user interface — another agent's account cannot read or
write your data even if there were a bug in the app itself.

### 4. Third-party processors

- **Google Firebase** (Google LLC): authentication, database, file
  storage, and serverless functions that run the app's backend logic.
- **Google Cloud Speech-to-Text**: converts your voice notes to text.
- **Anthropic** (Claude API): cleans up the raw transcript into readable
  text. Anthropic does not use this data to train its models under our
  agreement with them.
- **RevenueCat**: manages subscription state across the Apple App Store
  and Google Play Store.
- **Apple / Google**: process your subscription payment; we never see
  your card details.

### 5. Data retention and deletion

- **Voice recordings** are deleted automatically once transcribed — see
  §1.
- **You can delete an individual lead** (and its full activity log and
  any recording) at any time from within the app. This is permanent.
- **You can delete your entire account** from Settings → Delete account.
  This permanently removes your profile, every lead you created, every
  activity note, and any recordings. This does not automatically cancel
  an active subscription — cancel that separately through the App Store
  or Play Store.
- If you delete your account, deletion is immediate, not a soft-disable.

### 6. Your rights under PDPA

You may request access to, correction of, or deletion of your personal
data by contacting us at [support email]. Most of this you can already do
yourself in-app (see §5).

### 7. Children

This app is intended for licensed property agents and is not directed at
children. We do not knowingly collect data from anyone under 18.

### 8. Changes to this policy

We'll update the "last updated" date above when this policy changes, and
notify you in-app for material changes.

### 9. Contact

[Support email]
[Company name and address, if applicable]
