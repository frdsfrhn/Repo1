import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/lead.dart';
import '../../providers/lead_provider.dart';
import '../../widgets/app_empty_state.dart';
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

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final leadProvider = context.watch<LeadProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Leads'),
        actions: [
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
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const LeadFormScreen()),
        ),
        icon: const Icon(Icons.add),
        label: const Text('New lead'),
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
