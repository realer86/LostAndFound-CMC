"""Current support UI contract. Historical Sprint 2 P2P UI remains in Git history only."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
main=(r/'frontend/src/main.jsx').read_text()
view=(r/'frontend/src/components/SupportInbox.jsx').read_text()
css=(r/'frontend/src/components/supportInbox.css').read_text()
assert "<SupportInbox key=" in main and "user={me}" in main
assert 'Liên hệ người đăng qua hộp thư' not in main
assert 'Phòng bàn giao' in view and 'Hộp thư hỗ trợ' in view
assert '/support/threads' in view and '/directory' in view
assert '.support-chat-layout' in css and '@media(max-width:700px)' in css
print('PASS: official support inbox and campus directory replace user-to-user chat')
