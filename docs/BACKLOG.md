# Backlog — not yet built

Feature requests captured for a future session, not yet implemented.
None of these were in the original locked data model — same "flag it,
don't silently redefine the spec" approach as `ownerName`/`ownerPhone`
and `propertyImageUrl` earlier.

## Viewing date + move-in date (calendar-pick fields)

- Two new optional `Lead` fields: `viewingDate` and `moveInDate`, both
  `DateTime?`, both editable via `showDatePicker` in the lead form —
  same pattern as the existing `nextFollowUpDate` picker in
  `lib/screens/lead/lead_form_screen.dart`.
- Show both on lead detail alongside the existing date rows.
- Open question for next session: does "viewing date" mean a single
  scheduled viewing, or does an agent need a *history* of multiple
  viewings per lead (re-viewings are common)? A single field only
  supports the former. Confirm before building — changes whether this
  is a `Lead` field or a new sub-collection like `activities`.

## Simple today/tomorrow alerts, driven by `nextFollowUpDate`

- A lightweight in-app section (dashboard banner or a dedicated
  "Today" view) surfacing leads whose `nextFollowUpDate` is today or
  tomorrow — reuses `Formatters.relativeDueDate()` and the overdue
  logic already in `LeadListTile` (`lib/screens/dashboard/widgets/lead_list_tile.dart`),
  just needs a filtered view rather than new date logic.
- "Alerts" — clarify scope before building: an in-app list/banner is
  simple (no new infra). Actual push notifications are a different,
  much bigger scope (needs FCM setup, background delivery, permission
  prompts) — confirm which one is meant before starting.

## Follow-up note

- A note attached to the follow-up date/action itself — likely a new
  optional text field (e.g. `nextFollowUpNote`) rather than reusing the
  existing `Activity` log, since activities are timestamped history
  entries, not a single "what to do next" field. Confirm the intended
  shape: one note per lead (simple field) vs. a note per follow-up
  occurrence (implies leads can have multiple scheduled follow-ups,
  which the current single-`nextFollowUpDate` model doesn't support).

## Open questions to resolve before starting

1. Viewing date: single occurrence or history of multiple viewings?
2. Alerts: in-app list/banner, or real push notifications (FCM)?
3. Follow-up note: single field per lead, or does this imply leads need
   multiple follow-ups over time (bigger data model change)?
