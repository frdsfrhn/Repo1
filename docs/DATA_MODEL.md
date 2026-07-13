# Data model (v1, locked)

This mirrors the schema in `lib/models/` and the security rules in
`firestore.rules`. Treat this as the source of truth alongside the code —
changing it after agents start using the app is the expensive kind of
rework the original requirements doc warned about.

## One assumption worth flagging

The requirements doc says commission is "percentage + computed value" but
doesn't name the base amount the percentage applies to. A computed value
needs a base, so this build adds **`dealValue`** (purchase price for a
purchase deal, or the rental value for a rent deal) as a required lead
field. `commissionValue = dealValue * commissionPercent / 100`. Confirm
this matches how agents actually think about it — some agencies compute
rental commission as a multiple of monthly rent rather than a flat
percentage of an annual figure, which would change the formula, not just
the field.

## Firestore layout

```
/users/{uid}                        one doc per agent, keyed by their Firebase Auth uid
/leads/{leadId}                     agentId field scopes every query + security rule
  /activities/{activityId}          agentId denormalized here too (see rules comment)
```

### `users/{uid}`

| Field | Type | Notes |
|---|---|---|
| `agentId` | string | Always equal to the document id / auth uid |
| `phoneNumber` | string | E.164 format, from phone auth |
| `pdpaConsentAt` | timestamp \| null | Null until the agent accepts the consent screen; app blocks access until set |
| `createdAt` | timestamp | |
| `trialStartedAt` | timestamp | Set at account creation; trial window = `trialStartedAt + AppConstants.trialLengthDays` |
| `subscriptionStatus` | `"trial" \| "active" \| "expired"` | Kept in sync by the RevenueCat webhook (`functions/src/revenuecatWebhook.ts`); the trial window is also checked independently client- and server-side so nothing depends on webhook latency |

### `leads/{leadId}`

| Field | Type | Notes |
|---|---|---|
| `agentId` | string | Owner — enforced by `firestore.rules` |
| `prospectName` | string | |
| `phoneNumber` | string | |
| `propertyName` | string | |
| `propertyAddress` | string | Optional |
| `telegramUrl` | string | Optional — full URL (e.g. `https://t.me/username`), opened directly via `url_launcher` from lead detail |
| `dealType` | `"rent" \| "purchase"` | Single toggle, not free text, per spec |
| `commissionPercent` | number | e.g. `3.0` for 3% |
| `dealValue` | number | See "assumption" above |
| `expectedYieldPercent` | number \| null | Purchase leads only — cleared automatically when `dealType` is `rent` |
| `nextFollowUpDate` | timestamp \| null | Drives dashboard sort order |
| `status` | `"open" \| "follow_up_needed" \| "closed"` | |
| `createdAt` / `updatedAt` | timestamp | |

### `leads/{leadId}/activities/{activityId}`

| Field | Type | Notes |
|---|---|---|
| `agentId` | string | Denormalized from the parent lead so the security rule on this subcollection doesn't need a `get()` lookup on every read |
| `type` | `"voice" \| "typed"` | |
| `timestamp` | timestamp | |
| `transcriptOriginal` | string | Raw STT output for voice notes; equals `transcriptCleaned` for typed notes |
| `transcriptCleaned` | string | AI-cleaned version (voice notes) — see `functions/src/cleanupTranscript.ts` |
| `transcriptionStatus` | `"none" \| "pending" \| "cleaned" \| "failed"` | UI watches this field to show a spinner / retry action |
| `audioUrl` | string \| null | `gs://…` path; set on upload, nulled out once transcription succeeds |
| `audioDeletedAt` | timestamp \| null | Set when the Cloud Function deletes the raw audio (PDPA data minimization) |

## Why audio gets deleted automatically

`functions/src/index.ts` (`processVoiceNote`) deletes the Storage object as
soon as it has a transcript, then clears `audioUrl`. This is the "store
voice recordings only as long as needed for transcription" requirement —
it's not a manual cleanup job, it's part of the same function call that
produces the transcript, so there's no window where a stale recording
survives because a cleanup job didn't run.

## Why security rules check `agentId`, not just app logic

`firestore.rules` requires `request.auth.uid == resource.data.agentId` (or
`request.resource.data.agentId` on create) for every read/write on
`leads` and `activities`. This is enforced at the database level — a bug
in the Flutter query logic can't leak another agent's data, because the
server rejects the read/write regardless of what the client asked for.
