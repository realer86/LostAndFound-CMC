"""Static regression tests for the public reviews and homepage trust section."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
api = (root / 'backend/Controllers/ReviewsController.cs').read_text(encoding='utf-8')
ui = (root / 'frontend/src/components/TrustSection.jsx').read_text(encoding='utf-8')
app = (root / 'frontend/src/main.jsx').read_text(encoding='utf-8')
css = (root / 'frontend/src/components/safetyPage.css').read_text(encoding='utf-8')
program = (root / 'backend/Program.cs').read_text(encoding='utf-8')
sql = (root / 'backend/DatabaseScripts/09_public_reviews.sql').read_text(encoding='utf-8')

assert '[AllowAnonymous, HttpGet("public")]' in api
assert '[Authorize, HttpPost]' in api
assert '[Authorize, HttpGet("eligible")]' in api
assert 'room.Status == "COMPLETED"' in api
assert 'claim.HandedOverAt != null' in api
assert 'claim.ClaimantId == userId || report.UserId == userId' in api
assert 'r.HandoverRoomId == request.HandoverRoomId && r.ReviewerId == userId' in api
assert 'new { r.Rating, r.Comment, r.CreatedAt, r.ParticipantRole }' in api
assert 'api/reviews/public' in ui and "credentials: 'omit'" in ui
assert 'reviews.items.length === 0' in ui
assert 'Chưa có đánh giá' in ui
assert 'TrustSection me={me}' in app
assert app.index('<TrustSection me={me}') < app.index('<section className="container feed">')
assert '.safety-page>.kicker{display:block' in css
assert '09_public_reviews.sql' in program
assert 'CREATE TABLE IF NOT EXISTS "FoundlyPublicReviews"' in sql
assert 'CREATE UNIQUE INDEX IF NOT EXISTS' in sql
print('Public trust reviews: static contracts passed.')
