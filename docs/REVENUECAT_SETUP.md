# Completing the RevenueCat setup

The app's code side is already done (`SubscriptionService`, `SubscriptionProvider`,
`PaywallScreen`, the `revenuecatWebhook` Cloud Function). What's left is
account/dashboard configuration — this doc is the exact checklist, with the
identifiers our code already expects.

## Why this matters

`AppConstants.aiFeaturesEntitlementId` = **`ai_features`** gates voice
transcription + AI cleanup — the two things that cost you money per use
(Google Speech-to-Text + Anthropic API calls). Until RevenueCat is wired up,
that gate has never actually been exercised: every run this session logged
`RevenueCat API key missing`, meaning the paywall has never rendered.

## 1. Create the RevenueCat project (free tier is fine to start)

1. Sign up at [app.revenuecat.com](https://app.revenuecat.com).
2. Create a project (e.g. "PropertyMate").
3. Add your Android app: Project settings → Apps → **+ New app** → Google
   Play Store. You'll need the package name (`com.frdsfrhn.property_agent_app`
   per your earlier setup — confirm it matches what `flutter create --org`
   generated).
4. (When ready for iOS) add a second app the same way, App Store, with your
   bundle ID.

## 2. Create the product in Google Play Console first

RevenueCat doesn't create the product — Play Console does, RevenueCat just
reads it. In Play Console → your app → **Monetize → Subscriptions**:
1. Create a subscription (e.g. `propertymate_monthly`), set price and
   billing period.
2. This requires the app to exist in Play Console already (even just as a
   draft/internal-testing entry) — you don't need to be in production for
   this step.

## 3. Wire the product into RevenueCat

Back in RevenueCat, for the Android app:
1. **Products** → import/add the Play Console product you just created.
2. **Entitlements** → create one named exactly **`ai_features`** (must
   match `AppConstants.aiFeaturesEntitlementId` in
   `lib/core/constants.dart` — a typo here means the app will always think
   the entitlement is missing) → attach your product to it.
3. **Offerings** → create one named exactly **`default`** (must match
   `AppConstants.defaultOfferingId`) → mark it "current" → add a package
   pointing at your product. `PaywallScreen` reads
   `Purchases.getOfferings().current`, so if there's no current offering,
   the paywall shows "No plans available right now" instead of erroring —
   easy to mistake for a bug if this step is skipped.

## 4. Wire the webhook (so Firestore's trial/subscription status stays in sync)

RevenueCat → **Integrations → Webhooks** → add a webhook pointing at your
deployed `revenuecatWebhook` Cloud Function URL (printed at the end of
`firebase deploy --only functions`, or find it in the Firebase console
under Functions). Set the `Authorization` header to the same value you
stored in the `REVENUECAT_WEBHOOK_AUTH` secret (`firebase functions:secrets:set
REVENUECAT_WEBHOOK_AUTH` — you set this earlier in the session; if you've
lost track of the value, just set a new one and redeploy, this field being
out of sync is not user-visible until a purchase event actually fires).

## 5. Get your public API key and run the app with it

RevenueCat → **Project settings → API keys** → copy the **public** app-specific
key for Android (a different one exists for iOS later — don't mix them up).

```powershell
flutter run -d emulator-5554 --dart-define=REVENUECAT_API_KEY_ANDROID=goog_xxx
```

The "RevenueCat API key missing" log line should disappear, and navigating
to the paywall (Settings → Upgrade, or automatically once
`AccessLevel.trialExpired`) should load your real offering.

## 6. Add yourself as a license tester (so test purchases don't charge real money)

Play Console → **Setup → License testing** → add your Google account (the
one signed into the emulator/phone you're testing with) as a license
tester. Purchases made while signed in as a license tester show a real
Play Billing purchase flow but aren't charged.

## 7. What to actually verify

- [ ] Paywall loads a real package with a real price (not "No plans
      available")
- [ ] Completing a test purchase closes the paywall and unlocks voice
      notes/AI cleanup immediately (don't need to restart the app)
- [ ] `users/{uid}.subscriptionStatus` in Firestore updates after purchase
      (confirms the webhook fired)
- [ ] "Restore purchases" works after reinstalling the app fresh
- [ ] Trial countdown (`AppConstants.trialLengthDays` = 14) shows correctly
      for a brand-new account, and the paywall correctly appears once it's
      exhausted — the fastest way to test this without waiting 14 days is
      to temporarily lower `trialLengthDays` locally, test, then revert
      before committing
