# Legacy P2P integration test, retained as historical archive. Deprecated by official support inbox.
"""Issue #13 integration smoke test. Requires running localhost API + development demo users.
Usage: python tests/issue13_contacts_api.py
Optional environment variable: FOUNDLY_API_URL.
"""
import http.cookiejar
import json
import os
import urllib.error
import urllib.request
from datetime import datetime, timezone, timedelta

BASE = os.environ.get('FOUNDLY_API_URL', 'http://127.0.0.1:8081').rstrip('/')

def client():
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def call(method, path, payload=None):
        raw = json.dumps(payload).encode() if payload is not None else None
        request = urllib.request.Request(BASE + '/api' + path, raw,
            {'Content-Type': 'application/json'} if payload is not None else {}, method=method)
        try:
            with opener.open(request, timeout=12) as response:
                return response.status, json.load(response)
        except urllib.error.HTTPError as e:
            try: return e.code, json.load(e)
            except Exception: return e.code, {}
    return call

def no_personal_fields(obj):
    # Directory has internalPhone by design; chat/user views must not expose private Email/Phone.
    if isinstance(obj, dict):
        assert not {'email', 'phone', 'passwordHash', 'privateIdentifier', 'verifyTokenHash'}.intersection(obj), obj
        for value in obj.values(): no_personal_fields(value)
    elif isinstance(obj, list):
        for value in obj: no_personal_fields(value)

owner, visitor, outsider = client(), client(), client()
assert owner('POST', '/auth/login', {'email': 'minh@campus.edu', 'password': 'Demo12345!'})[0] == 200
assert visitor('POST', '/auth/login', {'email': 'lan@campus.edu', 'password': 'Demo12345!'})[0] == 200
code, report = owner('POST', '/items', {
    'type': 'LOST', 'itemName': 'Túi thử nghiệm Issue13', 'description': 'API integration smoke',
    'eventTime': (datetime.now(timezone.utc) - timedelta(hours=1)).isoformat(),
    'color': None, 'brand': None, 'locationDetail': None, 'categoryId': None,
    'locationId': None, 'privateIdentifier': None})
assert code == 201, (code, report)
report_id = report['id']
code, contact = visitor('POST', '/contacts', {'reportId': report_id})
assert code == 201, (code, contact)
no_personal_fields(contact)
contact_id = contact['id']
assert visitor('POST', '/contacts', {'reportId': report_id})[1]['id'] == contact_id
assert visitor('POST', '/contacts', {'reportId': report_id})[0] == 200
code, sent = visitor('POST', '/messages', {'contactId': contact_id, 'content': 'Xin chào!'})
assert code == 201 and sent['content'] == 'Xin chào!', (code, sent)
no_personal_fields(sent)
code, history = owner('GET', '/messages?contactId=' + contact_id)
assert code == 200 and history['total'] == 1 and history['items'][0]['id'] == sent['id'], (code, history)
no_personal_fields(history)
assert visitor('POST', '/messages', {'contactId': contact_id, 'content': ' '})[0] == 400
assert outsider('GET', '/messages?contactId=' + contact_id)[0] == 401
assert outsider('POST', '/messages', {'contactId': contact_id, 'content': 'Không hợp lệ'})[0] == 401
assert owner('POST', '/contacts', {'reportId': report_id})[0] == 400
code, contacts = owner('GET', '/contacts')
assert code == 200 and any(c['id'] == contact_id for c in contacts['items'])
no_personal_fields(contacts)
code, directory = outsider('GET', '/directory')
assert code == 200 and isinstance(directory['items'], list)
assert all(set(x).issubset({'id','name','location','internalPhone','officeHours','category'}) for x in directory['items'])
code, _ = owner('PATCH', '/items/' + report_id + '/status')
assert code == 200, code
assert visitor('POST', '/messages', {'contactId': contact_id, 'content': 'Bài đã đóng'})[0] == 409
print('PASS: contact creation, idempotency, two-party messages, no personal fields, directory, closed report')
