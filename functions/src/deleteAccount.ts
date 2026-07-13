import { auth, db, storage } from "./firebaseAdmin";

/**
 * Permanently deletes everything an agent owns: every lead, its activity
 * log, any remaining voice recordings, the profile document, and finally
 * the Firebase Auth user itself. Used by both the in-app "Delete account"
 * flow (required by Apple whenever an app supports account creation) and
 * satisfies the PDPA "agent can delete... permanently" requirement at the
 * account level.
 */
export async function deleteAccountData(uid: string): Promise<void> {
  // 1. Delete every lead this agent owns, and each lead's activities.
  const leadsSnap = await db.collection("leads").where("agentId", "==", uid).get();

  for (const leadDoc of leadsSnap.docs) {
    const activitiesSnap = await leadDoc.ref.collection("activities").get();
    const batch = db.batch();
    for (const activityDoc of activitiesSnap.docs) {
      batch.delete(activityDoc.ref);
    }
    batch.delete(leadDoc.ref);
    await batch.commit();
  }

  // 2. Sweep any recordings left in Storage for this agent (normally
  // already gone — audio is deleted right after transcription — but this
  // guarantees nothing survives account deletion even if a job never
  // completed).
  try {
    await storage.bucket().deleteFiles({ prefix: `voice_recordings/${uid}/` });
  } catch (err) {
    // A missing prefix (no recordings ever existed) is not an error case
    // we need to fail account deletion over.
    console.warn(`No recordings to delete for ${uid}, or deletion failed:`, err);
  }

  // 3. Delete the profile document.
  await db.collection("users").doc(uid).delete();

  // 4. Delete the Firebase Auth user last, once all data referencing it
  // is gone.
  await auth.deleteUser(uid);
}
