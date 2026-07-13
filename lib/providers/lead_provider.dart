import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/lead.dart';
import '../services/firestore_service.dart';
import '../services/storage_service.dart';

enum LeadSortOrder { followUpDate, recentlyAdded, name }

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
