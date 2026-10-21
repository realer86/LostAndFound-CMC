"""Offline, source-only smoke checks. Rendering and responsiveness still require browser QA."""
from pathlib import Path
repo = Path(__file__).resolve().parents[1]
jsx = (repo / 'frontend/src/components/Sprint2ContactsView.jsx').read_text(encoding='utf-8')
css = (repo / 'frontend/src/components/sprint2Contacts.css').read_text(encoding='utf-8')
app = (repo / 'frontend/src/main.jsx').read_text(encoding='utf-8')
assert 'Liên hệ người đăng' in app and 'Liên hệ & Danh bạ' in app
assert '<Sprint2ContactsView' in app and 'onChat' in app
assert 'Bản demo không gửi tin nhắn đến người dùng thật' in jsx
assert 'role="tab"' in jsx and 'aria-label=' in jsx
assert 's2-bubble' in css and '.mine' in css and '.theirs' in css
assert '@media(max-width:760px)' in css and '@media(max-width:430px)' in css
assert "fetch(" not in jsx and "'/api" not in jsx
print('PASS: Issue #14 static navigation, chat/directory UI, mock-only, CSS responsive breakpoints')
