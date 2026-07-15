import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../providers/lead_provider.dart';
import '../../widgets/app_empty_state.dart';
import '../lead/lead_detail_screen.dart';

/// "What's on my plate" — every follow-up/viewing/move-in date across all
/// leads, bucketed into Overdue / Today / Tomorrow / This week / Later.
/// Client-side only, over [LeadProvider.agendaEntries] — no new Firestore
/// reads or push notifications, per the agreed scope (see docs/BACKLOG.md).
class AgendaScreen extends StatelessWidget {
  const AgendaScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final entries = context.watch<LeadProvider>().agendaEntries;

    return Scaffold(
      appBar: AppBar(title: const Text('Agenda')),
      body: entries.isEmpty
          ? const AppEmptyState(
              icon: Icons.event_available,
              title: 'Nothing scheduled',
              message: 'Follow-ups, viewings, and move-in dates you set on '
                  'leads will show up here.',
            )
          : _AgendaList(entries: entries),
    );
  }
}

class _AgendaList extends StatelessWidget {
  const _AgendaList({required this.entries});

  final List<AgendaEntry> entries;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final tomorrow = today.add(const Duration(days: 1));
    final weekEnd = today.add(const Duration(days: 7));

    final overdue = <AgendaEntry>[];
    final todayEntries = <AgendaEntry>[];
    final tomorrowEntries = <AgendaEntry>[];
    final thisWeek = <AgendaEntry>[];
    final later = <AgendaEntry>[];

    for (final entry in entries) {
      final day = DateTime(entry.date.year, entry.date.month, entry.date.day);
      if (day.isBefore(today)) {
        overdue.add(entry);
      } else if (day == today) {
        todayEntries.add(entry);
      } else if (day == tomorrow) {
        tomorrowEntries.add(entry);
      } else if (day.isBefore(weekEnd)) {
        thisWeek.add(entry);
      } else {
        later.add(entry);
      }
    }

    final sections = <_AgendaSection>[
      if (overdue.isNotEmpty) _AgendaSection('Overdue', overdue, isOverdue: true),
      if (todayEntries.isNotEmpty) _AgendaSection('Today', todayEntries),
      if (tomorrowEntries.isNotEmpty) _AgendaSection('Tomorrow', tomorrowEntries),
      if (thisWeek.isNotEmpty) _AgendaSection('This week', thisWeek),
      if (later.isNotEmpty) _AgendaSection('Later', later),
    ];

    return ListView.builder(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 32),
      itemCount: sections.length,
      itemBuilder: (context, index) => _AgendaSectionView(section: sections[index]),
    );
  }
}

class _AgendaSection {
  const _AgendaSection(this.title, this.entries, {this.isOverdue = false});

  final String title;
  final List<AgendaEntry> entries;
  final bool isOverdue;
}

class _AgendaSectionView extends StatelessWidget {
  const _AgendaSectionView({required this.section});

  final _AgendaSection section;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 8, top: 4),
          child: Text(
            section.title,
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: section.isOverdue ? AppTheme.danger : null,
                  fontWeight: FontWeight.w700,
                ),
          ),
        ),
        for (final entry in section.entries) ...[
          _AgendaTile(entry: entry),
          const SizedBox(height: 8),
        ],
        const SizedBox(height: 12),
      ],
    );
  }
}

class _AgendaTile extends StatelessWidget {
  const _AgendaTile({required this.entry});

  final AgendaEntry entry;

  @override
  Widget build(BuildContext context) {
    final typeColor = switch (entry.type) {
      AgendaType.followUp => AppTheme.warning,
      AgendaType.viewing => AppTheme.primary,
      AgendaType.moveIn => AppTheme.accent,
    };

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => LeadDetailScreen(leadId: entry.lead.id),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: typeColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  entry.type.label,
                  style: TextStyle(
                    color: typeColor,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      entry.lead.prospectName.isEmpty
                          ? '(No name)'
                          : entry.lead.prospectName,
                      style: Theme.of(context).textTheme.titleSmall,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      entry.lead.propertyName.isEmpty
                          ? '(No property)'
                          : entry.lead.propertyName,
                      style: Theme.of(context)
                          .textTheme
                          .bodySmall
                          ?.copyWith(color: Colors.grey.shade600),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Text(
                Formatters.date(entry.date),
                style: Theme.of(context)
                    .textTheme
                    .labelMedium
                    ?.copyWith(color: Colors.grey.shade600),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
