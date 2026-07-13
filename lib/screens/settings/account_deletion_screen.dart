import 'package:cloud_functions/cloud_functions.dart';
import 'package:flutter/material.dart';

import '../../core/constants.dart';
import '../../core/theme.dart';
import '../../widgets/loading_overlay.dart';

/// Apple requires an in-app account-deletion path whenever an app lets
/// users create accounts. This calls the `deleteAccount` Cloud Function
/// (functions/src/deleteAccount.ts), which removes every lead, activity,
/// and recording the agent owns, then the Firebase Auth user itself —
/// matching the PDPA "permanent deletion" requirement, not just a
/// soft-disable.
class AccountDeletionScreen extends StatefulWidget {
  const AccountDeletionScreen({super.key});

  @override
  State<AccountDeletionScreen> createState() => _AccountDeletionScreenState();
}

class _AccountDeletionScreenState extends State<AccountDeletionScreen> {
  final _confirmController = TextEditingController();
  bool _isDeleting = false;
  String? _error;

  bool get _canDelete =>
      _confirmController.text.trim().toUpperCase() == 'DELETE';

  @override
  void dispose() {
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _delete() async {
    setState(() {
      _isDeleting = true;
      _error = null;
    });
    try {
      final callable = FirebaseFunctions.instanceFor(
        region: AppConstants.cloudFunctionsRegion,
      ).httpsCallable('deleteAccount');
      await callable.call<void>();
      // Firebase Auth user is deleted server-side, so authStateChanges()
      // fires and the app router returns to the login screen on its own.
    } catch (e) {
      if (mounted) {
        setState(() {
          _isDeleting = false;
          _error = 'Could not delete your account: $e';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Delete account')),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.warning_amber_rounded,
                  color: AppTheme.danger, size: 40),
              const SizedBox(height: 16),
              Text(
                'This permanently deletes your account',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              const Text(
                'All of your leads, activity notes, transcripts, and any '
                'remaining voice recordings will be permanently deleted. '
                'Your subscription (if active) will not be automatically '
                'cancelled with your app store — cancel it separately from '
                'the App Store or Play Store if you no longer want to be '
                'billed. This cannot be undone.',
              ),
              const SizedBox(height: 24),
              Text('Type DELETE to confirm',
                  style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              TextField(
                controller: _confirmController,
                onChanged: (_) => setState(() {}),
                textCapitalization: TextCapitalization.characters,
                decoration: const InputDecoration(hintText: 'DELETE'),
              ),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: const TextStyle(color: AppTheme.danger)),
              ],
              const SizedBox(height: 20),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  style: FilledButton.styleFrom(
                    backgroundColor: AppTheme.danger,
                  ),
                  onPressed: (_canDelete && !_isDeleting) ? _delete : null,
                  child: _isDeleting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Permanently delete my account'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
