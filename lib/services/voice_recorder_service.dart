import 'dart:io';

import 'package:path_provider/path_provider.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:record/record.dart';

class MicPermissionDeniedException implements Exception {
  const MicPermissionDeniedException();
}

/// Thin wrapper over the `record` package for starting/stopping a single
/// voice-note recording. The microphone permission prompt is triggered
/// from here so the app-level usage string (see ios/android manifest
/// setup in README) is shown at first request, per the App Store
/// requirement in the MVP checklist.
class VoiceRecorderService {
  final AudioRecorder _recorder = AudioRecorder();
  String? _currentPath;

  bool get isRecording => _currentPath != null;

  Future<void> startRecording() async {
    final status = await Permission.microphone.request();
    if (!status.isGranted) {
      throw const MicPermissionDeniedException();
    }
    final dir = await getTemporaryDirectory();
    final path =
        '${dir.path}/voice_note_${DateTime.now().microsecondsSinceEpoch}.wav';
    // Uncompressed 16kHz mono LINEAR16 — the format Google Cloud
    // Speech-to-Text wants directly (see functions/src/transcribeAudio.ts),
    // so no server-side transcoding step is needed.
    await _recorder.start(
      const RecordConfig(
        encoder: AudioEncoder.wav,
        sampleRate: 16000,
        numChannels: 1,
      ),
      path: path,
    );
    _currentPath = path;
  }

  /// Stops recording and returns the local file, or null if nothing was
  /// captured (e.g. stopped within a fraction of a second of starting).
  Future<File?> stopRecording() async {
    final path = await _recorder.stop();
    _currentPath = null;
    if (path == null) return null;
    final file = File(path);
    if (!await file.exists() || await file.length() == 0) return null;
    return file;
  }

  Future<void> cancelRecording() async {
    await _recorder.stop();
    if (_currentPath != null) {
      final file = File(_currentPath!);
      if (await file.exists()) await file.delete();
    }
    _currentPath = null;
  }

  Future<void> dispose() async {
    await _recorder.dispose();
  }
}
