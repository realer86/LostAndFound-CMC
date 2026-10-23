"""Browser regression suite for Foundly React UI with mocked API responses.
Requires Vite (or preview) running at FOUNDLY_FRONTEND_URL, playwright + Chromium.
Tests all existing user journeys without writing to a real database.
Example: FOUNDLY_FRONTEND_URL=http://127.0.0.1:5175 CHROMIUM_PATH=/usr/bin/chromium python tests/frontend_full_e2e.py
"""
from pathlib import Path
from playwright.sync_api import sync_playwright
from urllib.parse import urlparse, parse_qs
import json, re, copy
ROOT=Path(__file__).resolve().parents[1]
BASE_URL=__import__('os').environ.get('FOUNDLY_FRONTEND_URL','http://127.0.0.1:5175')
INITIAL=[
 {'id':'00000000-0000-0000-0000-000000000011','type':'LOST','itemName':'Ví da đen','description':'Mất tại phòng 101','status':'OPEN','userId':'owner-id','createdAt':'2026-10-20T07:00:00Z','eventTime':'2026-10-19T07:00:00Z','location':'Tòa A','category':'Ví & túi','color':'Đen','images':[]},
 {'id':'00000000-0000-0000-0000-000000000022','type':'FOUND','itemName':'Balo xanh','description':'Nhặt tại thư viện','status':'OPEN','userId':'owner-id','createdAt':'2026-10-20T07:00:00Z','eventTime':'2026-10-19T07:00:00Z','location':'Tòa B','category':'Ví & túi','images':[]},
 {'id':'00000000-0000-0000-0000-000000000033','type':'LOST','itemName':'Điện thoại đỏ','description':'Đã hoàn trả','status':'RETURNED','userId':'owner-id','createdAt':'2026-10-20T07:00:00Z','eventTime':'2026-10-19T07:00:00Z','location':'Tòa C','category':'Điện tử','images':[]}
]
OWNER={'id':'owner-id','fullName':'Owner Test'}; VIEWER={'id':'viewer-id','fullName':'Visitor Test'}
class FakeAPI:
 def __init__(self,account=None):self.account=account;self.reports=copy.deepcopy(INITIAL);self.calls=[];self.contacts=[];self.messages=[];self.seq=40
 def handle(self,route):
  req=route.request
  url=urlparse(req.url);path=url.path
  if not path.startswith('/api/'):
   if path=='/' or path=='':route.fulfill(content_type='text/html',body='<!doctype html><html><head></head><body><div id="root"></div></body></html>')
   else:route.fulfill(status=404,body='')
   return
  p=path[4:];method=req.method;q=parse_qs(url.query);data={}
  try:data=req.post_data_json if req.post_data and 'application/json' in (req.headers.get('content-type') or '') else {}
  except:pass
  self.calls.append((method,p,q,data))
  code=200;body={}
  if p=='/auth/me':body={'user':self.account} if self.account else {'user':None}
  elif p=='/auth/login':self.account=OWNER if data.get('email','').startswith('owner') else VIEWER;body={'user':self.account}
  elif p=='/auth/register':body={'devVerificationCode':'123456'}
  elif p=='/auth/verify':body={'message':'verified'}
  elif p=='/auth/logout':self.account=None;body={'message':'logout'}
  elif p in ('/items/catalog','/reports/catalog'):
   body={'categories':[{'id':1,'name':'Điện tử'},{'id':2,'name':'Ví & túi'}],'locations':[{'id':1,'name':'Tòa A'},{'id':2,'name':'Thư viện'}]}
  elif p in ('/items','/reports') and method=='GET':
   vals=[r for r in self.reports if (q.get('type',[''])[0] in ('',r['type'])) and (q.get('q',[''])[0].lower() in (r['itemName']+r.get('description','')).lower())]
   page=int(q.get('page',[1])[0]);pageSize=int(q.get('pageSize',[12])[0]);body={'items':vals[(page-1)*pageSize:page*pageSize],'total':len(vals),'page':page,'pageSize':pageSize}
  elif p in ('/items/mine','/reports/mine') and method=='GET':body=[r for r in self.reports if r['userId']==(self.account or {}).get('id')]
  elif p in ('/items','/reports') and method=='POST':
   if not self.account:code=401;body={'message':'Unauthorized'}
   else:
    self.seq+=1;id=f'00000000-0000-0000-0000-{self.seq:012d}'
    x={'id':id,'status':'OPEN','userId':self.account['id'],'images':[],'createdAt':'2026-10-22T10:00:00Z','eventTime':data.get('eventTime'),'location':'Tòa A','category':'Điện tử',**data};self.reports.insert(0,x);code=201;body={'id':id,'message':'created'}
  elif re.fullmatch(r'/reports/[^/]+/status|/items/[^/]+/status',p) and method=='PATCH':
   id=p.split('/')[2]; x=next((x for x in self.reports if x['id']==id),None)
   if x and self.account and x['userId']==self.account['id']:x['status']='RETURNED';body={'id':id,'status':'RETURNED'}
   else:code=403;body={'message':'Forbidden'}
  elif p.startswith('/uploads/reports/') and method=='POST':code=201;body={'url':'/uploads/test.png'}
  elif p=='/contacts' and method=='GET':body={'items':self.contacts}
  elif p=='/contacts' and method=='POST':
   self.seq+=1;c={'id':f'00000000-0000-0000-0000-{self.seq:012d}','reportId':data.get('reportId'),'reportTitle':'Ví da đen','otherDisplayName':'Owner Test'};self.contacts.append(c);code=201;body=c
  elif p=='/messages' and method=='GET':body={'items':self.messages,'total':len(self.messages)}
  elif p=='/messages' and method=='POST':
   self.seq+=1;m={'id':str(self.seq),'content':data.get('content'),'createdAt':'2026-10-22T10:00:00Z','senderId':(self.account or {}).get('id')};self.messages.append(m);code=201;body=m
  elif p=='/directory':body={'items':[{'id':'office','name':'Phòng CTSV','category':'Hỗ trợ','location':'Tòa A','officeHours':'8-17h','internalPhone':'123'}]}
  else:code=404;body={'message':'No mock endpoint for '+p}
  route.fulfill(status=code,body=json.dumps(body,ensure_ascii=False),content_type='application/json')

