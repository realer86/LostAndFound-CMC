"""Lightweight static matching contract checks (no Docker/.NET required).

For behavioral/integration verification, run the manual scenarios in docs/MATCHING.md.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
service = (ROOT / 'backend/Services/ReportMatchingService.cs').read_text(encoding='utf-8')
controller = (ROOT / 'backend/Controllers/MatchingController.cs').read_text(encoding='utf-8')
notifier = (ROOT / 'backend/Services/MatchNotificationService.cs').read_text(encoding='utf-8')
notifications = (ROOT / 'backend/Controllers/NotificationsController.cs').read_text(encoding='utf-8')
frontend = (ROOT / 'frontend/src/main.jsx').read_text(encoding='utf-8')
panel = (ROOT / 'frontend/src/components/MatchingPanel.jsx').read_text(encoding='utf-8')

checks = {
    'Opposite report types only': 'source.Type == "LOST" && other.Type == "FOUND"' in service
        and 'source.Type == "FOUND" && other.Type == "LOST"' in service,
    'Both must be OPEN': 'source.Status != "OPEN" || other.Status != "OPEN"' in service,
    'Never match own reports': 'source.UserId == other.UserId' in service,
    'Heuristic normalizes Vietnamese': "ch == 'đ' ? 'd'" in service,
    '50 and 80 threshold': 'finalScore >= 80' in service and 'finalScore >= 50' in service,
    'Authentication required': '[Authorize]' in controller,
    'Owner check': 'source.UserId != userId' in controller,
    'Capped candidates': '.Take(1000)' in controller,
    'No private identifier returned': 'PrivateIdentifier' not in controller,
    'Endpoint aliases': 'api/items/{id:guid}/matches' in controller and 'api/reports/{id:guid}/matches' in controller,
    'Matching nav enabled on main': 'Gợi ý phù hợp{fullBackend&&matchingUnread>0' in frontend,
    'Matching page renders real results': '<MatchingPanel mine={mine}' in frontend and 'match.reasons.map' in panel,
    'REQ-04 does not become enabled': "claims: { title: 'Yêu cầu nhận đồ'" in frontend,
    'No Google auth code required': 'GoogleSignInButton' not in frontend,
    'Match events persist independently': '"MATCH"' in notifier and 'RelatedId' in notifier and 'ON CONFLICT' in notifier,
    'New report detection notifies both report owners': 'FindMatchesAsync' in notifier and 'RecordAsync(db, source, matches' in (ROOT / 'backend/Controllers/ItemsController.cs').read_text(encoding='utf-8'),
    'Unread match count has dedicated endpoint': '[HttpGet("matches/unread")]' in notifications and 'x.Type=="MATCH"' in notifications,
    'Viewing matches marks only matching events read': 'matches/{reportId:guid}/read' in notifications and 'x.Type=="MATCH" && x.ReportId==reportId' in notifications,
    'Matching badge independent from message badge': 'matchingUnread>0' in frontend and 'notifications.some(x=>x.type===\'MESSAGE\'' in frontend,
}
for name, passed in checks.items():
    print(f"{'PASS' if passed else 'FAIL'} {name}")
if not all(checks.values()):
    raise SystemExit(1)
print(f'PASS {len(checks)} static checks (not a runtime API test)')
