import 'package:cloud_firestore/cloud_firestore.dart';

enum DealType { rent, purchase }

enum LeadStatus { open, followUpNeeded, closed }

extension DealTypeX on DealType {
  String get wireValue => this == DealType.rent ? 'rent' : 'purchase';

  String get label => this == DealType.rent ? 'Rent' : 'Purchase';

  static DealType fromWire(String? value) {
    return value == 'purchase' ? DealType.purchase : DealType.rent;
  }
}

extension LeadStatusX on LeadStatus {
  String get wireValue {
    switch (this) {
      case LeadStatus.open:
        return 'open';
      case LeadStatus.followUpNeeded:
        return 'follow_up_needed';
      case LeadStatus.closed:
        return 'closed';
    }
  }

  String get label {
    switch (this) {
      case LeadStatus.open:
        return 'Open';
      case LeadStatus.followUpNeeded:
        return 'Follow-up needed';
      case LeadStatus.closed:
        return 'Closed';
    }
  }

  static LeadStatus fromWire(String? value) {
    switch (value) {
      case 'follow_up_needed':
        return LeadStatus.followUpNeeded;
      case 'closed':
        return LeadStatus.closed;
      default:
        return LeadStatus.open;
    }
  }
}

/// The locked v1 lead record.
///
/// `dealValue` (property price for a purchase, or annual/monthly rental
/// value for a rent deal — decide the convention with agents) isn't named
/// explicitly in the MVP checklist, but "commission: percentage + computed
/// value" requires a base amount to compute against, so it's included here.
class Lead {
  Lead({
    required this.id,
    required this.agentId,
    required this.prospectName,
    required this.phoneNumber,
    required this.propertyName,
    this.propertyAddress = '',
    this.telegramUrl = '',
    this.propertyImageUrl = '',
    this.ownerName = '',
    this.ownerPhone = '',
    required this.dealType,
    required this.commissionPercent,
    required this.dealValue,
    this.expectedYieldPercent,
    required this.nextFollowUpDate,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  final String id;
  final String agentId;
  final String prospectName;
  final String phoneNumber;
  final String propertyName;
  final String propertyAddress;

  /// Full URL an agent pastes in (e.g. `https://t.me/someusername`), opened
  /// directly via url_launcher — not validated beyond being non-empty, since
  /// Telegram usernames/invite links come in several valid URL shapes.
  final String telegramUrl;

  /// Download URL for the property's one-pager/flyer photo, if the agent
  /// has attached one — see StorageService.uploadPropertyImage.
  final String propertyImageUrl;

  /// Property owner/landlord's contact — the other party in the deal, as
  /// opposed to [prospectName]/[phoneNumber] (the client the agent is
  /// representing). Not in the original locked data model; added as an
  /// optional pair since agents need to reach owners directly too.
  final String ownerName;
  final String ownerPhone;

  final DealType dealType;

  /// Commission percentage, e.g. 3.0 for 3%.
  final double commissionPercent;

  /// Property price (purchase) or rental value (rent) commission is computed on.
  final double dealValue;

  /// Purchase leads only — rental yield percentage the agent is tracking.
  final double? expectedYieldPercent;

  final DateTime? nextFollowUpDate;
  final LeadStatus status;
  final DateTime createdAt;
  final DateTime updatedAt;

  /// Simple computed commission — no split/tier/co-broker structure in v1.
  double get commissionValue => dealValue * (commissionPercent / 100);

  factory Lead.fromFirestore(DocumentSnapshot<Map<String, dynamic>> doc) {
    final data = doc.data()!;
    return Lead(
      id: doc.id,
      agentId: data['agentId'] as String,
      prospectName: data['prospectName'] as String? ?? '',
      phoneNumber: data['phoneNumber'] as String? ?? '',
      propertyName: data['propertyName'] as String? ?? '',
      propertyAddress: data['propertyAddress'] as String? ?? '',
      telegramUrl: data['telegramUrl'] as String? ?? '',
      propertyImageUrl: data['propertyImageUrl'] as String? ?? '',
      ownerName: data['ownerName'] as String? ?? '',
      ownerPhone: data['ownerPhone'] as String? ?? '',
      dealType: DealTypeX.fromWire(data['dealType'] as String?),
      commissionPercent: (data['commissionPercent'] as num?)?.toDouble() ?? 0,
      dealValue: (data['dealValue'] as num?)?.toDouble() ?? 0,
      expectedYieldPercent: (data['expectedYieldPercent'] as num?)?.toDouble(),
      nextFollowUpDate: (data['nextFollowUpDate'] as Timestamp?)?.toDate(),
      status: LeadStatusX.fromWire(data['status'] as String?),
      createdAt: (data['createdAt'] as Timestamp?)?.toDate() ?? DateTime.now(),
      updatedAt: (data['updatedAt'] as Timestamp?)?.toDate() ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toFirestore({bool isCreate = false}) {
    return {
      'agentId': agentId,
      'prospectName': prospectName,
      'phoneNumber': phoneNumber,
      'propertyName': propertyName,
      'propertyAddress': propertyAddress,
      'telegramUrl': telegramUrl,
      'propertyImageUrl': propertyImageUrl,
      'ownerName': ownerName,
      'ownerPhone': ownerPhone,
      'dealType': dealType.wireValue,
      'commissionPercent': commissionPercent,
      'dealValue': dealValue,
      'expectedYieldPercent':
          dealType == DealType.purchase ? expectedYieldPercent : null,
      'nextFollowUpDate': nextFollowUpDate == null
          ? null
          : Timestamp.fromDate(nextFollowUpDate!),
      'status': status.wireValue,
      'updatedAt': FieldValue.serverTimestamp(),
      if (isCreate) 'createdAt': FieldValue.serverTimestamp(),
    };
  }

  Lead copyWith({
    String? prospectName,
    String? phoneNumber,
    String? propertyName,
    String? propertyAddress,
    String? telegramUrl,
    String? propertyImageUrl,
    bool clearPropertyImage = false,
    String? ownerName,
    String? ownerPhone,
    DealType? dealType,
    double? commissionPercent,
    double? dealValue,
    double? expectedYieldPercent,
    bool clearExpectedYield = false,
    DateTime? nextFollowUpDate,
    LeadStatus? status,
  }) {
    return Lead(
      id: id,
      agentId: agentId,
      prospectName: prospectName ?? this.prospectName,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      propertyName: propertyName ?? this.propertyName,
      propertyAddress: propertyAddress ?? this.propertyAddress,
      telegramUrl: telegramUrl ?? this.telegramUrl,
      propertyImageUrl: clearPropertyImage
          ? ''
          : (propertyImageUrl ?? this.propertyImageUrl),
      ownerName: ownerName ?? this.ownerName,
      ownerPhone: ownerPhone ?? this.ownerPhone,
      dealType: dealType ?? this.dealType,
      commissionPercent: commissionPercent ?? this.commissionPercent,
      dealValue: dealValue ?? this.dealValue,
      expectedYieldPercent: clearExpectedYield
          ? null
          : (expectedYieldPercent ?? this.expectedYieldPercent),
      nextFollowUpDate: nextFollowUpDate ?? this.nextFollowUpDate,
      status: status ?? this.status,
      createdAt: createdAt,
      updatedAt: updatedAt,
    );
  }
}