def setup(browser,which='after',account=None,width=1280):
 context=browser.new_context(viewport={'width':width,'height':840})
 page=context.new_page();errors=[]
 page.on('pageerror',lambda e:errors.append(str(e)))
 api=FakeAPI(account)
 fixture=__import__('os').environ.get('FOUNDLY_TEST_FIXTURE_JS')
 if fixture:
  def call_js(arg):
   class R:
    url=arg['url'];method=arg['method'];headers={'content-type':arg.get('type','')};post_data=arg.get('body')
    @property
    def post_data_json(self):return json.loads(self.post_data) if self.post_data else {}
   class Route:
    request=R()
    def fulfill(self,status=200,body='',content_type='application/json'):self.value={'status':status,'body':body}
   route=Route();api.handle(route);return route.value
  page.expose_function('__mockapi',call_js)
  page.set_content('<!doctype html><html><head></head><body><div id="root"></div></body></html>')
  page.evaluate("""(()=>{window.fetch=async (url,opts={})=>{const x=await window.__mockapi({url,method:opts.method||'GET',body:typeof opts.body==='string'?opts.body:'',type:opts.headers?.['Content-Type']||''});return new Response(x.body,{status:x.status,headers:{'Content-Type':'application/json'}})};})();""")
  page.add_style_tag(content=(ROOT/'frontend/src/style.css').read_text())
  page.add_style_tag(content=(ROOT/'frontend/src/components/sprint2Contacts.css').read_text())
  page.add_script_tag(content=Path(fixture).read_text())
 else:
  page.route('**/api/**',api.handle)
  page.goto(BASE_URL,wait_until='domcontentloaded')
 page.locator('.app').wait_for(timeout=8000)
 page.wait_for_timeout(150)
 return context,page,api,errors

