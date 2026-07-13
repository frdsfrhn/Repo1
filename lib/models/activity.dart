import 'package:cloud_firestore/cloud_firestore.dart';

enum ActivityType { voice, typed }

extension ActivityTypeX on ActivityType {
  String get wireValue => this == ActivityType.voice ? 'voice' : 'typed';

  static ActivityType fromWire(String? value) {
    return value == 'voice' ? ActivityType.voice : ActivityType.typed;
  }
}

enum TranscriptionStatus { none, pending, cleaned, failed }

extension TranscriptionStatusX on TranscriptionStatus {
  String get wireValue {
    switch (this) {
      case TranscriptionStatus.none:
        return 'none';
      case TranscriptionStatus.pending:
        return 'pending';
      case TranscriptionStatus.cleaned:
        return 'cleaned';
      case TranscriptionStatus.failed:
        return 'failed';
    }
  }

  static TranscriptionStatus fromWire(String? value) {
    switch (value) {
      case 'pending':
        return TranscriptionStatus.pending;
      case 'cleaned':
        return TranscriptionStatus.cleaned;
      case 'failed':
        return TranscriptionStatus.failed;
      default:
        return TranscriptionStatus.none;
    }
  }
}

/// A single activity-log entry on a lead: a voice note or a typed note.
///
/// For voice notes, [transcriptOriginal] is the raw speech-to-text output
/// and [transcriptCleaned] is the AI-tidied version — both are kept and
/// viewable per the MVP spec ("transcript (AI-cleaned) + original
/// transcript (kept, viewable)"). The underlying audio file is deleted by
/// a Cloud Function once transcription completes (PDPA data minimization);
/// [audioUrl] is null after that point.
class Activity {
  Activity({
    required this.id,
    required this.leadId,
    required this.agentId,
    required this.type,
    required this.timestamp,
    this.transcriptOriginal = '',
    this.transcriptCleaned = '',
    this.transcriptionStatus = TranscriptionStatus.none,
    this.audioUrl,
    this.audioDeletedAt,
  });

  final String id;
  final String leadId;
  final String agentId;
  final ActivityType type;
  final DateTime timestamp;
  final String transcriptOriginal;
  final String transcriptCleaned;
  final TranscriptionStatus transcriptionStatus;
  final String? audioUrl;
  final DateTime? audioDeletedAt;

  /// What to show as the primary note text — prefer the AI-cleaned
  /// version, fall back to the original if cleanup hasn't happened yet.
  String get displayText =>
      transcriptCleaned.isNotEmpty ? transcriptCleaned : transcriptOriginal;

  factory Activity.fromFirestore(
    DocumentSnapshot<Map<String, dynamic>> doc,
    String leadId,
  ) {
    final data = doc.data()!;
    return Activity(
      id: doc.id,
      leadId: leadId,
      agentId: data['agentId'] as String,
      type: ActivityTypeX.fromWire(data['type'] as String?),
      timestamp:
          (data['timestamp'] as Timestamp?)?.toDate() ?? DateTime.now(),
      transcriptOriginal: data['transcriptOriginal'] as String? ?? '',
      transcriptCleaned: data['transcriptCleaned'] as String? ?? '',
      transcriptionStatus:
          TranscriptionStatusX.fromWire(data['transcriptionStatus'] as String?),
      audioUrl: data['audioUrl'] as String?,
      audioDeletedAt: (data['audioDeletedAt'] as Timestamp?)?.toDate(),
    );
  }

  Map<String, dynamic> toFirestore({bool isCreate = false}) {
    return {
      'agentId': agentId,
      'type': type.wireValue,
      'transcriptOriginal': transcriptOriginal,
      'transcriptCleaned': transcriptCleaned,
      'transcriptionStatus': transcriptionStatus.wireValue,
      'audioUrl': audioUrl,
      'audioDeletedAt':
          audioDeletedAt == null ? null : Timestamp.fromDate(audioDeletedAt!),
      if (isCreate) 'timestamp': FieldValue.serverTimestamp(),
    };
  }
}
