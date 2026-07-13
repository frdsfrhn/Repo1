import 'dart:io';

import 'package:firebase_storage/firebase_storage.dart';

/// Uploads and deletes voice-note recordings in Cloud Storage.
///
/// Storage layout: voice_recordings/{agentId}/{leadId}/{activityId}.wav
/// — see storage.rules for the matching access-control path.
class StorageService {
  StorageService({FirebaseStorage? storage})
      : _storage = storage ?? FirebaseStorage.instance;

  final FirebaseStorage _storage;

  Reference _recordingRef({
    required String agentId,
    required String leadId,
    required String activityId,
  }) {
    return _storage
        .ref()
        .child('voice_recordings')
        .child(agentId)
        .child(leadId)
        .child('$activityId.wav');
  }

  /// Uploads a local recording and returns its gs:// storage path (not a
  /// download URL — the app never needs a public URL, only the Cloud
  /// Function that transcribes it, which reads via the Admin SDK).
  Future<String> uploadRecording({
    required File file,
    required String agentId,
    required String leadId,
    required String activityId,
  }) async {
    final ref = _recordingRef(
      agentId: agentId,
      leadId: leadId,
      activityId: activityId,
    );
    await ref.putFile(file, SettableMetadata(contentType: 'audio/wav'));
    return 'gs://${ref.bucket}/${ref.fullPath}';
  }

  Future<void> deleteByGsUrl(String gsUrl) async {
    try {
      final ref = _storage.refFromURL(gsUrl);
      await ref.delete();
    } on FirebaseException catch (e) {
      // object-not-found just means it was already cleaned up — ignore.
      if (e.code != 'object-not-found') rethrow;
    }
  }
}
