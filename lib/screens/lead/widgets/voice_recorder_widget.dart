import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:uuid/uuid.dart';

import '../../../core/theme.dart';
import '../../../providers/activity_provider.dart';
import '../../../providers/subscription_provider.dart';
import '../../../services/storage_service.dart';
import '../../../services/voice_recorder_service.dart';
import '../../subscription/paywall_screen.dart';

/// Inline voice-note capture control for the lead-detail screen. Gated on
/// the AI-features entitlement, since transcription + cleanup are the
/// app's real cost driver (see MVP checklist §6).
class VoiceRecorderWidget extends StatefulWidget {
  const VoiceRecorderWidget({super.key});

  @override
  State<VoiceRecorderWidget> createState() => _VoiceRecorderWidgetState();
}

class _VoiceRecorderWidgetState extends State<VoiceRecorderWidget> {
  final _recorder = VoiceRecorderService();
  final _storage = StorageService();
  final _uuid = const Uuid();

  bool _isRecording = false;
  bool _isUploading = false;
  Duration _elapsed = Duration.zero;
  Timer? _timer;
  String? _error;

  @override
  void dispose() {
    _timer?.cancel();
    _recorder.dispose();
    super.dispose();
  }

  Future<void> _start() async {
    setState(() => _error = null);
    try {
      await _recorder.startRecording();
      setState(() {
        _isRecording = true;
        _elapsed = Duration.zero;
      });
      _timer = Timer.periodic(const Duration(seconds: 1), (_) {
        setState(() => _elapsed += const Duration(seconds: 1));
      });
    } on MicPermissionDeniedException {
      setState(() =>
          _error = 'Microphone access is needed to record voice notes. '
              'Enable it in system settings.');
    } catch (e) {
      setState(() => _error = 'Could not start recording: $e');
    }
  }

  Future<void> _stopAndUpload() async {
    _timer?.cancel();
    setState(() {
      _isRecording = false;
      _isUploading = true;
    });

    final file = await _recorder.stopRecording();
    if (file == null) {
      setState(() {
        _isUploading = false;
        _error = 'Recording was too short — nothing was saved.';
      });
      return;
    }

    final activityProvider = context.read<ActivityProvider>();
    final activityId = _uuid.v4();

    try {
      final storagePath = await _storage.uploadRecording(
        file: file,
        agentId: activityProvider.agentId,
        leadId: activityProvider.leadId,
        activityId: activityId,
      );
      await activityProvider.addVoiceNote(storagePath: storagePath);
    } catch (e) {
      if (mounted) setState(() => _error = 'Upload failed: $e');
    } finally {
      if (mounted) setState(() => _isUploading = false);
    }
  }

  Future<void> _cancel() async {
    _timer?.cancel();
    await _recorder.cancelRecording();
    setState(() => _isRecording = false);
  }

  String _formatDuration(Duration d) {
    final minutes = d.inMinutes.toString().padLeft(2, '0');
    final seconds = (d.inSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }

  @override
  Widget build(BuildContext context) {
    final canUseAi = context.watch<SubscriptionProvider>().canUseAiFeatures;

    if (!canUseAi) {
      return OutlinedButton.icon(
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const PaywallScreen()),
        ),
        icon: const Icon(Icons.lock_outline),
        label: const Text('Subscribe to record voice notes'),
      );
    }

    if (_isUploading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 8),
        child: Row(
          children: [
            SizedBox(
              width: 18,
              height: 18,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
            SizedBox(width: 12),
            Text('Uploading and transcribing…'),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            if (!_isRecording)
              FilledButton.icon(
                onPressed: _start,
                icon: const Icon(Icons.mic),
                label: const Text('Record voice note'),
              )
            else ...[
              Container(
                width: 12,
                height: 12,
                decoration: const BoxDecoration(
                  color: AppTheme.danger,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 8),
              Text(_formatDuration(_elapsed)),
              const Spacer(),
              IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Discard',
                onPressed: _cancel,
              ),
              FilledButton.icon(
                onPressed: _stopAndUpload,
                icon: const Icon(Icons.stop),
                label: const Text('Stop & save'),
              ),
            ],
          ],
        ),
        if (_error != null) ...[
          const SizedBox(height: 6),
          Text(_error!, style: const TextStyle(color: AppTheme.danger)),
        ],
      ],
    );
  }
}
