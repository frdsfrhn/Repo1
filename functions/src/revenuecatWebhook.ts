import { Request } from "firebase-functions/v2/https";
import { Response } from "express";
import { db } from "./firebaseAdmin";

// Event types RevenueCat sends — see
// https://www.revenuecat.com/docs/integrations/webhooks/event-types-and-fields
const ACTIVE_EVENT_TYPES = new Set([
  "INITIAL_PURCHASE",
  "RENEWAL",
  "UNCANCELLATION",
  "PRODUCT_CHANGE",
]);
const EXPIRED_EVENT_TYPES = new Set(["EXPIRATION"]);

interface RevenueCatEvent {
  type: string;
  app_user_id: string;
}

/**
 * Keeps users/{uid}.subscriptionStatus in sync with RevenueCat's view of
 * the world, so the rest of the app (and the Cloud Functions that gate AI
 * work) can read subscription state from Firestore without an SDK round
 * trip. `app_user_id` is the Firebase uid because the client calls
 * Purchases.logIn(uid) right after Firebase sign-in — see
 * SubscriptionService.logIn / AuthProvider.
 *
 * CANCELLATION is deliberately not treated as "expired" here: a
 * cancelled subscription still has access until its current period ends,
 * at which point RevenueCat sends EXPIRATION.
 */
export async function handleRevenueCatWebhook(
  req: Request,
  res: Response,
  expectedAuthHeader: string
): Promise<void> {
  const authHeader = req.get("Authorization");
  if (!expectedAuthHeader || authHeader !== expectedAuthHeader) {
    res.status(401).send("Unauthorized");
    return;
  }

  const event = req.body?.event as RevenueCatEvent | undefined;
  if (!event?.app_user_id || !event.type) {
    res.status(400).send("Malformed event");
    return;
  }

  const uid = event.app_user_id;
  let newStatus: "active" | "expired" | null = null;
  if (ACTIVE_EVENT_TYPES.has(event.type)) {
    newStatus = "active";
  } else if (EXPIRED_EVENT_TYPES.has(event.type)) {
    newStatus = "expired";
  }

  if (newStatus) {
    await db.collection("users").doc(uid).set(
      { subscriptionStatus: newStatus },
      { merge: true }
    );
  }

  res.status(200).send("ok");
}
