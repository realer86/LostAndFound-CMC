"""Foundly Issue #17: concurrent Claim approval test (stdlib only).

Run from repository root, with Docker Compose already running:
    python tests/concurrency_test.py
    python tests/concurrency_test.py --rounds 10

Environment: FOUNDLY_BASE_URL=http://localhost:8081 (default).
This test creates fresh reports/claims in the DEVELOPMENT database. It never
modifies existing reports. It will not run against a non-localhost API.

Expected for each round:
    - two concurrent PATCH requests target different PENDING Claims on one item;
    - one responds 200 APPROVED, the other 409 Conflict;
    - final API and PostgreSQL state is one APPROVED + one REJECTED.
"""

import argparse
import json
import os
import subprocess
import sys
import threading
import time
import uuid
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timedelta, timezone
from http.cookiejar import CookieJar
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import HTTPCookieProcessor, Request, build_opener

DEFAULT_BASE = os.environ.get('FOUNDLY_BASE_URL', 'http://localhost:8081').rstrip('/')


class CheckFailed(AssertionError):
    pass


def require(condition, message):
    if not condition:
        raise CheckFailed(message)


class Client:
    """Each instance has an independent authenticated cookie jar."""

    def __init__(self, base):
        self.base = base
        self.opener = build_opener(HTTPCookieProcessor(CookieJar()))

    def request(self, method, path, payload=None):
        body = None if payload is None else json.dumps(payload).encode('utf-8')
        request = Request(self.base + path, data=body, method=method)
        request.add_header('Accept', 'application/json')
        if body is not None:
            request.add_header('Content-Type', 'application/json')
        try:
            with self.opener.open(request, timeout=30) as response:
                status = response.status
                content = response.read().decode('utf-8')
        except HTTPError as error:
            status = error.code
            content = error.read().decode('utf-8', errors='replace')
        try:
            data = json.loads(content) if content else None
        except json.JSONDecodeError:
            data = {'raw': content[:600]}
        return status, data

    def expect(self, method, path, payload=None, status=200):
        actual, data = self.request(method, path, payload)
        require(actual == status,
                f'{method} {path}: expected HTTP {status}, got {actual}: {data}')
        return data

    def login(self, email):
        self.expect('POST', '/api/auth/login',
                    {'email': email, 'password': 'Demo12345!'}, status=200)


def database_counts(item_id):
    """Query the actual PostgreSQL container, not merely the API response."""
    safe_id = str(uuid.UUID(item_id))
    sql = (
        'SELECT COUNT(*) FILTER (WHERE "Status" = \'APPROVED\') || \'|\' || '
        'COUNT(*) FILTER (WHERE "Status" = \'REJECTED\') || \'|\' || COUNT(*) '
        'FROM "Claims" WHERE "ItemId" = \'%s\'::uuid;' % safe_id
    )
    command = [
        'docker', 'compose', 'exec', '-T', 'postgres',
        'psql', '-X', '-U', 'lostfound', '-d', 'lostfound',
        '-A', '-t', '-v', 'ON_ERROR_STOP=1', '-c', sql,
    ]
    try:
        result = subprocess.run(command, capture_output=True, text=True,
                                encoding='utf-8', errors='replace', timeout=25,
                                check=False)
    except FileNotFoundError as error:
        raise CheckFailed('Docker CLI not found. Install/start Docker, or use --skip-db.') from error
    except subprocess.TimeoutExpired as error:
        raise CheckFailed('PostgreSQL verification timed out.') from error
    require(result.returncode == 0,
            f'PostgreSQL verification failed: {result.stderr.strip()}')
    output = result.stdout.strip()
    require(output == '1|1|2',
            f'Database must have 1 APPROVED + 1 REJECTED (2 total), got {output!r}')
    return output


def approve_at_once(client, claim_id, barrier):
    barrier.wait(timeout=15)
    started = time.perf_counter_ns()
    code, body = client.request(
        'PATCH', f'/api/claims/{claim_id}/status', {'status': 'APPROVED'})
    return {'id': claim_id, 'status_code': code, 'body': body, 'started': started}


