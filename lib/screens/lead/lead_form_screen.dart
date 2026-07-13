import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/formatters.dart';
import '../../models/lead.dart';
import '../../providers/auth_provider.dart';
import '../../providers/lead_provider.dart';
import '../../widgets/loading_overlay.dart';

/// Create or edit a lead. Passing an existing [lead] switches to edit mode.
class LeadFormScreen extends StatefulWidget {
  const LeadFormScreen({super.key, this.lead});

  final Lead? lead;

  bool get isEditing => lead != null;

  @override
  State<LeadFormScreen> createState() => _LeadFormScreenState();
}

class _LeadFormScreenState extends State<LeadFormScreen> {
  final _formKey = GlobalKey<FormState>();

  late final TextEditingController _nameController;
  late final TextEditingController _phoneController;
  late final TextEditingController _propertyNameController;
  late final TextEditingController _propertyAddressController;
  late final TextEditingController _dealValueController;
  late final TextEditingController _commissionPercentController;
  late final TextEditingController _yieldController;

  late DealType _dealType;
  late LeadStatus _status;
  DateTime? _nextFollowUpDate;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    final lead = widget.lead;
    _nameController = TextEditingController(text: lead?.prospectName ?? '');
    _phoneController = TextEditingController(text: lead?.phoneNumber ?? '');
    _propertyNameController =
        TextEditingController(text: lead?.propertyName ?? '');
    _propertyAddressController =
        TextEditingController(text: lead?.propertyAddress ?? '');
    _dealValueController = TextEditingController(
        text: lead == null || lead.dealValue == 0 ? '' : lead.dealValue.toString());
    _commissionPercentController = TextEditingController(
        text: lead == null || lead.commissionPercent == 0
            ? ''
            : lead.commissionPercent.toString());
    _yieldController = TextEditingController(
        text: lead?.expectedYieldPercent?.toString() ?? '');
    _dealType = lead?.dealType ?? DealType.rent;
    _status = lead?.status ?? LeadStatus.open;
    _nextFollowUpDate = lead?.nextFollowUpDate;

    for (final controller in [_dealValueController, _commissionPercentController]) {
      controller.addListener(() => setState(() {}));
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _phoneController.dispose();
    _propertyNameController.dispose();
    _propertyAddressController.dispose();
    _dealValueController.dispose();
    _commissionPercentController.dispose();
    _yieldController.dispose();
    super.dispose();
  }

  double get _dealValue => double.tryParse(_dealValueController.text) ?? 0;
  double get _commissionPercent =>
      double.tryParse(_commissionPercentController.text) ?? 0;
  double get _computedCommission => _dealValue * (_commissionPercent / 100);

  Future<void> _pickFollowUpDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _nextFollowUpDate ?? now,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 3),
    );
    if (picked != null) setState(() => _nextFollowUpDate = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);

    final leadProvider = context.read<LeadProvider>();
    final agentId = context.read<AuthProvider>().firebaseUser!.uid;

    try {
      if (widget.isEditing) {
        final updated = widget.lead!.copyWith(
          prospectName: _nameController.text.trim(),
          phoneNumber: _phoneController.text.trim(),
          propertyName: _propertyNameController.text.trim(),
          propertyAddress: _propertyAddressController.text.trim(),
          dealType: _dealType,
          commissionPercent: _commissionPercent,
          dealValue: _dealValue,
          expectedYieldPercent: _dealType == DealType.purchase
              ? double.tryParse(_yieldController.text)
              : null,
          clearExpectedYield: _dealType == DealType.rent,
          nextFollowUpDate: _nextFollowUpDate,
          status: _status,
        );
        await leadProvider.updateLead(updated);
      } else {
        final lead = Lead(
          id: '',
          agentId: agentId,
          prospectName: _nameController.text.trim(),
          phoneNumber: _phoneController.text.trim(),
          propertyName: _propertyNameController.text.trim(),
          propertyAddress: _propertyAddressController.text.trim(),
          dealType: _dealType,
          commissionPercent: _commissionPercent,
          dealValue: _dealValue,
          expectedYieldPercent: _dealType == DealType.purchase
              ? double.tryParse(_yieldController.text)
              : null,
          nextFollowUpDate: _nextFollowUpDate,
          status: _status,
          createdAt: DateTime.now(),
          updatedAt: DateTime.now(),
        );
        await leadProvider.createLead(lead);
      }
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Could not save: $e')));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.isEditing ? 'Edit lead' : 'New lead'),
      ),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
            children: [
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(labelText: 'Prospect name'),
                textCapitalization: TextCapitalization.words,
                validator: (v) =>
                    (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _phoneController,
                decoration: const InputDecoration(labelText: 'Phone number'),
                keyboardType: TextInputType.phone,
                validator: (v) =>
                    (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _propertyNameController,
                decoration:
                    const InputDecoration(labelText: 'Property name'),
                validator: (v) =>
                    (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _propertyAddressController,
                decoration: const InputDecoration(
                    labelText: 'Property address (optional)'),
                textCapitalization: TextCapitalization.sentences,
              ),
              const SizedBox(height: 20),
              Text('Deal type', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              SegmentedButton<DealType>(
                segments: const [
                  ButtonSegment(value: DealType.rent, label: Text('Rent')),
                  ButtonSegment(
                      value: DealType.purchase, label: Text('Purchase')),
                ],
                selected: {_dealType},
                onSelectionChanged: (selection) =>
                    setState(() => _dealType = selection.first),
              ),
              const SizedBox(height: 20),
              TextFormField(
                controller: _dealValueController,
                decoration: InputDecoration(
                  labelText: _dealType == DealType.rent
                      ? 'Rental value (RM)'
                      : 'Purchase price (RM)',
                ),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}')),
                ],
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _commissionPercentController,
                decoration:
                    const InputDecoration(labelText: 'Commission (%)'),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}')),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Computed commission: ${Formatters.currency(_computedCommission)}',
                style: Theme.of(context)
                    .textTheme
                    .bodyMedium
                    ?.copyWith(fontWeight: FontWeight.w600),
              ),
              if (_dealType == DealType.purchase) ...[
                const SizedBox(height: 12),
                TextFormField(
                  controller: _yieldController,
                  decoration: const InputDecoration(
                      labelText: 'Expected yield (%) — purchase leads only'),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(
                        RegExp(r'^\d*\.?\d{0,2}')),
                  ],
                ),
              ],
              const SizedBox(height: 20),
              Text('Next follow-up',
                  style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: _pickFollowUpDate,
                icon: const Icon(Icons.event),
                label: Text(
                  _nextFollowUpDate == null
                      ? 'Set a date'
                      : Formatters.date(_nextFollowUpDate),
                ),
              ),
              const SizedBox(height: 20),
              Text('Status', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              SegmentedButton<LeadStatus>(
                segments: LeadStatus.values
                    .map((s) => ButtonSegment(value: s, label: Text(s.label)))
                    .toList(),
                selected: {_status},
                onSelectionChanged: (selection) =>
                    setState(() => _status = selection.first),
              ),
              const SizedBox(height: 28),
              InlineLoadingButton(
                isLoading: _isSaving,
                label: widget.isEditing ? 'Save changes' : 'Add lead',
                onPressed: _save,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
