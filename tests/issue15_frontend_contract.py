"""Source-level integration contract checks (does not replace a browser / Docker E2E test)."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
api = (root / 'frontend/src/components/ChatLogic.jsx').read_text(encoding='utf-8')
main = (root / 'frontend/src/main.jsx').read_text(encoding='utf-8')
view = (root / 'frontend/src/components/Sprint2ContactsView.jsx').read_text(encoding='utf-8')
backend = (root / 'backend/Controllers/MessagesController.cs').read_text(encoding='utf-8')
contacts = (root / 'backend/Controllers/ContactsController.cs').read_text(encoding='utf-8')
directory = (root / 'backend/Controllers/DirectoryController.cs').read_text(encoding='utf-8')

assert 'http://localhost:5000' not in api
assert '/api/chats' not in api
assert 'createRoot(document.getElementById(\'root\')).render(<App/>)' in main
assert 'render(<><App/><ChatLogic/></>)' not in main
assert 'live={chat}' in main and 'initialReport={chatReport}' in main
assert 'useChatLogic' in main and '<Sprint2ContactsView' in main
assert "chatApi('/contacts'" in api
assert "chatApi('/directory'" in api
assert "chatApi('/messages'" in api
assert '`/messages?contactId=${encodeURIComponent(activeId)}&page=1&pageSize=50`' in api
assert 'JSON.stringify({ reportId })' in api
assert 'JSON.stringify({ contactId: activeId, content: text })' in api
assert "credentials: 'same-origin'" in api
assert 'Array.isArray(response.items)' in api
assert 'Array.isArray(data.items)' in api
assert 'm.senderId === live.user?.id' in view
assert 'await live.sendMessage(text)' in view
assert 'live?.messageError' in view
assert 'Bản demo không gửi tin nhắn đến người dùng thật' in view
assert 'fetch(' not in view
assert '[HttpPost]' in backend and '[HttpGet]' in backend and 'Route("api/messages")' in backend
assert 'OpenContactRequest(Guid ReportId)' in contacts
assert 'SendChatMessageRequest(Guid ContactId, string? Content)' in backend
assert 'Route("api/directory")' in directory
print('PASS: Issue #15 uses real #13 endpoints, preserves #14 view/demo, removes duplicate root')
