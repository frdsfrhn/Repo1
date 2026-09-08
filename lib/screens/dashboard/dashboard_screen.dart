import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:showcaseview/showcaseview.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../models/lead.dart';
import '../../providers/auth_provider.dart';
import '../../providers/lead_provider.dart';
import '../../widgets/app_empty_state.dart';
import '../agenda/agenda_screen.dart';
import '../lead/lead_detail_screen.dart';
import '../lead/lead_form_screen.dart';
import '../settings/settings_screen.dart';
import '../subscription/widgets/trial_banner.dart';
import 'widgets/lead_list_tile.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final _searchController = TextEditingController();
  final _newLeadShowcaseKey = GlobalKey();
  final _agendaShowcaseKey = GlobalKey();

  @override
  void initState() {
    super.initState();
    ShowcaseView.register(
      onComplete: (index, key) {
        if (key == _agendaShowcaseKey) {
          context.read<AppAuthProvider>().markOnboardingSeen();
        }
      },
      // A tester who dismisses early (taps the barrier) shouldn't get
      // nagged with the same tour again on next launch.
      onDismiss: (key) => context.read<AppAuthProvider>().markOnboardingSeen(),
    );
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final profile = context.read<AppAuthProvider>().profile;
      if (profile != null && !profile.hasSeenOnboarding) {
        ShowcaseView.get()
            .startShowCase([_newLeadShowcaseKey, _agendaShowcaseKey]);
      }
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    ShowcaseView.get().unregister();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final leadProvider = context.watch<LeadProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Leads'),
        actions: [
          Showcase(
            key: _agendaShowcaseKey,
            title: 'Your agenda',
            description: 'Upcoming follow-ups, viewings, and move-ins land '
                'here automatically.',
            child: IconButton(
              icon: const Icon(Icons.event_note_outlined),
              tooltip: 'Agenda',
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const AgendaScreen()),
              ),
            ),
          ),
          PopupMenuButton<LeadSortOrder>(
            icon: const Icon(Icons.sort),
            tooltip: 'Sort',
            onSelected: leadProvider.setSortOrder,
            itemBuilder: (context) => const [
              PopupMenuItem(
                value: LeadSortOrder.followUpDate,
                child: Text('By follow-up date'),
              ),
              PopupMenuItem(
                value: LeadSortOrder.recentlyAdded,
                child: Text('Recently added'),
              ),
              PopupMenuItem(
                value: LeadSortOrder.name,
                child: Text('Name (A–Z)'),
              ),
            ],
          ),
          IconButton(
            icon: const Icon(Icons.settings_outlined),
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const SettingsScreen()),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          const TrialBanner(),
          if (!leadProvider.isLoading && !leadProvider.isEmpty)
            _StatusSummaryRow(provider: leadProvider),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: TextField(
              controller: _searchController,
              onChanged: leadProvider.setSearchQuery,
              decoration: InputDecoration(
                hintText: 'Search by name, property, or phone',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: leadProvider.searchQuery.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchController.clear();
                          leadProvider.setSearchQuery('');
                        },
                      )
                    : null,
              ),
            ),
          ),
          _StatusFilterRow(provider: leadProvider),
          const SizedBox(height: 4),
          Expanded(child: _LeadListBody(provider: leadProvider)),
        ],
      ),
      floatingActionButton: Showcase(
        key: _newLeadShowcaseKey,
        title: 'Add your first lead',
        description: 'Track a prospect from first contact to closed deal — '
            'tap here to get started.',
        targetBorderRadius: const BorderRadius.all(Radius.circular(16)),
        child: FloatingActionButton.extended(
          onPressed: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const LeadFormScreen()),
          ),
          icon: const Icon(Icons.add),
          label: const Text('New lead'),
        ),
      ),
    );
  }
}

/// Lead count + total commission per status, tap a card to filter the list
/// below by that status.
class _StatusSummaryRow extends StatelessWidget {
  const _StatusSummaryRow({required this.provider});

  final LeadProvider provider;

  @override
  Widget build(BuildContext context) {
    final summary = provider.summaryByStatus;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      child: Row(
        children: [
          for (final status in LeadStatus.values) ...[
            if (status != LeadStatus.values.first) const SizedBox(width: 8),
            Expanded(
              child: _StatusSummaryCard(
                status: status,
                summary: summary[status]!,
                selected: provider.statusFilter == status,
                onTap: () => provider.setStatusFilter(
                  provider.statusFilter == status ? null : status,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _StatusSummaryCard extends StatelessWidget {
  const _StatusSummaryCard({
    required this.status,
    required this.summary,
    required this.selected,
    required this.onTap,
  });

  final LeadStatus status;
  final LeadStatusSummary summary;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = AppTheme.statusColor(status.wireValue);
    return InkWell(
      borderRadius: BorderRadius.circular(10),
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: BoxDecoration(
          color: color.withValues(alpha: selected ? 0.18 : 0.08),
          borderRadius: BorderRadius.circular(10),
          border: selected ? Border.all(color: color, width: 1.5) : null,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              status.label,
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: color,
              ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 2),
            Text(
              '${summary.count}',
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            Text(
              Formatters.currency(summary.totalCommission),
              style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusFilterRow extends StatelessWidget {
  const _StatusFilterRow({required this.provider});

  final LeadProvider provider;

  @override
  Widget build(BuildContext context) {
    final options = <LeadStatus?>[null, ...LeadStatus.values];
    return SizedBox(
      height: 40,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: options.length,
        separatorBuilder: (_, __) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final status = options[index];
          final selected = provider.statusFilter == status;
          return ChoiceChip(
            label: Text(status?.label ?? 'All'),
            selected: selected,
            onSelected: (_) => provider.setStatusFilter(status),
          );
        },
      ),
    );
  }
}

class _LeadListBody extends StatelessWidget {
  const _LeadListBody({required this.provider});

  final LeadProvider provider;

  @override
  Widget build(BuildContext context) {
    if (provider.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (provider.error != null) {
      return AppEmptyState(
        icon: Icons.error_outline,
        title: 'Couldn\'t load your leads',
        message: provider.error!,
      );
    }
    if (provider.isEmpty) {
      return AppEmptyState(
        icon: Icons.people_outline,
        title: 'No leads yet',
        message: 'Add your first lead to start tracking follow-ups and '
            'commissions.',
        actionLabel: 'Add a lead',
        onAction: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const LeadFormScreen()),
        ),
      );
    }

    final visible = provider.visibleLeads;
    if (visible.isEmpty) {
      return const AppEmptyState(
        icon: Icons.search_off,
        title: 'No matches',
        message: 'Try a different search term or filter.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 96),
      itemCount: visible.length,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final lead = visible[index];
        return LeadListTile(
          lead: lead,
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => LeadDetailScreen(leadId: lead.id)),
          ),
        );
      },
    );
  }
}
