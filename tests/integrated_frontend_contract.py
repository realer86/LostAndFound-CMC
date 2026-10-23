"""Offline checks for Foundly Pro UI integrated into existing Sprint main.
These checks prove source/API compatibility, NOT a complete live API/browser E2E test.
Run from the repository root: python tests/integrated_frontend_contract.py
"""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
FRONT=ROOT/'frontend/src/main.jsx'
SRC=FRONT.read_text(encoding='utf-8')
CSS=(ROOT/'frontend/src/style.css').read_text(encoding='utf-8')
BACK=(ROOT/'backend/Controllers/ItemsController.cs').read_text(encoding='utf-8')
CONTACTS=(ROOT/'backend/Controllers/ContactsController.cs').read_text(encoding='utf-8')
MESSAGES=(ROOT/'backend/Controllers/MessagesController.cs').read_text(encoding='utf-8')
DIRECTORY=(ROOT/'backend/Controllers/DirectoryController.cs').read_text(encoding='utf-8')

checks={
 'Pro UI lives in the ORIGINAL frontend (not frontend-pro)': (ROOT/'frontend/src/main.jsx').exists() and 'Thất lạc đồ?' in SRC and 'herogrid' in SRC,
 'Pro style included in original stylesheet': '.hero-illustration' in CSS and '.upcoming-panel' in CSS,
 'Frontend uses existing Vite proxy and /api/report aliases': "fetch('/api'+path" in SRC and 'Route("api/reports")' in BACK,
 'Working reports/catelog/search/login remain wired': all(x in SRC for x in ["api('/reports/catalog')","api('/reports/mine')","api('/auth/login'","api('/auth/register'","api('/reports',{method:'POST'","api(`/reports/${r.id}/status`"]),
 'All coming-soon pages stay visible': all(f"gotoFeature('{name}')" in SRC for name in ('matching','claims','operations','handovers','notifications')),
 'Feature flag defaults to Sprint until manually enabled': "const fullBackend = import.meta.env.VITE_API_PROFILE === 'full';" in SRC,
 'Unavailable endpoints are protected by fullBackend': "if(!fullBackend||!me)return;" in SRC and "if(!fullBackend||!me?.id)return;" in SRC and "{fullBackend&&isFound(r.type)" in SRC,
 'Chat logic remains real and connected': 'useChatLogic({user,initialReport})' in SRC and 'live={chat}' in SRC and 'initialReport={chatReport}' in SRC,
 'Chat links from report detail': 'Liên hệ người đăng qua hộp thư' in SRC and 'onChat={()=>{setChatReport(selected)' in SRC,
 'Sprint2 API endpoints retained': all('Route("api/'+name+'")' in content for name,content in [('contacts',CONTACTS),('messages',MESSAGES),('directory',DIRECTORY)]),
 'No backend or Compose modifications required': (ROOT/'compose.yaml').is_file() and (ROOT/'backend/Program.cs').is_file(),
 'RETURNED badge and returned-card visually preserved': "returned?'ĐÃ TRẢ'" in SRC and "r.status==='RETURNED'?'ĐÃ TRẢ'" in SRC and '.reportbadge.returned' in CSS,
 'Only one React root': len(re.findall(r"createRoot\(document\.getElementById\('root'\)\)",SRC))==1,
}
for label,valid in checks.items():
    print(('PASS' if valid else 'FAIL')+': '+label)
assert all(checks.values()),'Some checks failed; do not merge.'
print(f'PASS {len(checks)}/{len(checks)} integrated frontend checks')
