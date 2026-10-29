"""Live support UI is wired to server-side case/custody conversations."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
main=(r/'frontend/src/main.jsx').read_text()
view=(r/'frontend/src/components/SupportInbox.jsx').read_text()
backend=(r/'backend/Controllers/SupportController.cs').read_text()
admin=(r/'admin/src/main.jsx').read_text()
assert "<SupportInbox key=" in main and "user={me}" in main
assert 'initialReport={chatReport}' not in main
assert "request('/support/threads')" in view
assert "request(`/support/threads/${active}/messages`" in view
assert "'POST'" in view and 'credentials:' in view
assert 'Route("api/support")' in backend
assert 'Route("api/admin/support")' in backend
assert 'SupportAdminInbox' in admin
print('PASS: official support API and Admin inbox are wired')
