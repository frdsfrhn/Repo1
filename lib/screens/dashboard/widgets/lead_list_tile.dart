import 'package:flutter/material.dart';

import '../../../core/formatters.dart';
import '../../../core/theme.dart';
import '../../../models/lead.dart';

class LeadListTile extends StatelessWidget {
  const LeadListTile({super.key, required this.lead, required this.onTap});

  final Lead lead;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final statusColor = AppTheme.statusColor(lead.status.wireValue);
    final isOverdue = lead.nextFollowUpDate != null &&
        lead.nextFollowUpDate!.isBefore(DateTime.now()) &&
        lead.status != LeadStatus.closed;

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              lead.propertyImageUrl.isNotEmpty
                  ? ClipRRect(
                      borderRadius: BorderRadius.circular(10),
                      child: Image.network(
                        lead.propertyImageUrl,
                        width: 44,
                        height: 44,
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => _InitialAvatar(
                          lead: lead,
                          statusColor: statusColor,
                        ),
                      ),
                    )
                  : _InitialAvatar(lead: lead, statusColor: statusColor),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      lead.prospectName.isEmpty
                          ? '(No name)'
                          : lead.prospectName,
                      style: Theme.of(context).textTheme.titleMedium,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 2),
                    Text(
                      lead.propertyName.isEmpty
                          ? '(No property)'
                          : lead.propertyName,
                      style: Theme.of(context)
                          .textTheme
                          .bodySmall
                          ?.copyWith(color: Colors.grey.shade600),
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        _StatusChip(label: lead.status.label, color: statusColor),
                        const SizedBox(width: 8),
                        Text(
                          Formatters.relativeDueDate(lead.nextFollowUpDate),
                          style: TextStyle(
                            fontSize: 12,
                            color: isOverdue
                                ? AppTheme.danger
                                : Colors.grey.shade600,
                            fontWeight:
                                isOverdue ? FontWeight.w600 : FontWeight.normal,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Text(
                lead.dealType.label,
                style: Theme.of(context)
                    .textTheme
                    .labelMedium
                    ?.copyWith(color: Colors.grey.shade500),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _InitialAvatar extends StatelessWidget {
  const _InitialAvatar({required this.lead, required this.statusColor});

  final Lead lead;
  final Color statusColor;

  @override
  Widget build(BuildContext context) {
    return CircleAvatar(
      radius: 22,
      backgroundColor: statusColor.withValues(alpha: 0.15),
      child: Text(
        lead.prospectName.isNotEmpty ? lead.prospectName[0].toUpperCase() : '?',
        style: TextStyle(color: statusColor, fontWeight: FontWeight.bold),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
