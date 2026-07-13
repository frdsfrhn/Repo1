import { setGlobalOptions } from "firebase-functions/v2";
import { onCall, onRequest, HttpsError } from "firebase-functions/v2/https";
import { defineSecret } from "firebase-functions/params";

import { db, storage } from "./firebaseAdmin";
import { assertAiFeaturesEntitled } from "./entitlement";
import { transcribeAudio } from "./transcribeAudio";
import { cleanupTranscript } from "./cleanupTranscript";
import { deleteAccountData } from "./deleteAccount";
import { handleRevenueCatWebhook } from "./revenuecatWebhook";

setGlobalOptions({ region: "asia-southeast1", maxInstances: 10 });

const anthropicApiKey = defineSecret("ANTHROPIC_API_KEY");
const revenueCatWebhookAuth = defineSecret("REVENUECAT_WEBHOOK_AUTH");

/**
 * Runs the full voice-note pipeline for one activity document: speech-to-
 * text, AI cleanup, then deletes the raw audio (PDPA data minimization —
 * only the transcript is kept after this point).
 *
 * The client (TranscriptionService.processVoiceNote) has already created
 * the activity doc with transcriptionStatus: "pending" and an audioUrl
 * pointing at the just-uploaded recording before calling this.
 */
export const processVoiceNote = onCall(
  { secrets: [anthropicApiKey] },
  async (request) => {
    const uid = request.auth?.uid;
    if (!uid) throw new HttpsError("unauthenticated", "Sign in required.");

    const { leadId, activityId } = request.data as {
      leadId?: string;
      activityId?: string;
    };
    if (!leadId || !activityId) {
      throw new HttpsError("invalid-argument", "leadId and activityId are required.");
    }

    await assertAiFeaturesEntitled(uid);

    const activityRef = db
      .collection("leads")
      .doc(leadId)
      .collection("activities")
      .doc(activityId);
    const activitySnap = await activityRef.get();
    if (!activitySnap.exists) {
      throw new HttpsError("not-found", "Activity not found.");
    }
    const activity = activitySnap.data()!;
    if (activity.agentId !== uid) {
      throw new HttpsError("permission-denied", "This isn't your lead.");
    }
    const audioUrl = activity.audioUrl as string | undefined;
    if (!audioUrl) {
      throw new HttpsError("failed-precondition", "No audio to process.");
    }

    try {
      const originalTranscript = await transcribeAudio(audioUrl);
      const cleanedTranscript = await cleanupTranscript(originalTranscript);

      await activityRef.update({
        transcriptOriginal: originalTranscript,
        transcriptCleaned: cleanedTranscript,
        transcriptionStatus: "cleaned",
      });

      // Delete the raw audio now that we have the transcript — PDPA data
      // minimization requirement (see MVP checklist §5).
      const bucketMatch = /^gs:\/\/([^/]+)\/(.+)$/.exec(audioUrl);
      if (bucketMatch) {
        await storage
          .bucket(bucketMatch[1])
          .file(bucketMatch[2])
          .delete({ ignoreNotFound: true });
      }
      await activityRef.update({
        audioUrl: null,
        audioDeletedAt: new Date(),
      });
    } catch (err) {
      console.error(`processVoiceNote failed for ${leadId}/${activityId}:`, err);
      await activityRef.update({ transcriptionStatus: "failed" });
      throw new HttpsError("internal", "Transcription failed. You can retry from the note.");
    }
  }
);

/**
 * Retries just the AI-cleanup step against the existing original
 * transcript. If the original transcript is empty (speech-to-text itself
 * never produced anything, e.g. STT failed before cleanup ran), retries
 * the full pipeline instead — audio may still be present in that case.
 */
export const retryTranscriptCleanup = onCall(
  { secrets: [anthropicApiKey] },
  async (request) => {
    const uid = request.auth?.uid;
    if (!uid) throw new HttpsError("unauthenticated", "Sign in required.");

    const { leadId, activityId } = request.data as {
      leadId?: string;
      activityId?: string;
    };
    if (!leadId || !activityId) {
      throw new HttpsError("invalid-argument", "leadId and activityId are required.");
    }

    await assertAiFeaturesEntitled(uid);

    const activityRef = db
      .collection("leads")
      .doc(leadId)
      .collection("activities")
      .doc(activityId);
    const activitySnap = await activityRef.get();
    if (!activitySnap.exists) {
      throw new HttpsError("not-found", "Activity not found.");
    }
    const activity = activitySnap.data()!;
    if (activity.agentId !== uid) {
      throw new HttpsError("permission-denied", "This isn't your lead.");
    }

    await activityRef.update({ transcriptionStatus: "pending" });

    try {
      const existingOriginal = (activity.transcriptOriginal as string) ?? "";
      let originalTranscript = existingOriginal;

      if (originalTranscript.trim().length === 0 && activity.audioUrl) {
        originalTranscript = await transcribeAudio(activity.audioUrl as string);
      }

      const cleanedTranscript = await cleanupTranscript(originalTranscript);

      await activityRef.update({
        transcriptOriginal: originalTranscript,
        transcriptCleaned: cleanedTranscript,
        transcriptionStatus: "cleaned",
      });
    } catch (err) {
      console.error(`retryTranscriptCleanup failed for ${leadId}/${activityId}:`, err);
      await activityRef.update({ transcriptionStatus: "failed" });
      throw new HttpsError("internal", "Cleanup failed again. Please try later.");
    }
  }
);

/** In-app account deletion — see functions/src/deleteAccount.ts. */
export const deleteAccount = onCall(async (request) => {
  const uid = request.auth?.uid;
  if (!uid) throw new HttpsError("unauthenticated", "Sign in required.");
  await deleteAccountData(uid);
});

/**
 * RevenueCat server-to-server webhook. Configure the same secret value as
 * an "Authorization" header (e.g. "Bearer <random-string>") in the
 * RevenueCat dashboard's webhook settings — see README "RevenueCat setup".
 */
export const revenuecatWebhook = onRequest(
  { secrets: [revenueCatWebhookAuth] },
  async (req, res) => {
    await handleRevenueCatWebhook(req, res, revenueCatWebhookAuth.value());
  }
);
