import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/formatters.dart';
import '../../../models/activity.dart';
import '../../../providers/activity_provider.dart';
import '../../../widgets/app_empty_state.dart';

class ActivityLogList extends StatelessWidget {
  const ActivityLogList({super.key});

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<ActivityProvider>();

    if (provider.isLoading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (provider.activities.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: AppEmptyState(
          icon: Icons.forum_outlined,
          title: 'No activity yet',
          message: 'Voice and typed notes about this lead will show up here.',
        ),
      );
    }

    return Column(
      children: provider.activities
          .map((a) => _ActivityCard(activity: a))
          .toList(growable: false),
    );
  }
}

class _ActivityCard extends StatefulWidget {
  const _ActivityCard({required this.activity});

  final Activity activity;

  @override
  State<_ActivityCard> createState() => _ActivityCardState();
}

class _ActivityCardState extends State<_ActivityCard> {
  bool _showOriginal = false;

  @override
  Widget build(BuildContext context) {
    final activity = widget.activity;
    final isVoice = activity.type == ActivityType.voice;

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  isVoice ? Icons.mic : Icons.edit_note,
                  size: 18,
                  color: Colors.grey.shade600,
                ),
                const SizedBox(width: 6),
                Text(
                  Formatters.dateTime(activity.timestamp),
                  style: Theme.of(context)
                      .textTheme
                      .labelMedium
                      ?.copyWith(color: Colors.grey.shade600),
                ),
                const Spacer(),
                PopupMenuButton<String>(
                  onSelected: (value) async {
                    final provider = context.read<ActivityProvider>();
                    if (value == 'retry') {
                      await provider.retryCleanup(activity.id);
                    } else if (value == 'delete') {
                      final confirmed = await showDialog<bool>(
                        context: context,
                        builder: (ctx) => AlertDialog(
                          title: const Text('Delete this note?'),
                          content: const Text(
                              'This permanently deletes the note and any recording. This cannot be undone.'),
                          actions: [
                            TextButton(
                              onPressed: () => Navigator.pop(ctx, false),
                              child: const Text('Cancel'),
                            ),
                            FilledButton(
                              onPressed: () => Navigator.pop(ctx, true),
                              child: const Text('Delete'),
                            ),
                          ],
                        ),
                      );
                      if (confirmed == true) {
                        await provider.deleteActivity(activity);
                      }
                    }
                  },
                  itemBuilder: (context) => [
                    if (isVoice &&
                        activity.transcriptionStatus ==
                            TranscriptionStatus.failed)
                      const PopupMenuItem(
                        value: 'retry',
                        child: Text('Retry AI cleanup'),
                      ),
                    const PopupMenuItem(value: 'delete', child: Text('Delete')),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 8),
            if (isVoice &&
                activity.transcriptionStatus == TranscriptionStatus.pending)
              Row(
                children: [
                  const SizedBox(
                    width: 14,
                    height: 14,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    'Transcribing…',
                    style: TextStyle(color: Colors.grey.shade600),
                  ),
                ],
              )
            else if (isVoice &&
                activity.transcriptionStatus == TranscriptionStatus.failed)
              Text(
                'Transcription failed. Try "Retry AI cleanup" from the menu above.',
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              )
            else
              Text(
                _showOriginal
                    ? (activity.transcriptOriginal.isEmpty
                        ? '(No transcript)'
                        : activity.transcriptOriginal)
                    : (activity.displayText.isEmpty
                        ? '(No content)'
                        : activity.displayText),
              ),
            if (isVoice &&
                activity.transcriptCleaned.isNotEmpty &&
                activity.transcriptOriginal.isNotEmpty &&
                activity.transcriptOriginal != activity.transcriptCleaned)
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton(
                  onPressed: () =>
                      setState(() => _showOriginal = !_showOriginal),
                  child: Text(_showOriginal
                      ? 'Show AI-cleaned version'
                      : 'Show original transcript'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
