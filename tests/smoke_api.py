"""Foundly issues #13 + #17: stdlib-only smoke test against a RUNNING Docker API.

Run: python tests/smoke_api.py
Override API: Environment variable: FOUNDLY_BASE_URL=http://localhost:8081
Creates a new FOUND report, 2 claims and anonymous messages in the DEV DB.
Do not run against real production data.
"""
import json
import os
from datetime import datetime, timedelta, timezone
from http.cookiejar import CookieJar
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import HTTPCookieProcessor, Request, build_opener

BASE = os.getenv('FOUNDLY_BASE_URL', 'http://localhost:8081').rstrip('/')


class Client:
    def __init__(self):
        self.opener = build_opener(HTTPCookieProcessor(CookieJar()))

    def call(self, method, path, payload=None, expected=200):
        body = None if payload is None else json.dumps(payload).encode('utf-8')
        request = Request(BASE + path, data=body, method=method)
        request.add_header('Accept', 'application/json')
        if body is not None:
            request.add_header('Content-Type', 'application/json')
        try:
            with self.opener.open(request, timeout=15) as resp:
                status, text = resp.status, resp.read().decode('utf-8')
        except HTTPError as err:
            status, text = err.code, err.read().decode('utf-8')
        try:
            data = json.loads(text)
        except json.JSONDecodeError:
            data = {'raw': text}
        assert status == expected, f'{method} {path}: expected {expected}, got {status}, body={data}'
        return data

    def login(self, email):
        result = self.call('POST', '/api/auth/login',
                           {'email': email, 'password': 'Demo12345!'})
        return result['user']['id']


def verify(condition, explanation):
    assert condition, explanation
    print('  PASS:', explanation)


def main():
    owner, lan, bao = Client(), Client(), Client()
    owner_id = owner.login('minh@campus.edu')
    lan_id = lan.login('lan@campus.edu')
    bao_id = bao.login('bao@campus.edu')
    print('Logged in 3 Development demo accounts')

    now = (datetime.now(timezone.utc) - timedelta(minutes=5)).isoformat()
    report = owner.call('POST', '/api/items', {
        'type': 'FOUND', 'itemName': 'Foundly verification demo',
        'description': 'Issue #13 + #17 backend smoke test',
        'eventTime': now, 'categoryId': None, 'locationId': None,
        'color': None, 'brand': None, 'locationDetail': 'Demo',
        'privateIdentifier': 'Mau the xac minh demo',
    }, expected=201)
    item_id = report['id']
    print('Demo FOUND item:', item_id)

    s1 = 'Demo evidence Lan 123456 test secret'
    s2 = 'Demo evidence Bao 123456 test secret'
    c1 = lan.call('POST', '/api/claims', {'itemId': item_id, 'secretText': s1}, 201)
    c2 = bao.call('POST', '/api/claims', {'itemId': item_id, 'secretText': s2}, 201)
    verify('secrettext' not in json.dumps(c1).lower(), 'POST claim does not echo SecretText')
    verify(lan.call('GET', f'/api/claims/{c1["id"]}')['status'] == 'PENDING',
           'Claimant may see own PENDING status')
    verify('secrettext' not in json.dumps(lan.call('GET', f'/api/claims/{c1["id"]}')).lower(),
           'Claimant detail does not expose evidence')
    lan.call('GET', f'/api/items/{item_id}/claims', expected=403)
    bao.call('GET', f'/api/claims/{c1["id"]}', expected=404)
    verify(len(owner.call('GET', f'/api/items/{item_id}/claims')) == 2,
           'Owner can view exactly two claims')
    private = owner.call('GET', f'/api/claims/{c1["id"]}')
    verify(private['secretText'] == s1, 'Owner can view the private evidence')

    lan.call('POST', '/api/messages', {
        'itemId': item_id, 'recipientId': owner_id,
        'content': 'Chao ban, minh muon hoi thong tin mon do.'}, expected=201)
    lan.call('POST', '/api/messages', {
        'itemId': item_id, 'recipientId': owner_id,
        'content': 'So dien thoai 0987654321'}, expected=400)
    owner.call('POST', '/api/messages', {
        'itemId': item_id, 'recipientId': lan_id, 'content': 'Hen ban tai quay bao ve.'}, expected=201)
    messages = lan.call('GET', f'/api/messages?{urlencode({"itemId":item_id,"otherUserId":owner_id})}')
    verify(messages['total'] == 2, 'Both sides can view their own two messages')
    bao.call('GET', f'/api/messages?{urlencode({"itemId":item_id,"otherUserId":lan_id})}', expected=403)
    verify('minh@campus.edu' not in json.dumps(messages), 'Messages response hides email')

    approved = owner.call('PATCH', f'/api/claims/{c1["id"]}/status', {'status': 'APPROVED'})
    verify(approved['automaticallyRejected'] == 1, 'Approval auto-rejects other pending claims')
    verify(bao.call('GET', f'/api/claims/{c2["id"]}')['status'] == 'REJECTED',
           'Second claimant now REJECTED')
    owner.call('PATCH', f'/api/claims/{c2["id"]}/status', {'status': 'APPROVED'}, expected=409)
    bao.call('POST', '/api/claims', {'itemId': item_id, 'secretText': s2}, expected=409)
    owner.call('POST', '/api/messages', {
        'itemId': item_id, 'recipientId': bao_id, 'content': 'Test private reply'}, expected=403)
    print('  PASS: approved claimant gets exclusive contact')

    contacts = owner.call('GET', '/api/contacts')
    directory = owner.call('GET', '/api/contacts/directory')
verify(
    any(
        p['itemId'] == item_id and p['otherUserId'] == lan_id
        for p in contacts['people']
    ),
    'Approved claimant appears in contacts'
)

verify(
    not any(
        p['itemId'] == item_id and p['otherUserId'] == bao_id
        for p in contacts['people']
    ),
    'Rejected claimant without chat is absent from contacts'
)    verify(len(directory) >= 2, 'Development directory contains internal offices')
    payload = json.dumps(contacts, ensure_ascii=False)
    verify('@campus.edu' not in payload and '0987654321' not in payload,
           'Contacts response contains no personal email or phone')
    print('\nSUCCESS: all Issue #13 + #17 smoke checks passed')


if __name__ == '__main__':
    main()
