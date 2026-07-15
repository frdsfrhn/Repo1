import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/lead.dart';
import '../services/firestore_service.dart';
import '../services/storage_service.dart';

enum LeadSortOrder { followUpDate, recentlyAdded, name }

class LeadStatusSummary {
  const LeadStatusSummary({required this.count, required this.totalCommission});

  final int count;
  final double totalCommission;
}

/// What kind of date-driven item this agenda entry is — follow-up, viewing,
/// or move-in — so the agenda screen can label/group them.
enum AgendaType { followUp, viewing, moveIn }

extension AgendaTypeX on AgendaType {
  String get label {
    switch (this) {
      case AgendaType.followUp:
        return 'Follow-up';
      case AgendaType.viewing:
        return 'Viewing';
      case AgendaType.moveIn:
        return 'Move-in';
    }
  }
}

/// One dated item for the agenda screen — a lead paired with which of its
/// date fields this entry came from.
class AgendaEntry {
  const AgendaEntry({required this.lead, required this.type, required this.date});

  final Lead lead;
  final AgendaType type;
  final DateTime date;
}

/// Owns the dashboard's lead list: the live Firestore stream, plus
/// client-side search/sort/status-filter over it. See
/// FirestoreService.watchLeads for why search stays client-side in v1.
class LeadProvider extends ChangeNotifier {
  LeadProvider({
    required FirestoreService firestoreService,
    required StorageService storageService,
    required String agentId,
  })  : _firestoreService = firestoreService,
        _storageService = storageService,
        _agentId = agentId {
    _sub = _firestoreService.watchLeads(_agentId).listen((leads) {
      _allLeads = leads;
      _isLoading = false;
      notifyListeners();
    }, onError: (Object e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
    });
  }

  final FirestoreService _firestoreService;
  final StorageService _storageService;
  final String _agentId;
  StreamSubscription<List<Lead>>? _sub;

  List<Lead> _allLeads = [];
  bool _isLoading = true;
  String? _error;
  String _searchQuery = '';
  LeadStatus? _statusFilter;
  LeadSortOrder _sortOrder = LeadSortOrder.followUpDate;

  bool get isLoading => _isLoading;
  String? get error => _error;
  String get searchQuery => _searchQuery;
  LeadStatus? get statusFilter => _statusFilter;
  LeadSortOrder get sortOrder => _sortOrder;
  bool get isEmpty => _allLeads.isEmpty;

  /// Unfiltered — use this (not [visibleLeads]) to look up a single lead
  /// by id, so an active dashboard search/status filter can't hide a lead
  /// the user has already navigated into.
  List<Lead> get allLeads => _allLeads;

  /// Lead count and total commission per status, across the whole
  /// portfolio (not affected by the dashboard's search/filter) — backs
  /// the dashboard's summary row. Not part of the original locked data
  /// model; a quick aggregation over data that already exists per lead.
  Map<LeadStatus, LeadStatusSummary> get summaryByStatus {
    final map = {
      for (final status in LeadStatus.values)
        status: const LeadStatusSummary(count: 0, totalCommission: 0),
    };
    for (final lead in _allLeads) {
      final current = map[lead.status]!;
      map[lead.status] = LeadStatusSummary(
        count: current.count + 1,
        totalCommission: current.totalCommission + lead.commissionValue,
      );
    }
    return map;
  }

  /// Every follow-up/viewing/move-in date across all leads, flattened into
  /// one sorted list — backs the agenda screen ("what's on my plate"),
  /// which buckets these into today/tomorrow/this week client-side.
  List<AgendaEntry> get agendaEntries {
    final entries = <AgendaEntry>[];
    for (final lead in _allLeads) {
      if (lead.nextFollowUpDate != null) {
        entries.add(AgendaEntry(
          lead: lead,
          type: AgendaType.followUp,
          date: lead.nextFollowUpDate!,
        ));
      }
      if (lead.viewingDate != null) {
        entries.add(AgendaEntry(
          lead: lead,
          type: AgendaType.viewing,
          date: lead.viewingDate!,
        ));
      }
      if (lead.moveInDate != null) {
        entries.add(AgendaEntry(
          lead: lead,
          type: AgendaType.moveIn,
          date: lead.moveInDate!,
        ));
      }
    }
    entries.sort((a, b) => a.date.compareTo(b.date));
    return entries;
  }

  List<Lead> get visibleLeads {
    var leads = _allLeads.where((lead) {
      if (_statusFilter != null && lead.status != _statusFilter) return false;
      if (_searchQuery.trim().isEmpty) return true;
      final q = _searchQuery.trim().toLowerCase();
      return lead.prospectName.toLowerCase().contains(q) ||
          lead.propertyName.toLowerCase().contains(q) ||
          lead.phoneNumber.contains(q);
    }).toList();

    switch (_sortOrder) {
      case LeadSortOrder.followUpDate:
        leads.sort((a, b) {
          if (a.nextFollowUpDate == null && b.nextFollowUpDate == null) {
            return 0;
          }
          if (a.nextFollowUpDate == null) return 1;
          if (b.nextFollowUpDate == null) return -1;
          return a.nextFollowUpDate!.compareTo(b.nextFollowUpDate!);
        });
        break;
      case LeadSortOrder.recentlyAdded:
        leads.sort((a, b) => b.createdAt.compareTo(a.createdAt));
        break;
      case LeadSortOrder.name:
        leads.sort((a, b) =>
            a.prospectName.toLowerCase().compareTo(b.prospectName.toLowerCase()));
        break;
    }
    return leads;
  }

  void setSearchQuery(String query) {
    _searchQuery = query;
    notifyListeners();
  }

  void setStatusFilter(LeadStatus? status) {
    _statusFilter = status;
    notifyListeners();
  }

  void setSortOrder(LeadSortOrder order) {
    _sortOrder = order;
    notifyListeners();
  }

  Future<String> createLead(Lead lead) => _firestoreService.createLead(lead);

  Future<void> updateLead(Lead lead) => _firestoreService.updateLead(lead);

  Future<void> deleteLead(String leadId) {
    return _firestoreService.deleteLeadCascade(
      leadId,
      agentId: _agentId,
      deleteAudio: _storageService.deleteByGsUrl,
    );
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}
