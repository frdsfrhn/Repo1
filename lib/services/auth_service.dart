import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:firebase_auth/firebase_auth.dart';

import '../models/app_user.dart';

/// Wraps Firebase phone-number auth (the only login method in v1) and the
/// per-agent profile document that lives alongside it.
class AuthService {
  AuthService({FirebaseAuth? auth, FirebaseFirestore? firestore})
      : _auth = auth ?? FirebaseAuth.instance,
        _firestore = firestore ?? FirebaseFirestore.instance;

  final FirebaseAuth _auth;
  final FirebaseFirestore _firestore;

  User? get currentUser => _auth.currentUser;

  Stream<User?> authStateChanges() => _auth.authStateChanges();

  CollectionReference<Map<String, dynamic>> get _usersRef =>
      _firestore.collection('users');

  /// Starts the phone verification flow. [onCodeSent] receives the
  /// verificationId to pass to [submitOtp]; on some Android devices
  /// [onAutoVerified] may fire first with no OTP screen needed.
  Future<void> startPhoneVerification({
    required String phoneNumber,
    required void Function(String verificationId) onCodeSent,
    required void Function(String message) onError,
    void Function(UserCredential credential)? onAutoVerified,
  }) async {
    await _auth.verifyPhoneNumber(
      phoneNumber: phoneNumber,
      timeout: const Duration(seconds: 60),
      verificationCompleted: (PhoneAuthCredential credential) async {
        try {
          final result = await _auth.signInWithCredential(credential);
          await _ensureUserDocument(result.user!, phoneNumber);
          onAutoVerified?.call(result);
        } on FirebaseAuthException catch (e) {
          onError(e.message ?? 'Automatic verification failed.');
        }
      },
      verificationFailed: (FirebaseAuthException e) {
        onError(e.message ?? 'Could not verify this phone number.');
      },
      codeSent: (String verificationId, int? resendToken) {
        onCodeSent(verificationId);
      },
      codeAutoRetrievalTimeout: (String verificationId) {},
    );
  }

  Future<UserCredential> submitOtp({
    required String verificationId,
    required String smsCode,
    required String phoneNumber,
  }) async {
    final credential = PhoneAuthProvider.credential(
      verificationId: verificationId,
      smsCode: smsCode,
    );
    final result = await _auth.signInWithCredential(credential);
    await _ensureUserDocument(result.user!, phoneNumber);
    return result;
  }

  Future<void> _ensureUserDocument(User user, String phoneNumber) async {
    final docRef = _usersRef.doc(user.uid);
    final snapshot = await docRef.get();
    if (!snapshot.exists) {
      final appUser = AppUser(
        uid: user.uid,
        agentId: user.uid,
        phoneNumber: phoneNumber,
        createdAt: DateTime.now(),
        trialStartedAt: DateTime.now(),
      );
      await docRef.set(appUser.toFirestoreCreate());
    }
  }

  Stream<AppUser?> watchCurrentUserProfile() {
    final uid = currentUser?.uid;
    if (uid == null) return Stream.value(null);
    return _usersRef.doc(uid).snapshots().map(
        (doc) => doc.exists ? AppUser.fromFirestore(doc) : null);
  }

  Future<void> recordPdpaConsent() async {
    final uid = currentUser?.uid;
    if (uid == null) return;
    await _usersRef.doc(uid).update({
      'pdpaConsentAt': FieldValue.serverTimestamp(),
    });
  }

  Future<void> signOut() => _auth.signOut();
}
