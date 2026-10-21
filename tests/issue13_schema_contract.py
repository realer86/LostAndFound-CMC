"""Offline source/safety checks. DOES NOT replace a dotnet build or Postman integration test."""
from pathlib import Path
import json
root = Path(__file__).resolve().parents[1]
entities = (root/'backend/Domain/Entities.cs').read_text()
db = (root/'backend/Data/LostFoundDb.cs').read_text()
sql = (root/'backend/DatabaseScripts/04_sprint2_contacts.sql').read_text()
program = (root/'backend/Program.cs').read_text()
controllers = '\n'.join(p.read_text() for p in (root/'backend/Controllers/ContactsController.cs',root/'backend/Controllers/MessagesController.cs',root/'backend/Controllers/DirectoryController.cs'))
for table in ['Contacts','Messages','Directories']:
    assert f'CREATE TABLE IF NOT EXISTS "{table}"' in sql
    assert f'ToTable("{table}")' in db
for field in ['ReportId','SenderId','RecipientId','Content','CreatedAt']:
    assert field in entities
assert 'CK_Contacts_DifferentUsers' in sql and 'IX_Contacts_ReportId_VisitorId' in sql
assert 'ExecuteSqlRawAsync' in program and '04_sprint2_contacts.sql' in program
assert 'Authorize' in controllers and 'User.FindFirstValue(ClaimTypes.NameIdentifier)' in controllers
assert 'SafeContact' in controllers and 'SafeMessage' in controllers
assert 'PasswordHash' not in controllers and 'PrivateIdentifier' not in controllers and 'Email' not in controllers
assert sql == (root/'database/04_sprint2_contacts.sql').read_text()
postman=json.loads((root/'docs/Foundly_Sprint2_Issue13.postman_collection.json').read_text())
assert len(postman['item']) == 6
print('PASS: Issue #13 EF models, non-destructive SQL, endpoint contracts, safe DTO fields, Postman JSON')
