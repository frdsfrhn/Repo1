import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/activity.dart';
import '../services/firestore_service.dart';
import '../services/storage_service.dart';
import '../services/transcription_service.dart';

/// Scoped to a single lead-detail screen: streams that lead's activity log
/// and handles adding typed notes / voice notes / deletions.
class ActivityProvider extends ChangeNotifier {
  ActivityProvider({
    required FirestoreService firestoreService,
    required StorageService storageService,
    required TranscriptionService transcriptionService,
    required this.leadId,
    required this.agentId,
  })  : _firestoreService = firestoreService,
        _storageService = storageService,
        _transcriptionService = transcriptionService {
    _sub = _firestoreService.watchActivities(leadId).listen((activities) {
      _activities = activities;
      _isLoading = false;
      notifyListeners();
    });
  }

  final FirestoreService _firestoreService;
  final StorageService _storageService;
  final TranscriptionService _transcriptionService;
  final String leadId;
  final String agentId;

  StreamSubscription<List<Activity>>? _sub;
  List<Activity> _activities = [];
  bool _isLoading = true;

  List<Activity> get activities => _activities;
  bool get isLoading => _isLoading;

  Future<void> addTypedNote(String text) async {
    final activity = Activity(
      id: '',
      leadId: leadId,
      agentId: agentId,
      type: ActivityType.typed,
      timestamp: DateTime.now(),
      transcriptOriginal: text,
      transcriptCleaned: text,
    );
    await _firestoreService.addActivity(activity);
  }

  /// Creates the activity doc immediately (typing indicator equivalent),
  /// then kicks off server-side transcription + AI cleanup. The activity
  /// stream above reflects transcriptionStatus changes as the Cloud
  /// Function progresses, so the UI updates itself without polling.
  Future<void> addVoiceNote({
    required String storagePath,
  }) async {
    final activity = Activity(
      id: '',
      leadId: leadId,
      agentId: agentId,
      type: ActivityType.voice,
      timestamp: DateTime.now(),
      transcriptionStatus: TranscriptionStatus.pending,
      audioUrl: storagePath,
    );
    final activityId = await _firestoreService.addActivity(activity);
    await _transcriptionService.processVoiceNote(
      leadId: leadId,
      activityId: activityId,
    );
  }

  Future<void> retryCleanup(String activityId) {
    return _transcriptionService.retryCleanup(
      leadId: leadId,
      activityId: activityId,
    );
  }

  Future<void> deleteActivity(Activity activity) {
    return _firestoreService.deleteActivity(
      leadId: leadId,
      activityId: activity.id,
      audioUrl: activity.audioUrl,
      deleteAudio: _storageService.deleteByGsUrl,
    );
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}
