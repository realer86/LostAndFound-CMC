# Manual acceptance test matrix (localhost)

| ID | Preconditions | Action | Expected |
|---|---|---|---|
| M01 | Open LOST and FOUND | Similar name/time/location | Matching recommendations only opposite types |
| M02 | Two users + 1 FOUND | Both claim; finder approves first | Second cannot be approved while first APPROVED |
| M03 | Claim approved; no physical confirmation | Claimant withdraws / finder revokes | Room cancelled; next claim can be approved |
| M04 | Appointment accepted | Either person cancels | Appointment CANCELLED; room returns ARRANGING; notification to other |
| M05 | Accepted time has passed | User files NO_SHOW | Room DISPUTED; Admin can review; both cannot confirm |
| M06 | Disputed room | Admin RESUME_ROOM with reason | Room resumes former safe state; audit and notifications created |
| M07 | Disputed room, no physical confirmation | Admin CANCEL_HANDOVER | Claim + room cancelled; another claim can be reviewed |
| M08 | Someone reports a public post | Admin HIDE_REPORT | Post hidden from public feed; cannot silently hide an ongoing handover |
| M09 | User account | Admin suspends 7 days | Mutating API calls return 403; Admin can lift suspension |
| M10 | FOUND open | Finder requests safe desk custody | Admin sees pending request; no confirmation until staff actually receives |
| M11 | Held in custody | Finder attempts to confirm physical handover | 409; Admin RELEASE after accepted claim and appointment; claimant confirms |
| M12 | Post expired (test DB clock) | Owner renews | OPEN again for 30 days; MatchEngine reevaluates |
| M13 | Upcoming appointment | Background process within 1h | Both users get one REMINDER; not duplicated in subsequent scans |
| M14 | Two-sided handover | Holder confirms only | Still not RETURNED until claimant confirms |
| M15 | All portals responsive | Browser 320px, 375px, 768px, 1366px | No unusable clipped forms, actions visible or scrollable |
| M16 | Existing old database in volume | Rebuild/restart new Compose | Existing posts, claims, images, handover messages still queryable |

Note: this matrix is for **manual execution on a machine with Docker**. Passing source syntax checks is not equivalent to passing these runtime tests.
