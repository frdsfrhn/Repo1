import 'package:cloud_firestore/cloud_firestore.dart';

enum SubscriptionStatus { trial, active, expired }

extension SubscriptionStatusX on SubscriptionStatus {
  String get wireValue {
    switch (this) {
      case SubscriptionStatus.trial:
        return 'trial';
      case SubscriptionStatus.active:
        return 'active';
      case SubscriptionStatus.expired:
        return 'expired';
    }
  }

  static SubscriptionStatus fromWire(String? value) {
    switch (value) {
      case 'active':
        return SubscriptionStatus.active;
      case 'expired':
        return SubscriptionStatus.expired;
      default:
        return SubscriptionStatus.trial;
    }
  }
}

/// The signed-in agent's profile document at /users/{uid}.
///
/// [pdpaConsentAt] gates the app: an agent cannot use the dashboard until
/// they've accepted the first-login consent screen (see
/// screens/auth/consent_screen.dart), satisfying the explicit-consent
/// requirement in the MVP checklist.
class AppUser {
  AppUser({
    required this.uid,
    required this.agentId,
    required this.phoneNumber,
    this.pdpaConsentAt,
    required this.createdAt,
    required this.trialStartedAt,
    this.subscriptionStatus = SubscriptionStatus.trial,
  });

  final String uid;
  final String agentId;
  final String phoneNumber;
  final DateTime? pdpaConsentAt;
  final DateTime createdAt;
  final DateTime trialStartedAt;
  final SubscriptionStatus subscriptionStatus;

  bool get hasGivenConsent => pdpaConsentAt != null;

  factory AppUser.fromFirestore(DocumentSnapshot<Map<String, dynamic>> doc) {
    final data = doc.data()!;
    return AppUser(
      uid: doc.id,
      agentId: data['agentId'] as String? ?? doc.id,
      phoneNumber: data['phoneNumber'] as String? ?? '',
      pdpaConsentAt: (data['pdpaConsentAt'] as Timestamp?)?.toDate(),
      createdAt: (data['createdAt'] as Timestamp?)?.toDate() ?? DateTime.now(),
      trialStartedAt:
          (data['trialStartedAt'] as Timestamp?)?.toDate() ?? DateTime.now(),
      subscriptionStatus:
          SubscriptionStatusX.fromWire(data['subscriptionStatus'] as String?),
    );
  }

  Map<String, dynamic> toFirestoreCreate() {
    return {
      'agentId': uid,
      'phoneNumber': phoneNumber,
      'pdpaConsentAt': null,
      'createdAt': FieldValue.serverTimestamp(),
      'trialStartedAt': FieldValue.serverTimestamp(),
      'subscriptionStatus': SubscriptionStatus.trial.wireValue,
    };
  }
}
