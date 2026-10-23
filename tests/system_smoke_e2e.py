"""End-to-end API/system integration test against RUNNING Foundly API + PostgreSQL.
Writes 2 test users, 2 test reports, 1 image, 1 contact and messages to the selected database.
Dev-only; requires FOUNDLY_ALLOW_TEST_WRITES=1; never run against production.

Usage (development Docker running):
  set FOUNDLY_ALLOW_TEST_WRITES=1 && python tests/system_smoke_e2e.py

Linux:
  FOUNDLY_ALLOW_TEST_WRITES=1 python tests/system_smoke_e2e.py
"""
import os, json, uuid, http.cookiejar, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone
from pathlib import Path
import base64
from urllib.parse import urlencode

BASE=os.environ.get('FOUNDLY_API_URL','http://127.0.0.1:8081').rstrip('/')
if os.environ.get('FOUNDLY_ALLOW_TEST_WRITES')!='1':
 raise SystemExit('SKIP: this script writes test users and reports. Set FOUNDLY_ALLOW_TEST_WRITES=1 for a local DEVELOPMENT database only.')
if os.environ.get('ASPNETCORE_ENVIRONMENT','Development').lower()=='production':
 raise SystemExit('REFUSED: do not run this write-heavy smoke test against production.')

class Client:
 def __init__(self):self.open=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(self,method,path,data=None,headers=None):
  raw=json.dumps(data).encode() if data is not None else None
  req=urllib.request.Request(BASE+path,raw,headers or ({'Content-Type':'application/json'} if raw else {}),method=method)
  try:
   with self.open.open(req,timeout=12) as resp:return resp.status, json.loads(resp.read())
  except urllib.error.HTTPError as exc:
   try:return exc.code,json.loads(exc.read())
   except Exception:return exc.code,{}
 def image(self,report_id):
  # 1x1 tiny PNG test fixture (signature is checked by uploads API).
  b=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Yy5RgwAAAAASUVORK5CYII=')
  boundary='FoundlySmokeBoundary'
  data=(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="smoke.png"\r\nContent-Type: image/png\r\n\r\n'.encode()+b+f'\r\n--{boundary}--\r\n'.encode())
  req=urllib.request.Request(BASE+f'/api/uploads/reports/{report_id}',data,{'Content-Type':f'multipart/form-data; boundary={boundary}'},method='POST')
  try:
   with self.open.open(req,timeout=12) as resp:return resp.status,json.loads(resp.read())
  except urllib.error.HTTPError as exc:return exc.code,json.loads(exc.read())

passed=[]
def check(label,ok,detail=''):
 if not ok:raise AssertionError(f'FAIL: {label}: {detail}')
 passed.append(label);print('PASS:',label)

def safe(obj):
 if isinstance(obj,dict):
  forbidden={'passwordHash','privateIdentifier','verifyTokenHash'}
  check('No private fields in public DTO',not forbidden.intersection(obj.keys()),str(obj.keys()))
  for x in obj.values():safe(x)
 elif isinstance(obj,list):
  for x in obj:safe(x)

anon=Client();owner=Client();visitor=Client();outsider=Client()
status,health=anon.call('GET','/api/health');check('API health',status==200)
status,catalog=anon.call('GET','/api/reports/catalog');check('Catalog categories / locations',status==200 and isinstance(catalog.get('categories'),list) and isinstance(catalog.get('locations'),list))
status,discovery=anon.call('GET','/api/reports?page=1&pageSize=10');check('Public listing and pagination',status==200 and isinstance(discovery.get('items'),list) and 'total' in discovery)
status,unauthorized=anon.call('GET','/api/reports/mine');check('Unauthorized access to my reports blocked',status==401)
status,dirs=anon.call('GET','/api/directory');check('Public institutional directory',status==200 and isinstance(dirs.get('items'),list))
suffix=uuid.uuid4().hex[:12];password='SmokeSafe2026!'
for idx,cli in enumerate([owner,visitor]):
 email=f'foundly-smoke-{suffix}-{idx}@example.edu'
 code,data=cli.call('POST','/api/auth/register',{'email':email,'fullName':f'Smoke Tester {idx}','password':password})
 check('Registration '+str(idx),code==200 and data.get('devVerificationCode'),str((code,data)))
 verify_code=data['devVerificationCode']
 check('Unverified user login blocked',cli.call('POST','/api/auth/login',{'email':email,'password':password})[0]==403)
 check('Email verification',cli.call('POST','/api/auth/verify',{'email':email,'code':verify_code})[0]==200)
 check('Verified login',cli.call('POST','/api/auth/login',{'email':email,'password':password})[0]==200)
 status,me=cli.call('GET','/api/auth/me');check('Authenticated cookie session',status==200 and me.get('user',{}).get('email')==email)
now=(datetime.now(timezone.utc)-timedelta(minutes=30)).isoformat()
def post(cli,typ,name):
 payload={'type':typ,'itemName':name,'description':'Foundly full system regression','color':'blue','brand':'smoke',
  'locationDetail':'Smoke test location','categoryId':None,'locationId':None,'eventTime':now,
  'privateIdentifier':'SMOKE_PRIVATE_DO_NOT_EXPOSE' if typ=='FOUND' else None}
 return cli.call('POST','/api/reports',payload)
status,found=owner.call('POST','/api/reports',{'type':'FOUND','itemName':'Smoke FOUND '+suffix,'description':'Full test',
 'categoryId':None,'locationId':None,'eventTime':now,'privateIdentifier':'SMOKE_PRIVATE_DO_NOT_EXPOSE'})
check('Create FOUND report',status==201,str((status,found)))
found_id=found['id']
status,lost=post(owner,'LOST','Smoke LOST '+suffix)
check('Create LOST report',status==201,str((status,lost)))
lost_id=lost['id']
status,detail=visitor.call('GET','/api/reports/'+found_id);check('Public report detail and private evidence protected',status==200 and 'SMOKE_PRIVATE_DO_NOT_EXPOSE' not in json.dumps(detail) and 'privateIdentifier' not in detail)
status,filtered=visitor.call('GET','/api/reports?'+urlencode({'q':suffix,'type':'FOUND'}));check('Search, query and type filter',status==200 and any(x['id']==found_id for x in filtered.get('items',[])) and all(x['type']=='FOUND' for x in filtered.get('items',[])))
status,mine=owner.call('GET','/api/reports/mine');check('Owner list / my reports',status==200 and any(x['id']==found_id for x in mine))
status,url=owner.image(found_id);check('Image upload on owned OPEN report',status==200 and url.get('url','').startswith('/uploads/'),str((status,url)))
status,blocked=visitor.image(found_id);check('Non-owner upload forbidden',status==403)
status,contact=visitor.call('POST','/api/contacts',{'reportId':found_id});check('Create contact',status==201,str((status,contact)))
contact_id=contact['id'];status,again=visitor.call('POST','/api/contacts',{'reportId':found_id});check('Duplicate contacts reuse conversation',status==200 and again['id']==contact_id)
status,msg=visitor.call('POST','/api/messages',{'contactId':contact_id,'content':'Smoke hello'});check('Send chat via API',status==201 and msg.get('content')=='Smoke hello')
status,msgs=owner.call('GET','/api/messages?'+urlencode({'contactId':contact_id}));check('Owner can read chat',status==200 and any(x.get('content')=='Smoke hello' for x in msgs.get('items',[])))
status,_=outsider.call('GET','/api/messages?'+urlencode({'contactId':contact_id}));check('Outsider cannot read chat',status in (401,404))
status,done=owner.call('PATCH','/api/reports/'+lost_id+'/status');check('Owner can mark returned',status==200 and done.get('status')=='RETURNED')
status,_=visitor.call('PATCH','/api/reports/'+found_id+'/status');check('Non-owner cannot mark returned',status==403)
status,exposed=anon.call('GET','/api/reports/'+lost_id);check('Returned report stays viewable',status==200 and exposed.get('status')=='RETURNED')
status,_=owner.call('POST','/api/auth/logout');check('Logout response',status==200)
status,me=owner.call('GET','/api/auth/me');check('Logout clears cookie session',status==200 and me.get('user') is None)
print(f'ALL PASS: {len(passed)} checks; development test records were added to the database.')
