import 'package:cloud_firestore/cloud_firestore.dart';

import '../models/activity.dart';
import '../models/lead.dart';

/// All Firestore reads/writes for leads and their activity logs.
///
/// Every query and write is scoped to `agentId == agentId` on the client
/// side too, even though Firestore security rules are the real enforcement
/// boundary (see firestore.rules) — this keeps queries index-friendly and
/// gives a second layer of defense against a coding mistake leaking another
/// agent's data.
class FirestoreService {
  FirestoreService({FirebaseFirestore? firestore})
      : _firestore = firestore ?? FirebaseFirestore.instance;

  final FirebaseFirestore _firestore;

  CollectionReference<Map<String, dynamic>> get _leadsRef =>
      _firestore.collection('leads');

  CollectionReference<Map<String, dynamic>> _activitiesRef(String leadId) =>
      _leadsRef.doc(leadId).collection('activities');

  /// All of an agent's leads, sorted by soonest follow-up first. Leads
  /// with no follow-up date are pushed to the end. Search and status
  /// filtering happen client-side on this stream (see LeadProvider) —
  /// the lead counts involved don't justify a search backend in v1.
  Stream<List<Lead>> watchLeads(String agentId) {
    return _leadsRef
        .where('agentId', isEqualTo: agentId)
        .orderBy('nextFollowUpDate', descending: false)
        .snapshots()
        .map((snap) => snap.docs.map(Lead.fromFirestore).toList());
  }

  Future<Lead> getLead(String leadId) async {
    final doc = await _leadsRef.doc(leadId).get();
    return Lead.fromFirestore(doc);
  }

  Future<String> createLead(Lead lead) async {
    final docRef = await _leadsRef.add(lead.toFirestore(isCreate: true));
    return docRef.id;
  }

  Future<void> updateLead(Lead lead) async {
    await _leadsRef.doc(lead.id).update(lead.toFirestore());
  }

  /// Deletes a lead's activity log first (batched), then the lead itself.
  /// Voice-note audio files are already gone by this point in the normal
  /// flow (deleted right after transcription — see TranscriptionService),
  /// but any stragglers are swept by [audioUrl] cleanup in the activity
  /// loop below, so "delete a lead and its recordings permanently" holds
  /// even if a transcription job never finished.
  Future<void> deleteLeadCascade(
    String leadId, {
    required String agentId,
    required Future<void> Function(String audioUrl) deleteAudio,
  }) async {
    final activitiesSnap = await _activitiesRef(leadId)
        .where('agentId', isEqualTo: agentId)
        .get();
    for (final doc in activitiesSnap.docs) {
      final audioUrl = doc.data()['audioUrl'] as String?;
      if (audioUrl != null && audioUrl.isNotEmpty) {
        await deleteAudio(audioUrl);
      }
    }
    final batch = _firestore.batch();
    for (final doc in activitiesSnap.docs) {
      batch.delete(doc.reference);
    }
    batch.delete(_leadsRef.doc(leadId));
    await batch.commit();
  }

  /// `agentId` must be passed and filtered on here even though every
  /// activity under this lead already belongs to the caller — Firestore's
  /// security rules can't validate a *list* query against a rule that
  /// reads `resource.data.agentId` unless the query itself constrains
  /// that field with a matching `where`. Without it, this fails with
  /// `permission-denied` regardless of what the documents actually
  /// contain, because Firestore has no way to prove every possible
  /// result would satisfy the rule from the query shape alone.
  Stream<List<Activity>> watchActivities(String leadId, String agentId) {
    return _activitiesRef(leadId)
        .where('agentId', isEqualTo: agentId)
        .orderBy('timestamp', descending: true)
        .snapshots()
        .map((snap) =>
            snap.docs.map((d) => Activity.fromFirestore(d, leadId)).toList());
  }

  Future<String> addActivity(Activity activity) async {
    final docRef = await _activitiesRef(activity.leadId)
        .add(activity.toFirestore(isCreate: true));
    return docRef.id;
  }

  Future<void> updateActivity(Activity activity) async {
    await _activitiesRef(activity.leadId)
        .doc(activity.id)
        .update(activity.toFirestore());
  }

  Future<void> deleteActivity({
    required String leadId,
    required String activityId,
    String? audioUrl,
    required Future<void> Function(String audioUrl) deleteAudio,
  }) async {
    if (audioUrl != null && audioUrl.isNotEmpty) {
      await deleteAudio(audioUrl);
    }
    await _activitiesRef(leadId).doc(activityId).delete();
  }
}
