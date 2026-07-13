import 'package:cloud_functions/cloud_functions.dart';

/// Triggers the server-side voice-note pipeline: speech-to-text, then AI
/// transcript cleanup, then deletion of the raw audio (PDPA data
/// minimization). All of this happens in the `processVoiceNote` Cloud
/// Function — see functions/src/transcribeAudio.ts and
/// functions/src/cleanupTranscript.ts.
///
/// The callable also re-checks the RevenueCat entitlement server-side
/// (functions/src/index.ts) before doing any paid work, since a client
/// gate alone can't be trusted to protect the AI cost driver.
class TranscriptionService {
  TranscriptionService({FirebaseFunctions? functions})
      : _functions = functions ?? FirebaseFunctions.instance;

  final FirebaseFunctions _functions;

  /// Fire-and-forget from the caller's perspective: the activity document's
  /// `transcriptionStatus` field is what the UI actually watches (via
  /// FirestoreService.watchActivities) to reflect pending → cleaned/failed.
  /// This call still awaits the function so callers can surface an
  /// immediate paywall/error message if it's rejected outright.
  Future<void> processVoiceNote({
    required String leadId,
    required String activityId,
  }) async {
    final callable = _functions.httpsCallable(
      'processVoiceNote',
      options: HttpsCallableOptions(timeout: const Duration(minutes: 3)),
    );
    await callable.call<void>({
      'leadId': leadId,
      'activityId': activityId,
    });
  }

  /// Re-runs AI cleanup only, against the existing original transcript —
  /// used by the inline "clean up again" action if a cleanup attempt
  /// failed without needing to re-record.
  Future<void> retryCleanup({
    required String leadId,
    required String activityId,
  }) async {
    final callable = _functions.httpsCallable(
      'retryTranscriptCleanup',
      options: HttpsCallableOptions(timeout: const Duration(minutes: 1)),
    );
    await callable.call<void>({
      'leadId': leadId,
      'activityId': activityId,
    });
  }
}
