import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/theme.dart';
import '../../../providers/subscription_provider.dart';
import '../paywall_screen.dart';

/// Persistent, non-blocking banner reflecting trial/subscription state.
/// The MVP checklist requires "clear in-app messaging when trial ends —
/// no silent feature lockout", so this is shown everywhere the dashboard
/// is, not just at the moment AI features are blocked.
class TrialBanner extends StatelessWidget {
  const TrialBanner({super.key});

  @override
  Widget build(BuildContext context) {
    final sub = context.watch<SubscriptionProvider>();

    switch (sub.accessLevel) {
      case AccessLevel.subscribed:
        return const SizedBox.shrink();
      case AccessLevel.trialActive:
        final days = sub.trialDaysRemaining;
        return _Banner(
          color: AppTheme.primary,
          text: days <= 1
              ? 'Your free trial ends today'
              : 'Free trial: $days days left',
          actionLabel: 'Upgrade',
        );
      case AccessLevel.trialExpired:
        return const _Banner(
          color: AppTheme.warning,
          text: 'Your trial has ended — AI voice features are paused',
          actionLabel: 'Subscribe',
        );
    }
  }
}

class _Banner extends StatelessWidget {
  const _Banner({
    required this.color,
    required this.text,
    required this.actionLabel,
  });

  final Color color;
  final String text;
  final String actionLabel;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: color.withValues(alpha: 0.12),
      child: InkWell(
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const PaywallScreen()),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Row(
            children: [
              Icon(Icons.info_outline, size: 18, color: color),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  text,
                  style: TextStyle(color: color, fontWeight: FontWeight.w500),
                ),
              ),
              Text(
                actionLabel,
                style: TextStyle(color: color, fontWeight: FontWeight.bold),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
