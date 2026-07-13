import 'package:flutter/foundation.dart';

import '../core/constants.dart';
import '../models/app_user.dart';

enum AccessLevel { trialActive, subscribed, trialExpired }

/// Derives paywall/trial state from the agent's profile document. This is
/// UI-facing gating only — the Cloud Functions that actually run AI work
/// re-check entitlement server-side (see functions/src/index.ts), since a
/// client-only gate can't protect the cost driver.
class SubscriptionProvider extends ChangeNotifier {
  AppUser? _profile;

  void updateProfile(AppUser? profile) {
    _profile = profile;
    notifyListeners();
  }

  DateTime? get trialEndsAt => _profile == null
      ? null
      : _profile!.trialStartedAt
          .add(const Duration(days: AppConstants.trialLengthDays));

  int get trialDaysRemaining {
    final endsAt = trialEndsAt;
    if (endsAt == null) return 0;
    final remaining = endsAt.difference(DateTime.now()).inHours / 24;
    return remaining.ceil().clamp(0, AppConstants.trialLengthDays);
  }

  AccessLevel get accessLevel {
    if (_profile == null) return AccessLevel.trialExpired;
    if (_profile!.subscriptionStatus == SubscriptionStatus.active) {
      return AccessLevel.subscribed;
    }
    if (_profile!.subscriptionStatus == SubscriptionStatus.expired) {
      return AccessLevel.trialExpired;
    }
    // subscriptionStatus == trial: check whether the trial window itself
    // has elapsed (the RevenueCat webhook flips this to `expired`
    // eventually, but the local trial clock is the immediate source of
    // truth so the UI doesn't wait on a webhook round trip).
    final endsAt = trialEndsAt;
    if (endsAt != null && DateTime.now().isAfter(endsAt)) {
      return AccessLevel.trialExpired;
    }
    return AccessLevel.trialActive;
  }

  /// AI features (voice transcription + cleanup) are gated on top of core
  /// CRUD, which stays available regardless of subscription state — a
  /// lapsed agent can still see and edit their leads, just not use AI.
  bool get canUseAiFeatures => accessLevel != AccessLevel.trialExpired;

  bool get shouldShowPaywall => accessLevel == AccessLevel.trialExpired;
}
