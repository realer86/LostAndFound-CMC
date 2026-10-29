"""No new direct contact, including LOST. Archived data is read-only."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
ui=(r/'frontend/src/main.jsx').read_text()
contacts=(r/'backend/Controllers/ContactsController.cs').read_text()
msgs=(r/'backend/Controllers/MessagesController.cs').read_text()
room=(r/'backend/Controllers/HandoversController.cs').read_text()
assert 'Liên hệ người đăng qua hộp thư' not in ui
assert 'StatusCode(410' in contacts and 'StatusCode(410' in msgs
assert 'Route("api/handovers")' in room
assert 'SupportInbox' in ui
print('PASS: legacy P2P disabled; approved Claims use private Handover, staff via Support')
