// API transport tests. Runs with Node.js; no React bundler or database required.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../frontend/src/components/ChatLogic.jsx'), 'utf8')
  .replace(/^import .* from 'react';\s*/m, '')
  .replaceAll('export async function chatApi', 'async function chatApi')
  .replaceAll('export function useChatLogic', 'function useChatLogic');

function createApi(mockFetch) {
  const context = vm.createContext({ fetch: mockFetch });
  vm.runInContext(`${source}\nthis.chatApi = chatApi;`, context);
  return context.chatApi;
}

function success(body = { items: [] }, status = 200) {
  return { ok: true, status, json: async () => body };
}

test('uses the same-origin proxy and cookie for #13 read endpoints', async () => {
  const seen = [];
  const api = createApi(async (url, opts) => {
    seen.push({ url, opts });
    return success({ items: [{ id: 'one' }] });
  });
  for (const path of ['/contacts', '/messages?contactId=abc&page=1&pageSize=50', '/directory']) {
    const response = await api(path);
    assert.equal(response.items[0].id, 'one');
  }
  assert.deepEqual(seen.map(x => x.url), [
    '/api/contacts', '/api/messages?contactId=abc&page=1&pageSize=50', '/api/directory',
  ]);
  for (const item of seen) assert.equal(item.opts.credentials, 'same-origin');
});

test('POST contact and POST message keep the exact contract', async () => {
  const seen = [];
  const api = createApi(async (url, opts) => {
    seen.push({ url, ...opts });
    return success({ id: 'contact-1' }, 201);
  });
  await api('/contacts', { method: 'POST', body: JSON.stringify({ reportId: 'report-1' }) });
  await api('/messages', { method: 'POST', body: JSON.stringify({ contactId: 'contact-1', content: 'Xin chào' }) });
  assert.deepEqual(JSON.parse(seen[0].body), { reportId: 'report-1' });
  assert.deepEqual(JSON.parse(seen[1].body), { contactId: 'contact-1', content: 'Xin chào' });
  assert.equal(seen[0].headers['Content-Type'], 'application/json');
  assert.equal(seen[1].headers['Content-Type'], 'application/json');
});

test('reports 401, 409, 429 rather than pretending to send', async () => {
  for (const status of [401, 409, 429]) {
    const api = createApi(async () => ({
      ok: false, status, json: async () => ({ message: `Server error ${status}` }),
    }));
    await assert.rejects(() => api('/messages', { method: 'POST' }), new RegExp(String(status)));
  }
});

test('handles a non-JSON response on server error', async () => {
  const api = createApi(async () => ({ ok: false, status: 503, json: async () => { throw Error('invalid'); } }));
  await assert.rejects(() => api('/directory'), /HTTP 503/);
});