def run_round(round_no, total_rounds, clients, verify_db):
    owner_a, owner_b, lan, bao = clients
    event_time = (datetime.now(timezone.utc) - timedelta(minutes=5)).isoformat()
    label = f'Foundly concurrency test {round_no}-{uuid.uuid4().hex[:8]}'
    item = owner_a.expect('POST', '/api/items', {
        'type': 'FOUND', 'itemName': label,
        'description': 'Development-only concurrent approval verification',
        'eventTime': event_time, 'categoryId': None, 'locationId': None,
        'color': None, 'brand': None, 'locationDetail': 'Test',
        'privateIdentifier': 'Temporary verification value',
    }, status=201)
    item_id = str(uuid.UUID(item['id']))

    claim_a = lan.expect('POST', '/api/claims', {
        'itemId': item_id,
        'secretText': 'Test evidence from account Lan',
    }, status=201)['id']
    claim_b = bao.expect('POST', '/api/claims', {
        'itemId': item_id,
        'secretText': 'Test evidence from account Bao',
    }, status=201)['id']

    before = owner_a.expect('GET', f'/api/items/{item_id}/claims')
    require(len(before) == 2 and all(c['status'] == 'PENDING' for c in before),
            f'Round {round_no}: expected two PENDING claims, got {before}')

    # One authenticated session per thread, both logged in as the item owner.
    # The main thread releases both workers through the same barrier.
    barrier = threading.Barrier(3)
    with ThreadPoolExecutor(max_workers=2) as executor:
        future_a = executor.submit(approve_at_once, owner_a, claim_a, barrier)
        future_b = executor.submit(approve_at_once, owner_b, claim_b, barrier)
        barrier.wait(timeout=15)
        result_a = future_a.result(timeout=40)
        result_b = future_b.result(timeout=40)

    results = [result_a, result_b]
    codes = sorted(result['status_code'] for result in results)
    print(f'  [{round_no}/{total_rounds}] PATCH HTTP: {result_a["status_code"]}, '
          f'{result_b["status_code"]}; item={item_id}')
    require(codes == [200, 409],
            f'Expected exactly one HTTP 200 and one HTTP 409, got {results}')

    winner = next(r for r in results if r['status_code'] == 200)
    loser = next(r for r in results if r['status_code'] == 409)
    require(isinstance(winner['body'], dict) and
            winner['body'].get('status') == 'APPROVED',
            f'Winner response is incorrect: {winner["body"]}')
    require(str(winner['body'].get('id', '')).lower() == winner['id'].lower(),
            f'Approved claim ID does not match request: {winner}')

    # The losing claim must be REJECTED, not still PENDING or also APPROVED.
    after = owner_a.expect('GET', f'/api/items/{item_id}/claims')
    final = {c['id'].lower(): c['status'] for c in after}
    require(len(final) == 2 and set(final) == {claim_a.lower(), claim_b.lower()},
            f'Unexpected claim set returned by API: {after}')
    require(final[winner['id'].lower()] == 'APPROVED',
            f'Winner is not APPROVED in persisted API state: {final}')
    require(final[loser['id'].lower()] == 'REJECTED',
            f'Loser is not REJECTED in persisted API state: {final}')
    if verify_db:
        database_counts(item_id)
    delta_ms = abs(result_a['started'] - result_b['started']) / 1_000_000
    print(f'      PASS: exactly one APPROVED, one REJECTED; '
          f'request start delta {delta_ms:.2f} ms'
          + ('; PostgreSQL verified' if verify_db else '; DB check skipped'))


def main():
    parser = argparse.ArgumentParser(description='Foundly Claim approval concurrency test')
    parser.add_argument('--rounds', type=int, default=5,
                        help='Number of fresh-item concurrent runs (default: 5, maximum: 30)')
    parser.add_argument('--skip-db', action='store_true',
                        help='Skip direct PostgreSQL verification; verify API state only')
    args = parser.parse_args()
    if not 1 <= args.rounds <= 30:
        parser.error('--rounds must be between 1 and 30')

    base = DEFAULT_BASE
    parsed = urlsplit(base)
    if parsed.scheme not in ('http', 'https') or parsed.hostname not in ('localhost', '127.0.0.1', '::1'):
        parser.error('Safety check: test is only allowed against a localhost API.')

    print(f'Foundly concurrent approval test | API: {base} | rounds: {args.rounds}')
    print('WARNING: creates new reports and Claims in the local DEVELOPMENT database.')
    print('Logging into demo accounts...')
    owner_a, owner_b, lan, bao = (Client(base) for _ in range(4))
    owner_a.login('minh@campus.edu')
    owner_b.login('minh@campus.edu')
    lan.login('lan@campus.edu')
    bao.login('bao@campus.edu')
    print('  PASS: four independent login sessions (two for owner)')

    for number in range(1, args.rounds + 1):
        run_round(number, args.rounds, (owner_a, owner_b, lan, bao),
                  verify_db=not args.skip_db)
    print(f'\nSUCCESS: all {args.rounds} concurrent approval rounds passed'
          + (' (API + PostgreSQL).' if not args.skip_db else ' (API only).'))
    print('Note: passing these runs does not prove every possible timing is safe.')


if __name__ == '__main__':
    try:
        main()
    except (CheckFailed, URLError, TimeoutError, threading.BrokenBarrierError) as error:
        print(f'\nFAIL: {error}', file=sys.stderr)
        sys.exit(1)
    except Exception as error:
        print(f'\nERROR: unexpected test failure: {error}', file=sys.stderr)
        sys.exit(1)
