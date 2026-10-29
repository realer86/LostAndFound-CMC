"""Source-level smoke checks. Does not replace HTTP, .NET or PostgreSQL integration tests."""
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(f):return (R/f).read_text(encoding='utf-8')
def test_console():
    compose=read('compose.yaml')
    assert 'admin:' in compose and '5174:5174' in compose
    assert 'frontend:' not in compose or 'web:' in compose
    program=read('backend/Program.cs')
    assert 'AdminCookie' in program and '06_operations.sql' in program
    assert '5174' in program and 'AdminCookie' in read('backend/Controllers/AdminOperationsController.cs')
    controller=read('backend/Controllers/AdminController.cs')
    assert 'AuthenticationSchemes="AdminCookie"' in controller
    assert 'DbSet' not in controller or 'db.Matches' not in controller
    for path in ('api/admin/stats','api/admin/reports','api/admin/users'):
        assert path.split('/')[-1] in controller
    auth=read('backend/Controllers/AdminAuthController.cs')
    assert 'Role!="ADMIN"' in auth and 'AdminCookie' in auth and 'EmailVerifiedAt' in auth
    assert 'Admin12345!' not in read('admin/src/main.jsx')
    sql=read('backend/DatabaseScripts/06_operations.sql')
    for name in ('ReportClaims','HandoverRooms','HandoverMessages','HandoverAppointments','CustodyRecords','Notifications','AuditLogs'):
        assert f'"{name}"' in sql
    assert 'DROP TABLE' not in sql.upper()
if __name__=='__main__':
    test_console();print('PASS admin console static contract')
