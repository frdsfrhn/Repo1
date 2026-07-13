import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/constants.dart';
import '../../core/theme.dart';
import '../../providers/auth_provider.dart';
import '../../providers/subscription_provider.dart';
import 'account_deletion_screen.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AppAuthProvider>();
    final sub = context.watch<SubscriptionProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          ListTile(
            leading: const Icon(Icons.phone_outlined),
            title: const Text('Phone number'),
            subtitle: Text(auth.profile?.phoneNumber ?? '—'),
          ),
          ListTile(
            leading: const Icon(Icons.workspace_premium_outlined),
            title: const Text('Subscription'),
            subtitle: Text(_subscriptionLabel(sub.accessLevel)),
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.privacy_tip_outlined),
            title: const Text('Privacy policy'),
            onTap: () => launchUrl(Uri.parse(AppConstants.privacyPolicyUrl)),
          ),
          ListTile(
            leading: const Icon(Icons.support_agent_outlined),
            title: const Text('Contact support'),
            subtitle: Text(AppConstants.supportEmail),
            onTap: () =>
                launchUrl(Uri.parse('mailto:${AppConstants.supportEmail}')),
          ),
          const Divider(),
          ListTile(
            leading: Icon(Icons.logout, color: Colors.grey.shade700),
            title: const Text('Sign out'),
            onTap: () => auth.signOut(),
          ),
          ListTile(
            leading: const Icon(Icons.delete_forever_outlined,
                color: AppTheme.danger),
            title: const Text('Delete account',
                style: TextStyle(color: AppTheme.danger)),
            subtitle: const Text(
                'Permanently deletes your account, all leads, and recordings'),
            onTap: () => Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const AccountDeletionScreen()),
            ),
          ),
        ],
      ),
    );
  }

  String _subscriptionLabel(AccessLevel level) {
    switch (level) {
      case AccessLevel.subscribed:
        return 'Active';
      case AccessLevel.trialActive:
        return 'Free trial';
      case AccessLevel.trialExpired:
        return 'Trial ended';
    }
  }
}