def run():
 report=[]
 with sync_playwright() as p:
  browser=p.chromium.launch(headless=True, executable_path=__import__('os').environ.get('CHROMIUM_PATH') or None,args=['--no-sandbox'])
  def case(name,fn):
   try:res=fn();report.append((name,'PASS',str(res or '')));print('PASS',name,res or '')
   except Exception as e:report.append((name,'FAIL',str(e)));print('FAIL',name,repr(e))
  def home():
   c,p,a,e=setup(browser)
   try:
    assert p.locator('.reportcard').count()==3
    p.locator('.filterpill').filter(has_text='Đồ nhặt được').click()
    assert p.locator('.reportcard').count()==1
    p.locator('.filterpill').filter(has_text='Tất cả').click()
    p.locator('.searchbox input').fill('điện thoại');p.wait_for_timeout(100)
    assert p.locator('.reportcard').count()==1
    p.locator('.searchbox input').fill('');p.wait_for_timeout(100)
    assert 'ĐÃ TRẢ' in p.locator('.reportcard').filter(has_text='Điện thoại đỏ').inner_text(),'returned status badge missing'
    p.locator('.reportcard').filter(has_text='Điện thoại đỏ').click()
    assert 'ĐÃ TRẢ' in p.locator('.detailbody').inner_text(),'returned detail status missing'
    assert not e,e
   finally:c.close()
  case('feed, search, filters, RETURNED status',home)
  def authenticate():
   c,p,a,e=setup(browser)
   try:
    p.get_by_role('button',name='Đăng nhập',exact=True).first.click()
    p.locator('input[type=email]').fill('owner@campus.edu');p.locator('input[type=password]').fill('SomeDemoPassword')
    p.locator('.modal button[type=submit]').click()
    p.get_by_role('button',name='Đăng xuất').wait_for(timeout=4000)
    assert any(x[1]=='/auth/login' for x in a.calls)
    p.get_by_role('button',name='Đăng xuất').click()
    p.get_by_role('button',name='Đăng nhập',exact=True).first.wait_for()
    assert a.account is None
    assert not e,e
   finally:c.close()
  case('login, logout and privacy state',authenticate)
  def register():
   c,p,a,e=setup(browser)
   try:
    p.get_by_role('button',name='Đăng nhập',exact=True).first.click();p.get_by_role('button',name='Đăng ký ngay').click()
    p.locator('input[placeholder="Nguyễn Văn A"]').fill('Test User');p.locator('input[type=email]').fill('user@campus.edu');p.locator('input[type=password]').fill('strongPassword2026!')
    p.locator('.modal button[type=submit]').click()
    p.get_by_text('Xác minh email',exact=True).wait_for()
    p.locator('input[pattern="[0-9]{6}"]').fill('123456');p.locator('.modal button[type=submit]').click()
    p.get_by_text('Đăng nhập vào Foundly',exact=True).wait_for()
    assert any(x[1]=='/auth/register' for x in a.calls) and any(x[1]=='/auth/verify' for x in a.calls)
    assert not e,e
   finally:c.close()
  case('registration and verification',register)
  def header_auth_actions():
   c,p,a,e=setup(browser,width=1440)
   try:
    post=p.locator('.topactions .topnew')
    register=p.locator('.topactions .topregister')
    login=p.locator('.topactions .signin')
    assert post.is_visible() and register.is_visible() and login.is_visible()
    assert post.bounding_box()['x'] < register.bounding_box()['x'] < login.bounding_box()['x']
    register.click()
    assert p.locator('.modalhead h2').inner_text()=='Tạo tài khoản Foundly'
    assert p.locator('.modal input[placeholder="Nguyễn Văn A"]').is_visible()
    p.get_by_role('button',name='Đăng nhập',exact=True).last.click()
    assert p.locator('.modalhead h2').inner_text()=='Đăng nhập vào Foundly'
    p.get_by_role('button',name='Đăng ký ngay').click()
    assert p.locator('.modalhead h2').inner_text()=='Tạo tài khoản Foundly'
    p.locator('.modalclose').click()
    login.click()
    assert p.locator('.modalhead h2').inner_text()=='Đăng nhập vào Foundly'
    assert 'KẾT NỐI CỘNG ĐỒNG' in p.locator('.modalhead').inner_text()
    p.locator('.modalclose').click()
    post.click()
    assert p.locator('.modalhead h2').inner_text()=='Đăng nhập vào Foundly'
    assert not e,e
   finally:c.close()
  case('header post/register/login order and auth form navigation',header_auth_actions)
  def mobile_auth_actions():
   c,p,a,e=setup(browser,width=375)
   try:
    assert p.get_by_role('button',name='Mở menu').is_visible()
    assert not p.locator('.topactions .topregister').is_visible()
    p.get_by_role('button',name='Mở menu').click()
    assert p.locator('.mobile-nav-actions .mobile-post').is_visible()
    p.locator('.mobile-nav-actions').get_by_role('button',name='Đăng ký').click()
    assert p.locator('.modalhead h2').inner_text()=='Tạo tài khoản Foundly'
    assert not p.locator('.nav').is_visible()
    p.locator('.modalclose').click()
    p.get_by_role('button',name='Mở menu').click()
    p.locator('.mobile-nav-actions').get_by_role('button',name='Đăng nhập').click()
    assert p.locator('.modalhead h2').inner_text()=='Đăng nhập vào Foundly'
    p.locator('.modal input[type=email]').fill('owner@campus.edu')
    p.locator('.modal input[type=password]').fill('demo-password')
    p.locator('.modal button[type=submit]').click()
    p.get_by_role('button',name='Mở menu').click()
    assert p.locator('.mobile-nav-actions .mobile-post').is_visible()
    assert p.locator('.mobile-nav-actions button').count()==1  # Signed-in user needs no login/register actions.
    p.locator('.mobile-nav-actions .mobile-post').click()
    assert p.locator('.postform').is_visible()
    assert not e,e
   finally:c.close()
  case('mobile hamburger registration and login',mobile_auth_actions)
  def make_post():
   c,p,a,e=setup(browser,account=OWNER)
   try:
    p.get_by_role('button',name='Tôi bị mất đồ').click()
    p.locator('.postform input[maxlength="140"]').fill('Laptop Lenovo')
    p.locator('.postform textarea').fill('Máy màu xám')
    p.locator('.postform select').first.select_option('1')
    p.locator('.postform input[type=file]').set_input_files({'name':'test.png','mimeType':'image/png','buffer':b'fake image test'})
    p.get_by_role('button',name='Đăng tin ngay').click()
    p.get_by_text('Bài đăng của tôi',exact=True).first.wait_for(timeout=5000)
    assert any(x[0]=='POST' and x[1]=='/reports' and x[3].get('itemName')=='Laptop Lenovo' for x in a.calls)
    assert any(x[1].startswith('/uploads/reports/') for x in a.calls)
    assert any(x['itemName']=='Laptop Lenovo' for x in a.reports)
    assert not e,e
   finally:c.close()
  case('create report and photo upload',make_post)
  def return_post():
   c,p,a,e=setup(browser,account=OWNER)
   try:
    p.get_by_role('button',name='Mở menu').click() if p.get_by_role('button',name='Mở menu').is_visible() else None;p.get_by_role('button',name='Bài của tôi').click()
    p.wait_for_timeout(100)
    p.once('dialog',lambda dialog:dialog.accept())
    p.locator('.mycard').filter(has_text='Ví da đen').get_by_role('button',name=re.compile('Đã hoàn trả')).click()
    assert next(x for x in a.reports if x['itemName']=='Ví da đen')['status']=='RETURNED'
    assert any(x[0]=='PATCH' and x[1].endswith('/status') for x in a.calls)
    assert not e,e
   finally:c.close()
  case('my reports and mark returned',return_post)
  def chat():
   c,p,a,e=setup(browser,account=VIEWER)
   try:
    p.locator('.reportcard').filter(has_text='Ví da đen').click()
    p.get_by_role('button',name=re.compile('Liên hệ người đăng')).click()
    p.wait_for_timeout(150)
    assert any(x[0]=='POST' and x[1]=='/contacts' for x in a.calls),str(a.calls)
    assert any(x[0]=='GET' and x[1]=='/contacts' for x in a.calls)
    p.locator('#s2-draft').fill('Tôi vừa tìm thấy đồ');p.get_by_role('button',name='Gửi tin nhắn').click()
    p.wait_for_timeout(80)
    assert any(x[1]=='/messages' and x[0]=='POST' for x in a.calls)
    p.get_by_role('tab',name='Danh bạ').click()
    assert p.get_by_text('Phòng CTSV').is_visible()
    assert not e,e
   finally:c.close()
  case('contacts, send message and directory',chat)
  def coming_soon():
   c,p,a,e=setup(browser,account=OWNER)
   try:
    disallowed=('/claims','/matching','/notifications','/handovers','/operations')
    p.get_by_role('button',name='Mở menu').click() if p.get_by_role('button',name='Mở menu').is_visible() else None
    for label in ['Gợi ý phù hợp','Yêu cầu nhận đồ','Hỗ trợ & ký gửi','Phòng bàn giao']:
     p.locator('.nav button').filter(has_text=label).click()
     assert p.get_by_text('TÍNH NĂNG ĐANG PHÁT TRIỂN').is_visible()
     p.get_by_role('button',name='Mở menu').click() if p.get_by_role('button',name='Mở menu').is_visible() else None
    p.get_by_role('button',name='Thông báo').click()
    assert p.get_by_text('TÍNH NĂNG ĐANG PHÁT TRIỂN').is_visible()
    assert not any(x[1].startswith(disallowed) for x in a.calls),str(a.calls)
    assert not e,e
   finally:c.close()
  case('5 unsupported features do not call API',coming_soon)
  def layouts():
   failures=[]
   for width in [320,375,390,600,601,768,850,851,1024,1440]:
    c,p,a,e=setup(browser,width=width)
    try:
     links=p.locator('.topbar .nav > button')
     assert links.count()==7, f'wrong number of product nav links at {width}'
     if width >= 851:
      for j in range(7):
       if not links.nth(j).is_visible():failures.append(f'nav link {j+1} hidden at {width}')
      if p.get_by_role('button',name='Mở menu').is_visible():failures.append(f'unnecessary hamburger at {width}')
     else:
      if not p.get_by_role('button',name='Mở menu').is_visible():failures.append(f'mobile menu toggle missing at {width}')
      p.get_by_role('button',name='Mở menu').click()
      for j in range(7):
       if not links.nth(j).is_visible():failures.append(f'mobile nav link {j+1} hidden when opened at {width}')
     if p.evaluate('document.documentElement.scrollWidth > window.innerWidth'):failures.append(f'horizontal overflow at {width}')
     if e:failures+=e
    finally:c.close()
   assert not failures,failures
  case('responsive React interface: ten viewports',layouts)
  browser.close()
 print('TOTAL',len(report),'PASS',sum(x[1]=='PASS' for x in report),'FAIL',sum(x[1]=='FAIL' for x in report))
 if any(x[1]=='FAIL' for x in report):raise AssertionError('Frontend browser end-to-end tests failed')
if __name__=='__main__':run()
