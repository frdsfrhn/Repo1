import 'dart:async';

import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';

import '../models/app_user.dart';
import '../services/auth_service.dart';
import '../services/subscription_service.dart';

/// Tracks Firebase auth state plus the agent's Firestore profile
/// (consent status, trial/subscription status) as one observable stream
/// the router and screens can key off of.
///
/// Named `AppAuthProvider`, not `AuthProvider` — `package:firebase_auth`
/// already exports a public class literally called `AuthProvider` (the
/// federated-auth-provider base type), and importing both in the same
/// file is a real, common collision otherwise.
class AppAuthProvider extends ChangeNotifier {
  AppAuthProvider({
    required AuthService authService,
    required SubscriptionService subscriptionService,
  })  : _authService = authService,
        _subscriptionService = subscriptionService {
    _authSub = _authService.authStateChanges().listen(_onAuthChanged);
  }

  final AuthService _authService;
  final SubscriptionService _subscriptionService;

  StreamSubscription<User?>? _authSub;
  StreamSubscription<AppUser?>? _profileSub;

  User? _firebaseUser;
  AppUser? _profile;
  bool _isLoading = true;

  User? get firebaseUser => _firebaseUser;
  AppUser? get profile => _profile;
  bool get isLoading => _isLoading;
  bool get isSignedIn => _firebaseUser != null;
  bool get hasGivenConsent => _profile?.hasGivenConsent ?? false;

  AuthService get authService => _authService;

  Future<void> _onAuthChanged(User? user) async {
    _firebaseUser = user;
    _profileSub?.cancel();

    if (user == null) {
      _profile = null;
      _isLoading = false;
      _subscriptionService.logOut();
      notifyListeners();
      return;
    }

    // Keep _isLoading true (splash screen showing) until the profile
    // document is guaranteed to exist — otherwise the UI can briefly see
    // "signed in, no profile yet" and misread that as "consent not given",
    // routing to the consent screen before its own accept-consent write
    // would even be valid. See AuthService.ensureUserDocument. No
    // notifyListeners() happens until after this, so this await is
    // invisible to the UI either way.
    await _authService.ensureUserDocument(user);

    _subscriptionService.logIn(user.uid);
    _profileSub = _authService.watchCurrentUserProfile().listen((profile) {
      _profile = profile;
      _isLoading = false;
      notifyListeners();
    });
  }

  Future<void> acceptPdpaConsent() => _authService.recordPdpaConsent();

  Future<void> signOut() => _authService.signOut();

  @override
  void dispose() {
    _authSub?.cancel();
    _profileSub?.cancel();
    super.dispose();
  }
}
