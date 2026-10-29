"""Static regression gates; not a substitute for live Docker HTTP permission tests."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
api=(r/'backend/Controllers/SupportController.cs').read_text()
ops=(r/'backend/Controllers/OperationsController.cs').read_text()
sql=(r/'backend/DatabaseScripts/07_support_channels.sql').read_text()
legacy=(r/'backend/Controllers/MessagesController.cs').read_text()
main=(r/'frontend/src/main.jsx').read_text()
admin=(r/'admin/src/SupportAdminInbox.jsx').read_text()
assert 'db.SupportThreads.Add' in ops and 'CaseId=ticket.Id' in ops and 'CustodyId=record.Id' in ops
assert 'x.Id==id && x.UserId==owner' in api
assert 'IsStaff' not in api.split('// Restricted backoffice inbox.')[0], 'User support controller must not depend on admin-only methods'
assert 'Roles="ADMIN,STAFF"' in api and 'AuthenticationSchemes="AdminCookie"' in api
assert 'query=query.Where(x=>x.Department==ownDepartment)' in api
assert 'thread.Department!=await AssignedDepartment()' in api
assert 'SenderType=IsStaff?"STAFF":"ADMIN"' in api
assert 'Roles="ADMIN"' in (r/'backend/Controllers/AdminController.cs').read_text()
assert 'Roles="ADMIN"' in (r/'backend/Controllers/AdminOperationsController.cs').read_text()
assert 'AssignDepartment' in (r/'backend/Controllers/AdminOperationsController.cs').read_text()
assert 'ThreadId' in sql and 'REFERENCES "Users"' in sql and 'ON CONFLICT DO NOTHING' in sql
assert 'DROP TABLE' not in sql and 'TRUNCATE' not in sql
assert 'StatusCode(410' in legacy
assert 'onChat' not in main and 'SupportInbox' in main
assert 'Nhân viên phòng ban' in admin and 'Admin' in admin
print('PASS: support channels case/custody scoped and legacy contacts not writable')
