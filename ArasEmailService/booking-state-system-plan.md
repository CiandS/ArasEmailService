We are extending an existing .NET 8 Console App (ArasEmailService) that currently:

- Calls MotoPress Hotel Booking REST API
- Pulls bookings
- Sends emails via SMTP (MailKit)
- Logs sent emails in MySQL to prevent duplicates

DO NOT refactor or rewrite the existing architecture.
DO NOT replace existing services.
DO NOT change folder structure unless absolutely necessary.

We are adding a new feature: UID-based booking state tracking for operational alerts.

────────────────────────────

🎯 GOAL

Introduce a lightweight booking state tracking layer using UID to support:

1. FirstSeenUtc tracking (when booking first appears in API)
2. LastSeenUtc tracking (each sync pass)
3. Change detection via snapshot hash
4. Late booking detection flag:
   - check-in within 72 hours
   - AND FirstSeenUtc within 24 hours

────────────────────────────

🧱 IMPLEMENTATION REQUIREMENTS

1. Add a new MySQL table ONLY if needed:
   wp_booking_uid_log

2. Create a new service:

BookingStateService.cs

Responsibilities:
- Compare API bookings against UID table
- Insert new UID records
- Update existing records
- Calculate snapshot hash (based on check-in, check-out, accommodation, guest count)
- Set flags (LateBookingFlag, ModifiedFlag)

3. Modify EXISTING booking sync flow:

After fetching bookings from MotoPress API:
- Pass results into BookingStateService
- DO NOT change API client logic
- DO NOT change email service logic yet

4. Ensure idempotency:
- Running sync multiple times must not duplicate records or emails

5. Add minimal helper model:

BookingStateModel:
- UID
- CheckIn
- CheckOut
- AccommodationId
- GuestName
- SnapshotHash

────────────────────────────

🚫 STRICT CONSTRAINTS

- Do NOT create a new project
- Do NOT introduce new frameworks
- Do NOT replace MailKit email logic
- Do NOT implement Azure Functions yet
- Do NOT over-engineer (no CQRS, no MediatR unless already used)
- Keep it console-app friendly

────────────────────────────

🧠 ACCEPTANCE CRITERIA

- Existing email system still works unchanged
- New UID table is populated correctly
- FirstSeenUtc is correctly captured
- Duplicate sync runs do not create duplicates
- System logs state changes without breaking current flow

Start by implementing:
1. SQL table
2. BookingStateService
3. Minimal integration into existing sync loop