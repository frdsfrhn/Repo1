import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show PlatformException;
import 'package:provider/provider.dart';
import 'package:purchases_flutter/purchases_flutter.dart';

import '../../core/constants.dart';
import '../../providers/subscription_provider.dart';
import '../../services/subscription_service.dart';
import '../../widgets/app_empty_state.dart';

class PaywallScreen extends StatefulWidget {
  const PaywallScreen({super.key});

  @override
  State<PaywallScreen> createState() => _PaywallScreenState();
}

class _PaywallScreenState extends State<PaywallScreen> {
  final _subscriptionService = SubscriptionService();
  Offerings? _offerings;
  bool _isLoading = true;
  bool _isPurchasing = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final offerings = await _subscriptionService.getOfferings();
      if (!mounted) return;
      setState(() {
        _offerings = offerings;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = 'Could not load subscription options: $e';
        _isLoading = false;
      });
    }
  }

  Future<void> _purchase(Package package) async {
    setState(() => _isPurchasing = true);
    try {
      await _subscriptionService.purchasePackage(package);
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Subscribed! Thank you.')));
      Navigator.of(context).pop();
    } on PlatformException catch (e) {
      final errorCode = PurchasesErrorHelper.getErrorCode(e);
      if (errorCode != PurchasesErrorCode.purchaseCancelledError && mounted) {
        setState(() => _error = 'Purchase failed: ${e.message}');
      }
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = 'Purchase failed: $e');
    } finally {
      if (mounted) setState(() => _isPurchasing = false);
    }
  }

  Future<void> _restore() async {
    setState(() => _isPurchasing = true);
    try {
      await _subscriptionService.restorePurchases();
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Purchases restored.')));
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = 'Restore failed: $e');
    } finally {
      if (mounted) setState(() => _isPurchasing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final sub = context.watch<SubscriptionProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Subscribe')),
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  Text(
                    sub.accessLevel == AccessLevel.trialExpired
                        ? 'Your free trial has ended'
                        : 'Unlock AI voice features',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Subscribing keeps voice-to-text capture and AI '
                    'transcript cleanup working. Your leads and notes are '
                    'always yours, subscribed or not.',
                  ),
                  const SizedBox(height: 24),
                  if (_error != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: Text(
                        _error!,
                        style:
                            TextStyle(color: Theme.of(context).colorScheme.error),
                      ),
                    ),
                  if (_offerings?.current == null ||
                      _offerings!.current!.availablePackages.isEmpty)
                    const AppEmptyState(
                      icon: Icons.storefront_outlined,
                      title: 'No plans available right now',
                      message:
                          'Check your connection and try again shortly, or '
                          'contact support if this persists.',
                    )
                  else
                    ..._offerings!.current!.availablePackages.map(
                      (package) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _PackageTile(
                          package: package,
                          isBusy: _isPurchasing,
                          onTap: () => _purchase(package),
                        ),
                      ),
                    ),
                  const SizedBox(height: 12),
                  Center(
                    child: TextButton(
                      onPressed: _isPurchasing ? null : _restore,
                      child: const Text('Restore purchases'),
                    ),
                  ),
                  Center(
                    child: Text(
                      'Contact ${AppConstants.supportEmail} for billing help.',
                      style: Theme.of(context)
                          .textTheme
                          .bodySmall
                          ?.copyWith(color: Colors.grey.shade600),
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}

class _PackageTile extends StatelessWidget {
  const _PackageTile({
    required this.package,
    required this.isBusy,
    required this.onTap,
  });

  final Package package;
  final bool isBusy;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final product = package.storeProduct;
    return Card(
      child: ListTile(
        title: Text(product.title),
        subtitle: Text(product.description),
        trailing: Text(
          product.priceString,
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        onTap: isBusy ? null : onTap,
      ),
    );
  }
}
