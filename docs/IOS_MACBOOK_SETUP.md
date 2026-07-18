# Setting up a fresh MacBook for iOS builds

Mirrors the Windows/Android setup you already went through, for macOS +
Xcode. `ios/` isn't in this repo (same reason `android/` wasn't — it's
machine-generated, safer to regenerate locally than hand-write/commit), so
this Mac needs to produce its own copy.

## 1. Xcode (do this first — it's the slow part)

1. Install **Xcode** from the Mac App Store — multi-GB download, start it
   before anything else so it downloads in the background.
2. Once installed, open it once to accept the license and let it install
   additional components.
3. `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer`
4. `sudo xcodebuild -license accept`

## 2. Homebrew, Flutter, CocoaPods

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
brew install --cask flutter
brew install cocoapods
flutter doctor
```

Resolve whatever `flutter doctor` flags (it'll likely want you to accept
Android licenses too if you install Android Studio on this Mac for
cross-checking — optional, only needed if you also want to build Android
from the Mac).

## 3. Clone the repo and get the branch

```bash
git clone https://github.com/frdsfrhn/Repo1.git
cd Repo1
git checkout claude/flutter-firebase-mobile-app-h7fh9p
flutter pub get
```

## 4. Generate the iOS platform folder

```bash
flutter create --org com.frdsfrhn --project-name property_agent_app .
```

Same caution as the Android side: answer **no** if it prompts to overwrite
`lib/`, `pubspec.yaml`, or this repo's docs — only let it create the
`ios/` folder. Use the **same org identifier** you used for
`flutter create` on Windows (`com.frdsfrhn` — confirm by checking
`android/app/build.gradle`'s `applicationId` on the Windows machine if
unsure), so the bundle ID lines up with what you'll register in Firebase
and App Store Connect.

## 5. Firebase: register the iOS app

```bash
npm install -g firebase-tools
dart pub global activate flutterfire_cli
firebase login
flutterfire configure
```

Select the **same Firebase project** you already created
(`property-agent-app-309df`), and when prompted for platforms, add **iOS**
this time (Android's already registered from before). This updates
`lib/firebase_options.dart` to include iOS config and drops
`GoogleService-Info.plist` into `ios/Runner/`.

## 6. iOS-specific Info.plist entries

**Microphone** (same requirement as Android, different mechanism — add to
`ios/Runner/Info.plist`):
```xml
<key>NSMicrophoneUsageDescription</key>
<string>We need your microphone to record voice notes about your leads, which are transcribed into text.</string>
```

**Photo library** — iOS requires this even though Android's system photo
picker didn't need a manifest change; `image_picker`'s gallery picker will
crash on iOS without it:
```xml
<key>NSPhotoLibraryUsageDescription</key>
<string>Attach a photo of the property one-pager or flyer to a lead.</string>
```

**Phone auth URL scheme** — required for the phone-number sign-in screen;
without it, Firebase Auth crashes the app immediately when you request an
SMS code (it needs this to complete its reCAPTCHA verification redirect).
This project doesn't use Google Sign-In, so `GoogleService-Info.plist` has
no `REVERSED_CLIENT_ID` key — use the app's own bundle identifier as the
URL scheme instead (this is what Firebase Auth falls back to). Add to
`ios/Runner/Info.plist`:
```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>com.frdsfrhn.propertyAgentApp</string>
    </array>
  </dict>
</array>
```
(If `GoogleService-Info.plist` *does* have a `REVERSED_CLIENT_ID` — e.g.
if Google Sign-In gets added later — add a second `<dict>` entry in the
array with that value too; harmless to have both.)

## 7. Apple Developer account

- A **free Apple ID** lets you build and run on your own physical iPhone
  via Xcode for local testing (7-day signing certificate, needs
  re-signing weekly — fine for development, not for real distribution).
- A **paid Apple Developer Program membership** ($99/year) is required for:
  - TestFlight (the iOS equivalent of Play Console's Internal Testing)
  - Submitting to the App Store
  - Push notifications, if ever added (not currently used)
  - Longer-lived signing (no weekly re-sign)

  Enroll at [developer.apple.com/programs](https://developer.apple.com/programs/)
  if you don't already have this — identity verification can take a day
  or two, same caution as the Play Console account.

## 8. Signing in Xcode

```bash
open ios/Runner.xcworkspace
```
(Open the `.xcworkspace`, not `.xcodeproj` — CocoaPods requires this.)

In Xcode: select the `Runner` target → **Signing & Capabilities** → pick
your Apple ID/Team → let Xcode auto-manage the provisioning profile.

## 9. App icon for iOS

```bash
dart run flutter_launcher_icons
```
Same command as Android — it already targets both platforms (see
`flutter_launcher_icons:` config in `pubspec.yaml`, `ios: true` is already
set), so this single run regenerates both.

## 10. RevenueCat: add the iOS app

If you haven't already done the Android RevenueCat setup, see
`docs/REVENUECAT_SETUP.md` first — same project, just add a second app
(App Store platform, your iOS bundle ID) alongside the Android one, and
attach the same `ai_features` entitlement to an iOS product once you've
created one in App Store Connect.

## 11. Run it

Simulator (fastest first check, no signing needed):
```bash
open -a Simulator
flutter run --dart-define=REVENUECAT_API_KEY_IOS=appl_xxx
```

Real iPhone (USB, with the phone unlocked and "Trust this computer"
accepted):
```bash
flutter devices
flutter run -d <device-id> --dart-define=REVENUECAT_API_KEY_IOS=appl_xxx
```

## 12. Before TestFlight/App Store submission

- [ ] `flutter build ipa` produces a signed archive
      (`build/ios/archive/Runner.xcarchive` / the `.ipa` under
      `build/ios/ipa/`)
- [ ] Upload via Xcode Organizer or `xcrun altool` / Transporter app
- [ ] App Store Connect: create the app record, matching bundle ID
- [ ] Same privacy policy URL, support email, and Data Safety-equivalent
      (Apple calls it "App Privacy" / nutrition labels) as the Play Store
      side — see `docs/PLAY_STORE_SUBMISSION.md` for the data being
      collected, the answers are the same on both stores
- [ ] TestFlight internal testing (equivalent urgency to Android's
      Internal Testing track — no 14-day wait, unlike Play's *closed*
      testing requirement) before wider distribution
