# Backlog

## Viewing date + move-in date + agenda screen — built

Resolved and implemented. Scope, as confirmed by the agent lead:

1. **Viewing date** is a single editable field (`Lead.viewingDate`), not a
   history — an agent re-sets it if a viewing is rescheduled or repeated.
   The lead stays in `open` status through this; no new status was added.
2. **Alerts** means a simple in-app list, not push notifications. Built as
   `AgendaScreen` (`lib/screens/agenda/agenda_screen.dart`), reachable from
   the dashboard AppBar (calendar icon). Buckets every lead's
   `nextFollowUpDate` / `viewingDate` / `moveInDate` into Overdue / Today /
   Tomorrow / This week / Later, computed client-side from data already
   loaded (`LeadProvider.agendaEntries`) — no new Firestore reads.
3. **Follow-up notes** reuse the existing `Activity` log — no new field.
4. **Move-in date** (`Lead.moveInDate`) added. No new `LeadStatus` values:
   agents mark a lead `closed` once the client has moved in, same as any
   other closed deal.

See `docs/DATA_MODEL.md` for the `viewingDate`/`moveInDate` field docs.
