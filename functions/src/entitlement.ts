import { HttpsError } from "firebase-functions/v2/https";
import { db, TRIAL_LENGTH_DAYS } from "./firebaseAdmin";

/**
 * Server-side re-check of the AI-features entitlement, mirroring
 * lib/providers/subscription_provider.dart on the client. This is the
 * real enforcement point for the app's AI cost driver (voice
 * transcription + cleanup) — the client-side gate exists for UX, not
 * security, since a modified client could skip it.
 */
export async function assertAiFeaturesEntitled(uid: string): Promise<void> {
  const userSnap = await db.collection("users").doc(uid).get();
  if (!userSnap.exists) {
    throw new HttpsError("not-found", "Agent profile not found.");
  }
  const data = userSnap.data()!;
  const status = (data.subscriptionStatus as string | undefined) ?? "trial";

  if (status === "active") return;
  if (status === "expired") {
    throw new HttpsError(
      "failed-precondition",
      "Your subscription has ended. Subscribe to keep using AI voice features."
    );
  }

  // status === "trial" — check the trial window itself, since the
  // RevenueCat webhook only flips this to "expired" asynchronously.
  const trialStartedAt = data.trialStartedAt?.toDate?.() as Date | undefined;
  const trialStart = trialStartedAt ?? new Date();
  const trialEnd = new Date(trialStart);
  trialEnd.setDate(trialEnd.getDate() + TRIAL_LENGTH_DAYS);

  if (new Date() > trialEnd) {
    throw new HttpsError(
      "failed-precondition",
      "Your free trial has ended. Subscribe to keep using AI voice features."
    );
  }
}
