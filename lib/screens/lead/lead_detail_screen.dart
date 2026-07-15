import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../models/lead.dart';
import '../../providers/activity_provider.dart';
import '../../providers/auth_provider.dart';
import '../../providers/lead_provider.dart';
import '../../services/firestore_service.dart';
import '../../services/storage_service.dart';
import '../../services/transcription_service.dart';
import 'lead_form_screen.dart';
import 'widgets/activity_log_list.dart';
import 'widgets/voice_recorder_widget.dart';

class LeadDetailScreen extends StatelessWidget {
  const LeadDetailScreen({super.key, required this.leadId});

  final String leadId;

  @override
  Widget build(BuildContext context) {
    final agentId = context.read<AppAuthProvider>().firebaseUser!.uid;

    return ChangeNotifierProvider<ActivityProvider>(
      create: (_) => ActivityProvider(
        firestoreService: FirestoreService(),
        storageService: StorageService(),
        transcriptionService: TranscriptionService(),
        leadId: leadId,
        agentId: agentId,
      ),
      child: _LeadDetailBody(leadId: leadId),
    );
  }
}

class _LeadDetailBody extends StatefulWidget {
  const _LeadDetailBody({required this.leadId});

  final String leadId;

  @override
  State<_LeadDetailBody> createState() => _LeadDetailBodyState();
}

class _LeadDetailBodyState extends State<_LeadDetailBody> {
  Future<void> _addTypedNote(BuildContext context) async {
    final controller = TextEditingController();
    final text = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Add typed note'),
        content: TextField(
          controller: controller,
          maxLines: 5,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'What happened?'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, controller.text.trim()),
            child: const Text('Save'),
          ),
        ],
      ),
    );
    if (text != null && text.isNotEmpty && context.mounted) {
      await context.read<ActivityProvider>().addTypedNote(text);
    }
  }

  Future<void> _confirmDeleteLead(BuildContext context, Lead lead) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete this lead?'),
        content: Text(
          'This permanently deletes ${lead.prospectName.isEmpty ? 'this lead' : lead.prospectName}, '
          'its full activity log, and any recordings. This cannot be undone.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppTheme.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      await context.read<LeadProvider>().deleteLead(lead.id);
      if (context.mounted) Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final lead = _findLead(context, widget.leadId);

    if (lead == null) {
      return Scaffold(
        appBar: AppBar(),
        body: const Center(child: Text('This lead no longer exists.')),
      );
    }

    final resolvedLead = lead;

    return Scaffold(
      appBar: AppBar(
        title: Text(resolvedLead.prospectName.isEmpty
            ? 'Lead'
            : resolvedLead.prospectName),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit_outlined),
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => LeadFormScreen(lead: resolvedLead),
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.delete_outline),
            onPressed: () => _confirmDeleteLead(context, resolvedLead),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
        children: [
          _SummaryCard(lead: resolvedLead),
          const SizedBox(height: 24),
          Text('Add activity', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          const VoiceRecorderWidget(),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            onPressed: () => _addTypedNote(context),
            icon: const Icon(Icons.edit_note),
            label: const Text('Add typed note'),
          ),
          const SizedBox(height: 20),
          Text('Activity log', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          const ActivityLogList(),
        ],
      ),
    );
  }

  Lead? _findLead(BuildContext context, String id) {
    final provider = context.watch<LeadProvider>();
    for (final l in provider.allLeads) {
      if (l.id == id) return l;
    }
    return null;
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.lead});

  final Lead lead;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (lead.propertyImageUrl.isNotEmpty)
            GestureDetector(
              onTap: () => _showFullImage(context, lead.propertyImageUrl),
              child: Image.network(
                lead.propertyImageUrl,
                height: 180,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => const SizedBox.shrink(),
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        lead.propertyName,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppTheme.statusColor(lead.status.wireValue)
                            .withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        lead.status.label,
                        style: TextStyle(
                          color: AppTheme.statusColor(lead.status.wireValue),
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
                if (lead.propertyAddress.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(
                    lead.propertyAddress,
                    style: TextStyle(color: Colors.grey.shade600),
                  ),
                ],
                const Divider(height: 28),
                _row('Phone', lead.phoneNumber),
                _row('Deal type', lead.dealType.label),
                _row(
                    lead.dealType == DealType.rent
                        ? 'Rental value'
                        : 'Purchase price',
                    Formatters.currency(lead.dealValue)),
                _row(
                    'Commission',
                    '${Formatters.percent(lead.commissionPercent)} '
                    '(${Formatters.currency(lead.commissionValue)})'),
                if (lead.dealType == DealType.purchase &&
                    lead.expectedYieldPercent != null)
                  _row('Expected yield',
                      Formatters.percent(lead.expectedYieldPercent!)),
                _row('Next follow-up', Formatters.date(lead.nextFollowUpDate)),
                if (lead.viewingDate != null)
                  _row('Viewing date', Formatters.date(lead.viewingDate)),
                if (lead.moveInDate != null)
                  _row('Move-in date', Formatters.date(lead.moveInDate)),
                if (lead.phoneNumber.isNotEmpty ||
                    lead.telegramUrl.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      if (lead.phoneNumber.isNotEmpty)
                        OutlinedButton.icon(
                          onPressed: () => _launch(
                            context,
                            'https://wa.me/'
                            '${Formatters.whatsAppDigits(lead.phoneNumber)}',
                          ),
                          icon:
                              const Icon(Icons.chat, color: Color(0xFF25D366)),
                          label: const Text('WhatsApp'),
                        ),
                      if (lead.telegramUrl.isNotEmpty)
                        OutlinedButton.icon(
                          onPressed: () => _launch(context, lead.telegramUrl),
                          icon:
                              const Icon(Icons.send, color: Color(0xFF229ED9)),
                          label: const Text('Telegram'),
                        ),
                    ],
                  ),
                ],
                if (lead.ownerName.isNotEmpty || lead.ownerPhone.isNotEmpty) ...[
                  const Divider(height: 28),
                  Text('Property owner',
                      style: Theme.of(context).textTheme.labelLarge),
                  const SizedBox(height: 8),
                  if (lead.ownerName.isNotEmpty)
                    _row('Name', lead.ownerName),
                  if (lead.ownerPhone.isNotEmpty)
                    _row('Phone', lead.ownerPhone),
                  if (lead.ownerPhone.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    OutlinedButton.icon(
                      onPressed: () => _launch(
                        context,
                        'https://wa.me/'
                        '${Formatters.whatsAppDigits(lead.ownerPhone)}',
                      ),
                      icon: const Icon(Icons.chat, color: Color(0xFF25D366)),
                      label: const Text('WhatsApp owner'),
                    ),
                  ],
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  static void _showFullImage(BuildContext context, String url) {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => Scaffold(
        backgroundColor: Colors.black,
        appBar: AppBar(backgroundColor: Colors.black, foregroundColor: Colors.white),
        body: Center(
          child: InteractiveViewer(
            child: Image.network(url),
          ),
        ),
      ),
    ));
  }

  Future<void> _launch(BuildContext context, String url) async {
    final uri = Uri.tryParse(url);
    final launched = uri != null &&
        await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!launched && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not open the app for this link.')),
      );
    }
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: Text(label, style: TextStyle(color: Colors.grey.shade600)),
          ),
          Expanded(
            child:
                Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
          ),
        ],
      ),
    );
  }
}
