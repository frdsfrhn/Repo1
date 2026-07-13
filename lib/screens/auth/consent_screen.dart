import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/constants.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/loading_overlay.dart';

/// Explicit PDPA consent screen shown once, before an agent can reach the
/// dashboard, per the MVP checklist's Malaysia PDPA requirement. Lists
/// what's stored and links out to the full privacy policy.
class ConsentScreen extends StatefulWidget {
  const ConsentScreen({super.key});

  @override
  State<ConsentScreen> createState() => _ConsentScreenState();
}

class _ConsentScreenState extends State<ConsentScreen> {
  bool _isSubmitting = false;

  Future<void> _accept() async {
    setState(() => _isSubmitting = true);
    await context.read<AppAuthProvider>().acceptPdpaConsent();
    // AppAuthProvider's profile stream will update hasGivenConsent and the
    // app router will move on automatically.
  }

  Widget _bullet(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('•  '),
          Expanded(child: Text(text)),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Before you start')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'What we store',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              Text(
                'To run this app on your behalf, we store the following '
                'data, tied to your account only:',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 16),
              _bullet('Client names and phone numbers you enter as leads'),
              _bullet('Deal financials: commission percentage, deal value, expected yield'),
              _bullet('Voice recordings you make, only until they are transcribed — '
                  'the audio is then deleted and only the text transcript is kept'),
              _bullet('Transcripts (both the original and the AI-cleaned version)'),
              const SizedBox(height: 20),
              Text(
                'Your data, your control',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              _bullet('Only you can see your leads — this is enforced at the database level, '
                  'not just in the app'),
              _bullet('You can permanently delete any lead, its notes, and any recordings '
                  'from Settings at any time'),
              _bullet('You can delete your entire account and all associated data from Settings'),
              const SizedBox(height: 24),
              TextButton(
                onPressed: () =>
                    launchUrl(Uri.parse(AppConstants.privacyPolicyUrl)),
                child: const Text('Read the full privacy policy'),
              ),
              const SizedBox(height: 12),
              InlineLoadingButton(
                isLoading: _isSubmitting,
                label: 'I understand and agree',
                onPressed: _accept,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
