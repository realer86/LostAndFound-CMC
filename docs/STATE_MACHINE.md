# Business state rules — Foundly Full Operations

## ItemReport
- OPEN: visible/searchable, matching active, may be edited unless found-item has APPROVED claim or is held in custody.
- CLOSED: manually closed; Admin may reopen if no pending approved handover.
- EXPIRED: the scheduler ages OPEN posts after 30 days (paused while handover/custody is active). Owner can extend 30 days.
- HIDDEN: Admin hid post after moderating abuse report; Admin can reopen.
- RETURNED: terminal successful handover, cannot reopen.

## ClaimRequest
- PENDING → APPROVED or REJECTED.
- PENDING → CANCELLED: claimant withdraws.
- APPROVED → CANCELLED: claimant withdraws / holder revokes *before* either party confirms physical handover, or Admin cancels a dispute after review.
- Only one APPROVED claim per FOUND post is allowed by the review endpoint's serializable transaction and query check. Others remain PENDING until that is revoked or finished.

## HandoverRoom
- ARRANGING: both participants can message, propose/decline appointments.
- SCHEDULED: both parties have accepted a future appointment.
- AWAITING_CONFIRMATION: first participant or verified staff has recorded a physical handover; awaiting claimant confirmation.
- COMPLETED: two-sided acknowledgement; paired FOUND becomes RETURNED.
- DISPUTED: NO_SHOW or HANDOVER_DISPUTE opened; all mutations paused until Admin RESUME_ROOM / CANCEL_HANDOVER / DISMISS.
- CANCELLED: claim approval withdrawn/revoked or Admin cancelled a disputed handover before physical confirmation; terminal.

## Appointments
PROPOSED → ACCEPTED / DECLINED / CANCELLED / SUPERSEDED. Accepted appointments may be cancelled or replaced before physical confirmation. A background process reminds both parties once within an hour of an upcoming ACCEPTED meeting.

## Official custody
REQUESTED → RECEIVED (only Admin after physically receiving, with receipt code) or REJECTED.
RECEIVED → RELEASED (only Admin after verifying real release to an APPROVED claimant, with a mutually accepted appointment).
While RECEIVED the finder cannot self-confirm handover. After RELEASED the claimant must still confirm receipt in his/her room to complete the transaction.

## Case moderation
OPEN → DISMISSED / RESOLVED. User cannot resolve own reports. Admin may HIDE_REPORT for content, RESUME_ROOM or CANCEL_HANDOVER for dispute/NO_SHOW.

## Privacy
- Private evidence shown only to owner, claimant, and admin in relevant claim routes.
- Chat content readable only to the two room participants, not displayed to Admin.
- Full email and phone never displayed as public room profile.
- Admin actions produce audit log events.
