import React, { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Search, Plus, Bell, Menu, MapPin, CalendarDays, ArrowUpRight, ArrowRight, PackageOpen, PackageCheck, ShieldCheck, LogOut, X, CheckCircle2, Sparkles, SlidersHorizontal, Images, LoaderCircle, ChevronLeft, Clock3, UserRound, ListChecks, LayoutDashboard, Check, Eye, Inbox, Zap, MessageCircle, LockKeyhole, RefreshCcw, Send, CalendarClock, MapPinned, MessagesSquare, CheckCheck } from 'lucide-react';
import './style.css';
import { useChatLogic } from './components/ChatLogic.jsx';
import Sprint2ContactsView from './components/Sprint2ContactsView.jsx';

// `sprint` keeps the existing repository's API contract; `full` enables the original Foundly Pro flows.
// Sprint/main backend supports /api/reports (aliases /api/items), Sprint 2 chat and directory.
// Do not set VITE_API_PROFILE=full until Full Operations endpoints are implemented and tested.
const fullBackend = import.meta.env.VITE_API_PROFILE === 'full';
const upcomingFeatures = {
  matching: { title: 'Gợi ý phù hợp', description: 'Thuật toán ghép tin mất/nhặt và API matching đang được phát triển.' },
  claims: { title: 'Yêu cầu nhận đồ', description: 'Xác minh bằng chứng và phê duyệt yêu cầu cần API claims.' },
  operations: { title: 'Hỗ trợ & ký gửi', description: 'Quy trình hỗ trợ và ký gửi sẽ được kết nối khi API operations hoàn thành.' },
  handovers: { title: 'Phòng bàn giao nâng cao', description: 'Đặt lịch và xác nhận bàn giao hai bên cần API handovers. Bạn có thể trao đổi qua Hộp thư hiện có.' },
  notifications: { title: 'Thông báo', description: 'Thông báo tự động cần API notifications. Hiện bạn có thể kiểm tra tin nhắn trong Hộp thư.' },
};

async function api(path, options={}) {
  const isForm=options.body instanceof FormData;
  const headers=isForm?{}:{'Content-Type':'application/json'};
  const response=await fetch('/api'+path,{credentials:'same-origin',...options,headers:{...headers,...options.headers}});
  const body=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(body.message||`Lỗi HTTP ${response.status}`);
  return body;
}
const time=(x)=>new Date(x).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'});
const isFound=(x)=>x==='FOUND';
const categories=[['ALL','Tất cả'],['LOST','Đồ bị mất'],['FOUND','Đồ nhặt được']];
const emptyReport=()=>({type:'LOST',itemName:'',description:'',color:'',brand:'',categoryId:'',locationId:'',locationDetail:'',privateIdentifier:'',eventTime:new Date(Date.now()-new Date().getTimezoneOffset()*60000).toISOString().slice(0,16)});

function App(){
  const [me,setMe]=useState(null),[page,setPage]=useState('discover'),[filter,setFilter]=useState('ALL'),[search,setSearch]=useState('');
  const [reports,setReports]=useState([]),[mine,setMine]=useState([]),[catalog,setCatalog]=useState({categories:[],locations:[]});
  const [selected,setSelected]=useState(null),[showAuth,setShowAuth]=useState(false),[authInitialMode,setAuthInitialMode]=useState('login'),[showPost,setShowPost]=useState(false),[editingReport,setEditingReport]=useState(null),[reportPage,setReportPage]=useState(1);
  const [notifications,setNotifications]=useState([]),[matches,setMatches]=useState([]),[selectedMine,setSelectedMine]=useState(null);
  const [myClaims,setMyClaims]=useState([]),[inbox,setInbox]=useState([]);
  const [selectedClaimRoom,setSelectedClaimRoom]=useState(null);
  const [chatReport,setChatReport]=useState(null);
  const [busy,setBusy]=useState(false),[notice,setNotice]=useState(null),[mobileNav,setMobileNav]=useState(false),[count,setCount]=useState(0);
  const sessionKeyRef=useRef(0); // Prevent old account responses from overwriting a switched account.
  const reportsRequestRef=useRef(0); // Only the newest search/filter response may update the feed.
  const notify=(text,error=false)=>{setNotice({text,error});setTimeout(()=>setNotice(null),4800)};
  const request=async(fn)=>{setBusy(true);try{await fn();}catch(err){notify(err.message,true);}finally{setBusy(false);}};
  const loadReports=async(index=1)=>{const requestId=++reportsRequestRef.current;const result=await api(`/reports?type=${filter==='ALL'?'':filter}&q=${encodeURIComponent(search)}&page=${index}`);if(requestId!==reportsRequestRef.current)return;setReports(previous=>index===1?result.items:[...previous,...result.items]);setCount(result.total);setReportPage(index)};
  const loadMine=async()=>{if(!me)return;const session=sessionKeyRef.current;const items=await api('/reports/mine');if(session===sessionKeyRef.current)setMine(items);};
  const loadNotifs=async()=>{if(!fullBackend||!me)return;const session=sessionKeyRef.current;const items=await api('/notifications');if(session===sessionKeyRef.current)setNotifications(items);};
  const loadClaims=async()=>{if(!fullBackend||!me)return;const session=sessionKeyRef.current;const [a,b]=await Promise.all([api('/claims/mine'),api('/claims/inbox')]);if(session===sessionKeyRef.current){setMyClaims(a);setInbox(b);}};
  const refresh=async()=>{await Promise.all(fullBackend?[loadReports(),loadMine(),loadNotifs(),loadClaims()]:[loadReports(),loadMine()].filter(Boolean))};
  useEffect(()=>{api('/auth/me').then(data=>setMe(data.user)).catch(()=>{});api('/reports/catalog').then(setCatalog).catch(()=>{});},[]);
  useEffect(()=>{loadReports().catch(err=>notify(err.message,true))},[filter,search]);
  useEffect(()=>{if(me){Promise.all(fullBackend?[loadMine(),loadNotifs(),loadClaims()]:[loadMine()]).catch(()=>{});}else{setMine([]);setMatches([]);setNotifications([]);setMyClaims([]);setInbox([]);}},[me]);
  // Refresh the notification bell automatically, including when someone posts in another session.
  useEffect(()=>{
    if(!fullBackend||!me?.id)return;
    let active=true;
    const check=async()=>{try{const items=await api('/notifications');if(active)setNotifications(items);}catch{/* Session may have expired; the next login refreshes notifications. */}};
    const timer=setInterval(check,12000);
    return ()=>{active=false;clearInterval(timer);};
  },[me?.id]);
  useEffect(()=>{if(page==='discover')loadReports().catch(()=>{});else if(page==='mine')loadMine().catch(()=>{});else if(fullBackend&&page==='claims')loadClaims().catch(()=>{});else if(fullBackend&&page==='notifications')loadNotifs().catch(()=>{});},[page]);
  const authDone=(user)=>{sessionKeyRef.current++;setMe(user);setShowAuth(false);notify(`Chào mừng ${user.fullName}!`)};
  const openAuth=(mode='login')=>{setAuthInitialMode(mode);setMobileNav(false);setShowAuth(true)};
  const openPost=(type='LOST')=>{if(!me){openAuth();return;}setEditingReport(null);setShowPost(type)};
  const editPost=r=>{setEditingReport(r);setShowPost(r.type)};
  const goto=(name)=>{setPage(name);setMobileNav(false);window.scrollTo({top:0,behavior:'smooth'})};
  // All Pro buttons are visible for planning, but unsupported flows NEVER make API requests.
  const gotoFeature=(name)=>{
    if(!fullBackend && upcomingFeatures[name]) { goto('upcoming-'+name); return; }
    if(!me) { openAuth(); return; }
    goto(name);
  };
  // The public discovery feed can display both LOST and FOUND; personal matches must be opposite type.
  const openMatches=async(id, reportType)=>{
    if(!fullBackend){notify('Matching đang chờ API từ backend Full Operations.');return;}
    setSelectedMine(id);
    setMatches([]);
    const results=await api(`/reports/${id}/matches`);
    const sourceType=reportType||mine.find(r=>r.id===id)?.type;
    const oppositeType=sourceType==='LOST'?'FOUND':sourceType==='FOUND'?'LOST':null;
    setMatches(oppositeType?results.filter(m=>m.report?.type===oppositeType):results);
    goto('matching');
  };
  const getMatches=(id)=>request(()=>openMatches(id));
  const activeReport=mine.find(r=>r.id===selectedMine);

  const logout=()=>request(async()=>{
    await api('/auth/logout',{method:'POST'});
    sessionKeyRef.current++;
    // Clear every personal view to prevent the previous user's data lingering after account switching.
    setMe(null);setNotifications([]);setMine([]);setMatches([]);setSelectedMine(null);
    setMyClaims([]);setInbox([]);
    setSelected(null);setChatReport(null);setShowAuth(false);setShowPost(false);setEditingReport(null);
    setSearch('');setFilter('ALL');goto('discover');notify('Đã đăng xuất. Bạn có thể đăng nhập tài khoản khác.');
  });
  return <div className="app">
    <header className="topbar"><div className="container topinner">
      <button className="brand" onClick={()=>goto('discover')}><span className="brandmark"><PackageOpen size={22}/></span><span>found<span className="brandhighlight">ly</span><small>Campus Lost & Found</small></span></button>
      <nav className={'nav '+(mobileNav?'navopen':'')}>
        <button className={page==='discover'?'active':''} onClick={()=>goto('discover')}>Khám phá</button>
        <button className={page==='mine'?'active':''} onClick={()=>me?goto('mine'):openAuth()}>Bài của tôi</button>
        <button className={page==='matching'||page==='upcoming-matching'?'active':''} onClick={()=>gotoFeature('matching')}>Gợi ý phù hợp{!fullBackend&&<span className="soon-label">Sắp có</span>}</button>
        <button className={page==='claims'||page==='upcoming-claims'?'active':''} onClick={()=>gotoFeature('claims')}>Yêu cầu nhận đồ{!fullBackend&&<span className="soon-label">Sắp có</span>}</button>
        <button className={page==='operations'||page==='upcoming-operations'?'active':''} onClick={()=>gotoFeature('operations')}>Hỗ trợ & ký gửi{!fullBackend&&<span className="soon-label">Sắp có</span>}</button>
        <button className={page==='handovers'||page==='upcoming-handovers'?'active':''} onClick={()=>gotoFeature('handovers')}>Phòng bàn giao{!fullBackend&&<span className="soon-label">Sắp có</span>}</button>
        <button className={page==='contacts'?'active':''} onClick={()=>goto('contacts')}>Liên hệ & Danh bạ</button>
        <div className="mobile-nav-actions">
          <button type="button" className="mobile-post" onClick={()=>{setMobileNav(false);openPost('LOST')}}><Plus size={16}/> Đăng tin</button>
          {!me&&<>
            <button type="button" onClick={()=>openAuth('register')}>Đăng ký</button>
            <button type="button" onClick={()=>openAuth('login')}>Đăng nhập</button>
          </>}
        </div>
      </nav>
      <div className="topactions">
        {me&&<button className={'iconbutton notificationbutton '+(page==='notifications'?'selected':'')} title={fullBackend?'Thông báo':'Thông báo · Sắp có'} aria-label="Thông báo" onClick={()=>gotoFeature('notifications')}><Bell size={19}/>{fullBackend&&notifications.some(x=>!x.isRead)&&<i className="dot"/>}</button>}
        <button className="btn primary topnew" onClick={()=>openPost('LOST')}><Plus size={17}/> Đăng tin</button>
        {me?<>
          <button className="userchip" onClick={()=>goto('mine')} title={`Tài khoản: ${me.fullName}`}><span className="avatar">{me.fullName.slice(0,1).toUpperCase()}</span><span className="userlabel">{me.fullName.split(' ').slice(-2).join(' ')}</span></button>
          <button type="button" className="logoutbutton" onClick={logout} disabled={busy} title="Đăng xuất để chuyển tài khoản" aria-label="Đăng xuất"><LogOut size={16}/><span>Đăng xuất</span></button>
        </>:<>
          <button className="btn outline topregister" onClick={()=>openAuth('register')}>Đăng ký</button>
          <button className="btn outline signin" onClick={()=>openAuth('login')}>Đăng nhập</button>
        </>}
        <button className="iconbutton hamburg" aria-label="Mở menu" aria-expanded={mobileNav} onClick={()=>setMobileNav(!mobileNav)}><Menu/></button>
      </div>
    </div></header>
    <main>
    {page==='discover'&&<>
      <section className="hero"><div className="container herogrid"><div className="hero-copy">
        <div className="eyebrow"><span className="sparkle"><Sparkles size={13}/></span> Kết nối lại những gì bị thất lạc</div>
        <h1>Thất lạc đồ?<br/><em>Đừng lo, có Foundly.</em></h1>
        <p>{fullBackend?'Nơi sinh viên đăng tin mất đồ, chia sẻ những vật đã nhặt được và tìm lại đồ qua gợi ý tương đồng thông minh.':'Nơi sinh viên đăng tin, tìm đồ và trao đổi riêng qua Hộp thư. Các chức năng nâng cao sẽ mở khi API được hoàn thiện.'}</p>
        <div className="hero-actions"><button className="btn primary lg" onClick={()=>openPost('LOST')}><PackageOpen size={19}/> Tôi bị mất đồ</button><button className="btn ghost lg" onClick={()=>openPost('FOUND')}><PackageCheck size={19}/> Tôi nhặt được đồ</button></div>
        <div className="hero-proof"><div className="proof-icons"><span>🎒</span><span>🔑</span><span>📱</span></div><div><strong>Kết nối cộng đồng trong trường</strong><small>Đăng tin nhanh • Bảo vệ thông tin riêng tư</small></div></div>
      </div><div className="hero-illustration"><div className="hero-glow"/><div className="floating floatone"><div className="floatingicon mint"><PackageCheck size={25}/></div><div><strong>Đã nhặt được!</strong><small>Chùm chìa khóa • Giảng đường A</small></div><CheckCircle2 className="floatingcheck" size={19}/></div><div className="centerobj"><div className="centerRing ringone"/><div className="centerRing ringtwo"/><div className="bag">🎒</div><div className="orbit orbitA">🔑</div><div className="orbit orbitB">📱</div><div className="orbit orbitC">💳</div></div><div className="floating floattwo"><div className="floatingicon peach"><Sparkles size={24}/></div><div><strong>Gợi ý tương đồng</strong><small>{fullBackend?'Hệ thống tìm đồ có đặc điểm giống nhau':'Sẽ mở khi backend hỗ trợ'}</small></div><span className="matchtick">{fullBackend?'98%':'Sắp có'}</span></div></div></div></section>
      <section className="searchsection container"><div className="searchpanel"><div className="searchbox"><Search size={20}/><input placeholder="Tìm điện thoại, ví, thẻ sinh viên, chìa khóa..." value={search} onChange={e=>setSearch(e.target.value)}/>{search&&<button className="tinyx" onClick={()=>setSearch('')}><X size={16}/></button>}</div><button className="btn primary searchbtn" onClick={()=>loadReports()}><Search size={18}/> Tìm kiếm</button></div></section>
      <section className="container feed"><div className="sectionheading"><div><span className="kicker">CẬP NHẬT MỚI NHẤT</span><h2>Đồ thất lạc quanh bạn</h2><p>Khám phá những món đồ vừa được đăng trong cộng đồng.</p></div><div className="counttag">{count} bài đăng</div></div>
      <div className="filters">{categories.map(([id,label])=><button key={id} className={'filterpill '+(filter===id?'on':'')} onClick={()=>setFilter(id)}>{id==='ALL'?<SlidersHorizontal size={15}/>:id==='LOST'?<PackageOpen size={15}/>:<PackageCheck size={15}/>} {label}</button>)}<button className="refresh" onClick={()=>refresh().catch(err=>notify(err.message,true))}><RefreshCcw size={15}/> Làm mới</button></div>
      <div className="cardgrid">{reports.map(r=><ReportCard key={r.id} report={r} onClick={()=>setSelected(r)} />)}</div>
      {reports.length===0&&<Empty icon={<Search/>} title="Chưa tìm thấy bài đăng phù hợp" detail="Thử một từ khóa khác hoặc đổi bộ lọc."/>}{reports.length<count&&<div className="loadmore"><button className="btn outline" onClick={()=>loadReports(reportPage+1).catch(e=>notify(e.message,true))}>Xem thêm bài đăng <ArrowRight size={16}/></button></div>}
      </section><section className="how"><div className="container"><span className="kicker">CÁCH FOUNDLY HOẠT ĐỘNG</span><h2>Ba bước để tìm lại đồ</h2><div className="steps"><div><span>01</span><PackageOpen size={26}/><h3>Đăng thông tin</h3><p>Chọn mất đồ hoặc nhặt được, mô tả đặc điểm và khu vực.</p></div><div><span>02</span><Sparkles size={26}/><h3>Nhận gợi ý</h3><p>{fullBackend?'Hệ thống so sánh nhiều đặc điểm, cả khi thứ tự đăng bài khác nhau.':'Tính năng sắp có khi kết nối backend đầy đủ.'}</p></div><div><span>03</span><ShieldCheck size={26}/><h3>Xác minh & nhận lại</h3><p>{fullBackend?'Cung cấp bằng chứng riêng tư trước khi hai bên xác nhận bàn giao.':'Chỉ khả dụng khi tích hợp claim và bàn giao.'}</p></div></div></div></section>
    </>}
    {page==='contacts'&&<section className="container privatepage contacts-integrated"><ContactsScreen user={me}/></section>}
    {!fullBackend && page.startsWith('upcoming-') && (()=>{
      const feature=upcomingFeatures[page.slice('upcoming-'.length)];
      return feature&&<section className="container privatepage upcoming-page" role="status">
        <div className="upcoming-panel"><span className="kicker">TÍNH NĂNG ĐANG PHÁT TRIỂN</span>
          <Sparkles size={36}/><h1>{feature.title}</h1><p>{feature.description}</p>
          <div className="upcoming-actions"><button className="btn primary" onClick={()=>goto('discover')}>Về trang chủ</button>
          <button className="btn outline" onClick={()=>goto('contacts')}>Mở Liên hệ & Danh bạ</button></div>
        </div>
      </section>;
    })()}
    {page==='mine'&&<section className="container privatepage"><PageHeader kicker="KHÔNG GIAN CỦA BẠN" title="Bài đăng của tôi" subtitle={fullBackend?'Theo dõi các bài đã đăng và xem gợi ý phù hợp.':'Theo dõi bài đã đăng, đánh dấu hoàn trả và liên hệ qua Hộp thư.'} action={<button className="btn primary" onClick={()=>openPost('LOST')}><Plus size={16}/> Đăng bài mới</button>}/><div className="cardgrid">{mine.map(r=><div key={r.id} className="mycard"><ReportCard report={r} onClick={()=>setSelected(r)}/><div className="cardactions"><span className={'status '+r.status.toLowerCase()}>{r.status}</span><button type="button" onClick={()=>fullBackend?getMatches(r.id):gotoFeature('matching')}><Sparkles size={15}/> Gợi ý{!fullBackend?' · Sắp có':''}</button>{fullBackend&&r.status==='EXPIRED'&&<button onClick={()=>request(async()=>{await api(`/operations/reports/${r.id}/renew`,{method:'PATCH'});notify('Đã gia hạn bài thêm 30 ngày.');await refresh()})}>Gia hạn</button>}{fullBackend&&r.status==='OPEN'&&<button onClick={()=>editPost(r)}><Eye size={14}/> Sửa</button>}{fullBackend&&r.status==='OPEN'&&<button onClick={()=>request(async()=>{await api(`/reports/${r.id}/close`,{method:'PATCH'});notify('Đã đóng bài');await refresh()})}><X size={14}/> Đóng bài</button>}{!fullBackend&&r.status==='OPEN'&&<button type="button" onClick={()=>request(async()=>{if(!window.confirm('Xác nhận vật phẩm đã được hoàn trả?'))return;await api(`/reports/${r.id}/status`,{method:'PATCH'});await refresh();notify('Đã đánh dấu hoàn trả')})}><CheckCheck size={15}/> Đã hoàn trả</button>}</div></div>)}</div>{mine.length===0&&<Empty icon={<PackageOpen/>} title="Chưa có bài đăng nào" detail="Đăng thông tin về đồ bạn mất hoặc nhặt được."/>}</section>}
    {page==='matching'&&<section className="container privatepage"><PageHeader kicker="MATCHING ENGINE" title="Gợi ý phù hợp" subtitle="Báo MẤT chỉ đối chiếu với bài NHẶT ĐƯỢC và ngược lại. Điểm số không phải bằng chứng sở hữu."/><div className="matchinglayout"><aside className="reportselector"><h3>Chọn bài đăng</h3>{mine.map(r=><button key={r.id} className={selectedMine===r.id?'chosen':''} onClick={()=>getMatches(r.id)}><span className={'tinybadge '+(isFound(r.type)?'found':'lost')}>{r.type}</span><span>{r.itemName}</span><ArrowRight size={15}/></button>)}{mine.length===0&&<p>Đăng nhập và tạo bài để nhận gợi ý.</p>}</aside><div className="matchlist"><h3>{selectedMine?(activeReport?.type==='LOST'?'Các bài NHẶT ĐƯỢC có khả năng trùng khớp':activeReport?.type==='FOUND'?'Các bài BÁO MẤT có khả năng trùng khớp':'Các bài đối lập có khả năng trùng khớp'):'Chọn một bài bên trái để xem kết quả'}</h3>{matches.map(m=><div className="matchrow" key={m.id}><div className="matchscore"><strong>{Math.round(m.score)}</strong><small>điểm</small></div><div className="matchdetails"><span className="hint">{Math.round(m.coverage*100)}% độ phủ dữ liệu · {m.comparedFeatures} tiêu chí</span><h4>{m.report.itemName}</h4><span>{m.report.location||m.report.locationDetail||'Chưa rõ địa điểm'}</span></div><button className="btn outline" onClick={()=>setSelected(m.report)}>Xem bài <ArrowUpRight size={14}/></button><button className="iconbutton" title="Ẩn gợi ý" onClick={()=>request(async()=>{await api(`/reports/matches/${m.id}/dismiss`,{method:'PATCH'});await getMatches(selectedMine)})}><X size={17}/></button></div>)}{selectedMine&&matches.length===0&&<><Empty icon={<Sparkles/>} title="Chưa có gợi ý phù hợp" detail={activeReport?.type==='LOST'?'Chưa có bài NHẶT ĐƯỢC đủ tương đồng. Bài BÁO MẤT khác sẽ không được đề xuất.':'Chưa có bài BÁO MẤT đủ tương đồng. Bài NHẶT ĐƯỢC khác sẽ không được đề xuất.'}/><div className="loadmore"><button className="btn outline" onClick={()=>{setSearch('');setFilter(activeReport?.type==='LOST'?'FOUND':'LOST');goto('discover')}}>Xem tất cả bài {activeReport?.type==='LOST'?'nhặt được':'báo mất'} <ArrowRight size={16}/></button></div></>}</div></div></section>}
    {page==='claims'&&<section className="container privatepage"><PageHeader kicker="XÁC MINH AN TOÀN" title="Yêu cầu nhận đồ" subtitle="Bằng chứng chỉ hiển thị cho người liên quan và quản trị viên."/><div className="sectionheading minor"><h3>Yêu cầu bạn đã gửi</h3></div><div className="listpanel">{myClaims.map(c=><ClaimRow key={c.id} c={c} onRoom={()=>{setSelectedClaimRoom(c.id);goto('handovers')}} onAct={action=>request(async()=>{if(action==='withdraw'){await api(`/claims/${c.id}/withdraw`,{method:'POST'});notify('Đã rút yêu cầu.');await refresh()}})}/>)}{myClaims.length===0&&<p className="muted padded">Bạn chưa gửi yêu cầu nhận đồ nào.</p>}</div><div className="sectionheading minor"><h3>Yêu cầu cần bạn xét duyệt</h3></div><div className="listpanel">{inbox.map(c=><ClaimRow key={c.id} c={c} inbox onRoom={()=>{setSelectedClaimRoom(c.id);goto('handovers')}} onAct={(action)=>request(async()=>{if(action==='revoke'){const reason=window.prompt('Lý do hủy chấp thuận (tối thiểu 10 ký tự):');if(!reason)return;await api(`/claims/${c.id}/revoke`,{method:'POST',body:JSON.stringify({reason})})}else{await api(`/claims/${c.id}/review`,{method:'PATCH',body:JSON.stringify({approve:action==='approve'})})}notify('Đã cập nhật yêu cầu');await refresh()})}/>)}{inbox.length===0&&<p className="muted padded">Chưa có yêu cầu cần xét duyệt.</p>}</div></section>}
    {page==='operations'&&me&&<OperationsHub mine={mine} notify={notify} onRefresh={refresh}/>}
    {page==='handovers'&&me&&<HandoverHub user={me} selectedClaimId={selectedClaimRoom} notify={notify} onRefresh={async()=>{await Promise.all([loadNotifs(),loadClaims(),loadMine()])}}/>}
    {page==='notifications'&&<section className="container privatepage"><PageHeader kicker="CẬP NHẬT DÀNH CHO BẠN" title="Thông báo" subtitle="Gợi ý ghép đồ và thay đổi trạng thái yêu cầu."/>
      <div className="notification-toolbar"><span>Chuông cập nhật tự động mỗi 12 giây.</span><button type="button" onClick={()=>request(loadNotifs)} disabled={busy}><RefreshCcw size={15}/> Làm mới thông báo</button></div>
      <div className="listpanel">{notifications.map(n=><button key={n.id} className={'notirow '+(!n.isRead?'unread':'')} onClick={()=>request(async()=>{await api(`/notifications/${n.id}/read`,{method:'PATCH'});if(['HANDOVER','MESSAGE','APPOINTMENT','RETURNED','REMINDER'].includes(n.type)){setSelectedClaimRoom(null);goto('handovers')}else if(n.type==='CLAIM'){goto('claims')}else if(['CASE','DISPUTE','MODERATION','CUSTODY','EXPIRY'].includes(n.type)){goto('operations')}else if(n.reportId){const item=await api(`/reports/${n.reportId}`);setSelected(item)}await loadNotifs()})}><span className="notiicon"><Bell size={18}/></span><span><strong>{n.type==='MATCH'?'Gợi ý trùng khớp':n.type==='CLAIM'?'Yêu cầu nhận đồ':n.type==='MESSAGE'?'Tin nhắn bàn giao':n.type==='APPOINTMENT'?'Lịch bàn giao':n.type==='HANDOVER'?'Xác nhận bàn giao':n.type==='RETURNED'?'Đã trao trả':n.type==='CASE'?'Kết quả phản ánh':n.type==='DISPUTE'?'Tranh chấp':n.type==='MODERATION'?'Kiểm duyệt':n.type==='CUSTODY'?'Ký gửi tại quầy':n.type==='REMINDER'?'Sắp đến lịch hẹn':n.type==='EXPIRY'?'Bài hết hạn':'Thông báo'}</strong><small>{n.content}</small><em>{time(n.createdAt)}</em></span>{!n.isRead&&<i className="newdot"/>}</button>)}{notifications.length===0&&<p className="muted padded">Chưa có thông báo.</p>}</div></section>}
    </main>
    <footer><div className="container footerinner"><div className="footerbrand"><span className="brandmark"><PackageOpen size={18}/></span><div><strong>foundly</strong><small>Dự án quản lý đồ thất lạc tại trường đại học</small></div></div><p>Made for campus communities · Phiên bản demo chạy localhost · {fullBackend?<span>Quản trị sử dụng giao diện riêng</span>:<span>Quản trị nâng cao đang phát triển</span>}</p><p>Bảo vệ quyền riêng tư • Xác minh trước khi bàn giao</p></div></footer>
    {showAuth&&<AuthModal initialMode={authInitialMode} onClose={()=>setShowAuth(false)} onSuccess={authDone} notify={notify} request={request}/>}
    {showPost&&<PostModal type={showPost} initial={editingReport} catalog={catalog} onClose={()=>{setShowPost(false);setEditingReport(null)}} notify={notify} request={request} onCreated={async(id,type)=>{setShowPost(false);setEditingReport(null);await refresh();setSearch('');if(fullBackend){setFilter(type==='LOST'?'FOUND':'LOST');await openMatches(id,type)}else{goto('mine')}}}/>}
    {selected&&<DetailModal report={selected} me={me} request={request} notify={notify} onClose={()=>setSelected(null)} onAuth={()=>{setSelected(null);openAuth()}} refresh={refresh} fullBackend={fullBackend} onChat={()=>{setChatReport(selected);setSelected(null)}}/>}
    {chatReport&&<Modal wide onClose={()=>setChatReport(null)}><ContactsScreen user={me} initialReport={chatReport} onClose={()=>setChatReport(null)}/></Modal>}
    {notice&&<div className={'toast ' +(notice.error?'error':'')}><span>{notice.error?'⚠':'✓'}</span> {notice.text}<button onClick={()=>setNotice(null)}><X size={15}/></button></div>}
    {busy&&<div className="loadingcorner"><LoaderCircle size={16} className="spin"/> Đang xử lý...</div>}
  </div>;
}

function HandoverHub({user,selectedClaimId,notify,onRefresh}) {
  const [rooms,setRooms]=useState([]),[active,setActive]=useState(null),[room,setRoom]=useState(null),[messages,setMessages]=useState([]);
  const [text,setText]=useState(''),[place,setPlace]=useState('Quầy bảo vệ - Tòa A'),[when,setWhen]=useState('');
  const [ack,setAck]=useState(false),[pending,setPending]=useState(false),[loading,setLoading]=useState(true);
  const roomIdRef=useRef(null);
  const appliedClaimRef=useRef(false);
  const loadRooms=async()=>{
    const list=await api('/handovers');setRooms(list);
    setActive(current=>{
      if(selectedClaimId&&!appliedClaimRef.current){const matched=list.find(x=>x.claimId===selectedClaimId);if(matched){appliedClaimRef.current=true;return matched.id;}}
      return list.some(x=>x.id===current)?current:list[0]?.id??null;
    });
    setLoading(false);
  };
  const loadActive=async(id)=>{
    if(!id)return;
    const [details,history]=await Promise.all([api(`/handovers/${id}`),api(`/handovers/${id}/messages`)]);
    if(roomIdRef.current===id){setRoom(details);setMessages(history);}
  };
  useEffect(()=>{appliedClaimRef.current=false;loadRooms().catch(e=>{setLoading(false);notify(e.message,true)});},[selectedClaimId,user.id]);
  useEffect(()=>{
    roomIdRef.current=active;setRoom(null);setMessages([]);setAck(false);
    if(active)loadActive(active).catch(e=>notify(e.message,true));
    const timer=setInterval(()=>{
      loadRooms().catch(()=>{});
      if(roomIdRef.current)loadActive(roomIdRef.current).catch(()=>{});
    },10000);
    return()=>clearInterval(timer);
  },[active]);
  const act=async(fn)=>{
    setPending(true);try{await fn();await loadRooms();if(roomIdRef.current)await loadActive(roomIdRef.current);await onRefresh();}
    catch(e){notify(e.message,true)}finally{setPending(false)}
  };
  const sendMessage=e=>{e.preventDefault();if(!text.trim()||!active)return;
    act(async()=>{await api(`/handovers/${active}/messages`,{method:'POST',body:JSON.stringify({content:text})});setText('');});
  };
  const propose=e=>{e.preventDefault();if(!active||!when)return;
    act(async()=>{await api(`/handovers/${active}/appointments`,{method:'POST',body:JSON.stringify({place,meetingAt:new Date(when).toISOString()})});notify('Đã gửi lịch đề xuất cho đối phương.');});
  };
  const respond=(yes)=>act(async()=>{await api(`/handovers/${active}/appointments/${room.pendingAppointment.id}`,{method:'PATCH',body:JSON.stringify({accept:yes})});notify(yes?'Đã đồng ý lịch gặp.':'Đã từ chối lịch gặp.');});
  const confirm=()=>act(async()=>{const result=await api(`/handovers/${active}/confirm`,{method:'POST'});notify(result.message);setAck(false);});
  const other=room&&(room.myRole==='HOLDER'?room.claimant:room.holder);
  const confirmedSelf=room&&(room.myRole==='HOLDER'?room.holderConfirmedAt:room.claimantConfirmedAt);
  const complete=room?.status==='COMPLETED';
  return <section className="container privatepage handover-page"><PageHeader kicker="BÀN GIAO AN TOÀN" title="Phòng bàn giao" subtitle="Chỉ hai tài khoản liên quan được nhắn tin, hẹn gặp và cùng xác nhận hoàn trả. Không hiển thị thông tin liên hệ cá nhân."/>
    <div className="handover-layout">
      <aside className="handover-sidebar"><h3><MessagesSquare size={18}/> Các phòng của bạn</h3>
        {rooms.map(r=><button key={r.id} className={'room-choice '+(active===r.id?'selected':'')} onClick={()=>setActive(r.id)}><strong>{r.itemName}</strong><span>{r.myRole==='HOLDER'?'Bạn là người nhặt':'Bạn là người mất'} · {r.myRole==='HOLDER'?r.claimant.fullName:r.holder.fullName}</span><small>{r.status==='COMPLETED'?'✓ Đã hoàn tất':r.status==='SCHEDULED'?'Đã chốt lịch':r.status==='AWAITING_CONFIRMATION'?'Chờ xác nhận':'Đang hẹn bàn giao'}</small></button>)}
        {!rooms.length&&<p className="handover-muted">{loading?'Đang tải phòng...':'Chưa có phòng. Khi yêu cầu nhận đồ được duyệt, phòng riêng sẽ xuất hiện ở đây.'}</p>}
      </aside>
      <div className="handover-main">
        {!active?<Empty icon={<MessagesSquare/>} title="Chưa có phòng bàn giao" detail="Gửi yêu cầu xác minh cho bài FOUND và đợi người nhặt chấp thuận."/>:!room?<p className="handover-muted">Đang tải thông tin phòng...</p>:<>
          <div className="handover-heading"><div><span className="kicker">PHÒNG RIÊNG · {room.itemName}</span><h2><UserRound size={19}/> {other.fullName}</h2><p>{other.verified?'Tài khoản email đã được xác minh trong hệ thống':'Tài khoản chưa xác minh email'} · {room.myRole==='HOLDER'?'Người yêu cầu nhận đồ':'Người đang giữ đồ'}</p></div><span className={'handover-status '+(complete?'done':'')}>{complete?'Đã hoàn tất':room.status==='AWAITING_CONFIRMATION'?'Đợi xác nhận từ hai bên':'Đang trao đổi'}</span></div>
          <div className="handover-chat"><div className="chat-help"><LockKeyhole size={15}/> Tin nhắn chỉ hiển thị giữa hai tài khoản trong phòng. Không gửi mật khẩu, OTP hoặc thông tin nhạy cảm.</div>
          <div className="chat-history">{messages.map(m=><div key={m.id} className={'chat-bubble '+(m.senderId===user.id?'mine':'theirs')}><div>{m.content}</div><small>{time(m.createdAt)}</small></div>)}{!messages.length&&<div className="chat-empty">Bạn có thể bắt đầu bằng lời chào và đề nghị gặp tại điểm bảo vệ của trường.</div>}</div>
          <form className="chat-send" onSubmit={sendMessage}><input aria-label="Tin nhắn" maxLength={1500} placeholder={complete?'Vụ việc đã hoàn thành':'Nhắn tin hẹn nhận đồ...'} value={text} onChange={e=>setText(e.target.value)} disabled={complete||pending||room.status==='DISPUTED'||room.status==='CANCELLED'}/><button className="btn primary" type="submit" disabled={complete||pending||room.status==='DISPUTED'||room.status==='CANCELLED'||!text.trim()}><Send size={16}/> Gửi</button></form></div>
          <div className="appointment-section"><h3><CalendarClock size={19}/> Lịch trao trả</h3><p className="handover-muted">Chọn địa điểm có người trực trong trường; chỉ gặp sau khi hai bên đã xác nhận lịch.</p>
            {room.acceptedAppointment&&!complete&&!room.holderConfirmedAt&&!room.claimantConfirmedAt&&room.status!=='DISPUTED'&&<button className="btn outline" disabled={pending} onClick={()=>act(async()=>{if(window.confirm('Hủy lịch hiện tại để hẹn lại?')){await api(`/handovers/${active}/appointments/${room.acceptedAppointment.id}/cancel`,{method:'PATCH'});notify('Đã hủy lịch, có thể đề xuất mới.')}})}>Hủy lịch để hẹn lại</button>}
            {room.acceptedAppointment&&<div className="appointment-approved"><strong><CheckCircle2 size={18}/> Lịch đã thống nhất</strong><span><MapPinned size={15}/>{room.acceptedAppointment.place}</span><span><Clock3 size={15}/>{time(room.acceptedAppointment.meetingAt)}</span></div>}
            {room.pendingAppointment&&<div className="appointment-pending"><strong>Đề xuất đang chờ phản hồi</strong><span>{room.pendingAppointment.place} · {time(room.pendingAppointment.meetingAt)}</span>{room.pendingAppointment.proposedById!==user.id?<div className="room-actions"><button className="btn primary" disabled={pending} onClick={()=>respond(true)}><Check size={15}/> Đồng ý</button><button className="btn outline" disabled={pending} onClick={()=>respond(false)}><X size={15}/> Từ chối</button></div>:<small>Đang chờ đối phương đồng ý.</small>}</div>}
            {!complete&&room.status!=='DISPUTED'&&room.status!=='CANCELLED'&&!room.holderConfirmedAt&&!room.claimantConfirmedAt&&<form className="appointment-form" onSubmit={propose}><label>Địa điểm nhận đồ<select required value={place} onChange={e=>setPlace(e.target.value)}>{room.safePlaces.map(x=><option key={x} value={x}>{x}</option>)}</select></label><label>Ngày, giờ đề xuất<input required type="datetime-local" value={when} onChange={e=>setWhen(e.target.value)}/></label><button className="btn outline" disabled={pending||!when} type="submit"><CalendarDays size={16}/> Đề xuất lịch {room.acceptedAppointment?'mới':''}</button></form>}
          </div>
          <RoomIssueForm room={room} notify={notify} onDone={()=>loadActive(active)} />
          <div className="handover-confirm"><h3><CheckCheck size={19}/> Xác nhận bàn giao</h3><p>Chỉ xác nhận <strong>sau khi đã trực tiếp giao hoặc nhận món đồ</strong> tại địa điểm đã hẹn. Không nhấn trước khi gặp.</p><div className="confirm-grid"><div><strong>Người nhặt: {room.holder.fullName}</strong><small>{room.holderConfirmedAt?'✓ Đã xác nhận':'Đang chờ xác nhận'}</small></div><div><strong>Người mất: {room.claimant.fullName}</strong><small>{room.claimantConfirmedAt?'✓ Đã xác nhận':'Đang chờ xác nhận'}</small></div></div>
            {!complete&&room.status!=='DISPUTED'&&room.status!=='CANCELLED'&&!confirmedSelf&&room.acceptedAppointment&&<><label className="confirm-check"><input type="checkbox" checked={ack} onChange={e=>setAck(e.target.checked)}/> Tôi xác nhận mình đã {room.myRole==='HOLDER'?'trực tiếp giao':'thực sự nhận'} đúng vật phẩm.</label><button className="btn primary" disabled={!ack||pending} onClick={confirm}><CheckCheck size={17}/> Xác nhận {room.myRole==='HOLDER'?'đã giao đồ':'đã nhận đồ'}</button></>}
            {!room.acceptedAppointment&&!complete&&<p className="handover-muted">Cần có lịch được cả hai chấp thuận trước khi xác nhận bàn giao.</p>}
            {complete&&<p className="handover-complete"><CheckCircle2 size={18}/> Đã hoàn thành. Cả hai bên xác nhận và bài nhặt được chuyển thành RETURNED.</p>}
          </div>
        </>}
      </div>
    </div>
  </section>;
}

function ReportFlag({reportId,request,notify}){
 const [open,setOpen]=useState(false),[reason,setReason]=useState('');
 const submit=e=>{e.preventDefault();request(async()=>{await api('/operations/cases',{method:'POST',body:JSON.stringify({type:'REPORT_ABUSE',reportId,reason})});notify('Đã gửi báo cáo tới quản trị viên.');setReason('');setOpen(false)})};
 return <div className="ops-inline"><button className="btn outline small" onClick={()=>setOpen(!open)}>Báo cáo bài đăng vi phạm</button>{open&&<form onSubmit={submit} className="ops-form"><label>Mô tả nội dung vi phạm<textarea minLength={15} maxLength={2000} required value={reason} onChange={e=>setReason(e.target.value)}/></label><button className="btn primary small" type="submit">Gửi báo cáo</button></form>}</div>
}
function RoomIssueForm({room,notify,onDone}){
 const [kind,setKind]=useState('HANDOVER_DISPUTE'),[reason,setReason]=useState(''),[working,setWorking]=useState(false);
 if(['COMPLETED','CANCELLED'].includes(room.status))return null;
 const submit=async e=>{e.preventDefault();setWorking(true);try{await api('/operations/cases',{method:'POST',body:JSON.stringify({type:kind,roomId:room.id,reason})});notify('Đã chuyển phòng sang chờ Admin giải quyết.');setReason('');await onDone()}catch(e){notify(e.message,true)}finally{setWorking(false)}};
 return <div className="ops-panel"><h3>Báo vắng mặt / tranh chấp</h3><p>Nếu đối phương không đến đúng lịch hoặc có sự cố, gửi báo cáo để Admin xem xét. Phòng sẽ tạm khóa thao tác bàn giao.</p><form className="ops-form" onSubmit={submit}><label>Loại sự cố<select value={kind} onChange={e=>setKind(e.target.value)}><option value="HANDOVER_DISPUTE">Tranh chấp bàn giao</option><option value="NO_SHOW">Một bên không đến điểm hẹn</option></select></label><label>Chi tiết<textarea required minLength={15} maxLength={2000} value={reason} onChange={e=>setReason(e.target.value)}/></label><button className="btn outline" disabled={working||room.status==='DISPUTED'} type="submit">Gửi yêu cầu hỗ trợ Admin</button>{room.status==='DISPUTED'&&<small>Đang chờ quản trị viên xử lý.</small>}</form></div>
}
function OperationsHub({mine,notify,onRefresh}){
 const [cases,setCases]=useState([]),[custody,setCustody]=useState([]),[desks,setDesks]=useState([]),[reportId,setReportId]=useState(''),[desk,setDesk]=useState(''),[loading,setLoading]=useState(true);
 const refresh=async()=>{const [a,b,c]=await Promise.all([api('/operations/cases/mine'),api('/operations/custody/mine'),api('/operations/custody/desks')]);setCases(a);setCustody(b);setDesks(c);setDesk(d=>d||c[0]||'');setLoading(false)};
 useEffect(()=>{refresh().catch(e=>{notify(e.message,true);setLoading(false)})},[]);
 const request=e=>{e.preventDefault();api(`/operations/custody/${reportId}`,{method:'POST',body:JSON.stringify({desk})}).then(async x=>{notify(x.message);await refresh()}).catch(e=>notify(e.message,true))};
 return <section className="container privatepage"><PageHeader kicker="QUẢN LÝ NGOẠI LỆ" title="Hỗ trợ & ký gửi" subtitle="Tra cứu kết quả báo cáo, gia hạn bài đăng và gửi vật phẩm nhặt được vào quầy bảo vệ."/>
  <div className="ops-columns"><div className="ops-panel"><h3>Yêu cầu ký gửi đồ nhặt được</h3><p>Chọn bài FOUND và quầy tiếp nhận. Nhân viên Admin xác nhận ký gửi bằng mã biên nhận; không tự động coi đồ đã giao nếu chưa tiếp nhận thực tế.</p><form className="ops-form" onSubmit={request}><label>Bài nhặt được<select required value={reportId} onChange={e=>setReportId(e.target.value)}><option value="">Chọn bài nhặt được</option>{mine.filter(r=>r.type==='FOUND'&&r.status==='OPEN').map(r=><option value={r.id} key={r.id}>{r.itemName}</option>)}</select></label><label>Quầy bảo vệ<select required value={desk} onChange={e=>setDesk(e.target.value)}>{desks.map(d=><option key={d}>{d}</option>)}</select></label><button type="submit" className="btn primary" disabled={!reportId}>Gửi yêu cầu ký gửi</button></form></div>
  <div className="ops-panel"><h3>Hồ sơ ký gửi</h3>{custody.map(c=><p key={c.id}><strong>{mine.find(x=>x.id===c.reportId)?.itemName||'Đồ nhặt được'}</strong> · {c.desk}<br/><small>Trạng thái: {c.status}{c.receivedAt?' · đã tiếp nhận '+time(c.receivedAt):''}</small></p>)}{!custody.length&&<p>Chưa có hồ sơ nào.</p>}</div></div>
  <div className="ops-panel"><h3>Phản ánh của tôi</h3>{cases.map(c=><div className="ops-record" key={c.id}><strong>{c.type} · {c.status}</strong><span>{c.reason}</span>{c.resolution&&<em>Admin: {c.resolution}</em>}<small>{time(c.createdAt)}</small></div>)}{loading?<p>Đang tải...</p>:!cases.length&&<p>Chưa có phản ánh nào.</p>}</div>
  <button className="btn outline" onClick={()=>{refresh().catch(e=>notify(e.message,true));onRefresh()}}>Làm mới hồ sơ</button>
 </section>
}
function PageHeader({kicker,title,subtitle,action}){return <div className="pagehead"><div><span className="kicker">{kicker}</span><h1>{title}</h1><p>{subtitle}</p></div>{action}</div>}
function ReportCard({report:r,onClick}){const cover=r.images?.[0];const returned=r.status==='RETURNED';return <button className={'reportcard '+(returned?'returned-card':'')} onClick={onClick}><div className={'cardphoto '+(isFound(r.type)?'aqua':'sand')}>{cover?<img src={cover} alt={r.itemName} loading="lazy"/>:<div className="itememoji">{r.category==='Điện tử'?'📱':r.category==='Giấy tờ'?'🪪':r.category==='Ví & túi'?'👛':r.category==='Chìa khóa'?'🔑':'🎒'}</div>}<span className={'reportbadge '+(returned?'returned':(isFound(r.type)?'found':'lost'))}>{returned?'ĐÃ TRẢ':isFound(r.type)?'ĐÃ NHẶT ĐƯỢC':'ĐANG TÌM'}</span></div><div className="cardbody"><div className="cardmeta"><span>{r.category||'Vật dụng khác'}</span><span>·</span><span>{time(r.createdAt).split(',')[0]}</span></div><h3>{r.itemName}</h3><p>{r.description||'Chưa có mô tả bổ sung.'}</p><div className="cardbottom"><span><MapPin size={15}/> {r.location||r.locationDetail||'Chưa rõ khu vực'}</span><ArrowUpRight size={17}/></div></div></button>}
function Empty({icon,title,detail}){return <div className="emptystate"><span>{icon}</span><h3>{title}</h3><p>{detail}</p></div>}
function Modal({children,onClose,wide}){useEffect(()=>{const f=e=>{if(e.key==='Escape')onClose()};document.addEventListener('keydown',f);document.body.style.overflow='hidden';return()=>{document.removeEventListener('keydown',f);document.body.style.overflow=''}},[onClose]);return <div className="overlay" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}><div className={'modal '+(wide?'wide':'')}><button className="modalclose" onClick={onClose}><X size={19}/></button>{children}</div></div>}
function AuthModal({initialMode='login',onClose,onSuccess,notify,request}){
 const [mode,setMode]=useState(initialMode),[email,setEmail]=useState(''),[password,setPassword]=useState(''),[fullName,setFullName]=useState(''),[code,setCode]=useState('');
 const submit=e=>{e.preventDefault();request(async()=>{
   if(mode==='login'){const data=await api('/auth/login',{method:'POST',body:JSON.stringify({email,password})});onSuccess(data.user)}
   if(mode==='register'){const data=await api('/auth/register',{method:'POST',body:JSON.stringify({email,password,fullName})});setCode(data.devVerificationCode||'');setMode('verify');notify('Đã tạo tài khoản. Hãy xác minh email.');}
   if(mode==='verify'){await api('/auth/verify',{method:'POST',body:JSON.stringify({email,code})});notify('Xác minh thành công!');setMode('login');}
 })};
 return <Modal onClose={onClose}><div className="modalhead"><span className="modalsymbol"><ShieldCheck size={22}/></span><span className="kicker">FOUNDLY · KẾT NỐI CỘNG ĐỒNG</span><h2>{mode==='login'?'Đăng nhập vào Foundly':mode==='register'?'Tạo tài khoản Foundly':'Xác minh email'}</h2><p>{mode==='verify'?'Nhập mã xác minh để hoàn tất đăng ký. Trong localhost, mã thử nghiệm được hiển thị bên dưới.':mode==='register'?'Tham gia cộng đồng để báo mất, đăng tin nhặt được và trao đổi an toàn.':'Đăng nhập để đăng tin, tìm kiếm và liên hệ về đồ thất lạc trong trường.'}</p></div><form onSubmit={submit} className="form">
 {mode==='register'&&<label>Họ và tên<input required minLength={2} maxLength={120} value={fullName} onChange={e=>setFullName(e.target.value)} placeholder="Nguyễn Văn A"/></label>}
 <label>Email{mode==='login'?'':' của bạn'}<input required type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="sinhvien@campus.edu" readOnly={mode==='verify'}/></label>
 {mode==='verify'?<label>Mã xác minh<input value={code} onChange={e=>setCode(e.target.value)} required pattern="[0-9]{6}" maxLength={6}/><small>Chế độ Development: mã do API trả về khi đăng ký. Hệ thống chưa gửi email thật.</small></label>:<label>Mật khẩu<input type="password" value={password} onChange={e=>setPassword(e.target.value)} required minLength={mode==='register'?10:1} placeholder="••••••••••"/></label>}
 <button className="btn primary submit" type="submit">{mode==='login'?'Đăng nhập':mode==='register'?'Tạo tài khoản':'Xác minh'} <ArrowRight size={17}/></button>
 </form>{mode!=='verify'&&<p className="modalswitch">{mode==='login'?'Chưa có tài khoản?':'Đã có tài khoản?'} <button onClick={()=>setMode(mode==='login'?'register':'login')}>{mode==='login'?'Đăng ký ngay':'Đăng nhập'}</button></p>}
 </Modal>
}
function PostModal({type,initial,catalog,onClose,notify,request,onCreated}){
 const [form,setForm]=useState(initial?{...emptyReport(),...initial,categoryId:initial.categoryId??'',locationId:initial.locationId??'',privateIdentifier:'',eventTime:new Date(new Date(initial.eventTime).getTime()-new Date().getTimezoneOffset()*60000).toISOString().slice(0,16)}:{...emptyReport(),type}),[images,setImages]=useState([]);
 const update=(key,value)=>setForm(x=>({...x,[key]:value}));
 const submit=e=>{e.preventDefault();request(async()=>{
   const body={...form,categoryId:form.categoryId?Number(form.categoryId):null,locationId:form.locationId?Number(form.locationId):null,eventTime:new Date(form.eventTime).toISOString()};
   const result=initial?await api(`/reports/${initial.id}`,{method:'PUT',body:JSON.stringify(body)}):await api('/reports',{method:'POST',body:JSON.stringify(body)});
   const id=initial?.id||result.id;
   for(const file of images){const fd=new FormData();fd.append('file',file);await api(`/uploads/reports/${id}`,{method:'POST',body:fd})}
   notify(initial?'Đã cập nhật bài đăng.':(fullBackend?'Đã đăng tin. Đang kiểm tra các bài đăng đối lập.':'Đã đăng tin thành công.'));await onCreated(id,form.type);
 })};
 return <Modal wide onClose={onClose}><div className="modalhead left"><span className="kicker">ĐĂNG THÔNG TIN ĐỒ THẤT LẠC</span><h2>{initial?'Chỉnh sửa bài đăng':form.type==='LOST'?'Tôi đang tìm đồ':'Tôi đã nhặt được đồ'}</h2><p>Càng mô tả rõ, gợi ý tương đồng càng hữu ích. Tránh công khai các đặc điểm dùng để xác minh chủ sở hữu.</p></div>
 <form className="form postform" onSubmit={submit}><div className="twotabs"><button type="button" disabled={!!initial} className={form.type==='LOST'?'picked':''} onClick={()=>update('type','LOST')}><PackageOpen size={16}/> Báo mất đồ</button><button type="button" disabled={!!initial} className={form.type==='FOUND'?'picked':''} onClick={()=>update('type','FOUND')}><PackageCheck size={16}/> Báo nhặt được</button></div>
 <label className="full">Tên vật phẩm <i>*</i><input required maxLength={140} value={form.itemName} onChange={e=>update('itemName',e.target.value)} placeholder="Ví dụ: Ví da màu đen, điện thoại Samsung..."/></label>
 <label className="full">Mô tả<textarea rows={3} maxLength={3500} value={form.description} onChange={e=>update('description',e.target.value)} placeholder="Đặc điểm bên ngoài, vật dụng liên quan, nơi nhìn thấy gần nhất..."/></label>
 <label>Nhóm vật phẩm<select value={form.categoryId} onChange={e=>update('categoryId',e.target.value)}><option value="">Không rõ / Khác</option>{catalog.categories.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
 <label>Khu vực<select value={form.locationId} onChange={e=>update('locationId',e.target.value)}><option value="">Chưa rõ khu vực</option>{catalog.locations.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
 <label>Vị trí cụ thể<input value={form.locationDetail} maxLength={300} onChange={e=>update('locationDetail',e.target.value)} placeholder="Tầng 2, cạnh thang máy..."/></label>
 <label>Thời gian mất / nhặt <i>*</i><input required type="datetime-local" value={form.eventTime} onChange={e=>update('eventTime',e.target.value)} /></label>
 <label>Màu sắc<input maxLength={80} value={form.color} onChange={e=>update('color',e.target.value)} placeholder="Đen, xanh, trắng..."/></label><label>Thương hiệu<input maxLength={80} value={form.brand} onChange={e=>update('brand',e.target.value)} placeholder="Apple, Samsung..."/></label>
 {form.type==='FOUND'&&<label className="full">Đặc điểm xác minh bí mật <span className="optional">(chỉ người đăng được biết)</span><input maxLength={200} value={form.privateIdentifier} onChange={e=>update('privateIdentifier',e.target.value)} placeholder="Dấu hiệu chỉ chủ sở hữu biết, không công khai"/></label>}
 <label className="full uploadlabel"><Images size={20}/><strong>Ảnh vật phẩm (tối đa 4 ảnh)</strong><small>JPG / PNG / WebP · tối đa 5 MB mỗi ảnh</small><input type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={e=>setImages([...e.target.files].slice(0,4))}/>{images.length>0&&<span>{images.length} ảnh đã chọn</span>}</label>
 <div className="full formfoot"><p><LockKeyhole size={16}/> Không công khai email hoặc thông tin xác minh riêng tư.</p><button type="submit" className="btn primary lg"><Plus size={17}/> {initial?'Lưu chỉnh sửa':'Đăng tin ngay'}</button></div>
 </form></Modal>
}
function DetailModal({report:r,me,onClose,onAuth,onChat,request,notify,refresh,fullBackend}){
 const [evidence,setEvidence]=useState(''),[sending,setSending]=useState(false);
 const submit=e=>{e.preventDefault();request(async()=>{setSending(true);try{await api('/claims',{method:'POST',body:JSON.stringify({foundReportId:r.id,evidence})});notify('Đã gửi yêu cầu xác minh.');await refresh();onClose();}finally{setSending(false)}})};
 return <Modal wide onClose={onClose}><div className="detailgrid"><div className="detailvisual">{r.images?.length?<img src={r.images[0]} alt={r.itemName}/>:<div className="detailplaceholder">{isFound(r.type)?'📦':'🎒'}</div>}</div><div className="detailbody"><span className={'reportbadge inline '+(r.status==='RETURNED'?'returned':(isFound(r.type)?'found':'lost'))}>{r.status==='RETURNED'?'ĐÃ TRẢ':isFound(r.type)?'ĐÃ NHẶT ĐƯỢC':'ĐANG TÌM'}</span><h2>{r.itemName}</h2><div className="detailfacts"><span><MapPin size={16}/> {r.location||r.locationDetail||'Chưa rõ địa điểm'}</span><span><CalendarDays size={16}/> {time(r.eventTime)}</span><span><PackageOpen size={16}/> {r.category||'Nhóm khác'}</span></div><h4>Mô tả</h4><p className="description">{r.description||'Người đăng chưa bổ sung mô tả.'}</p>{(r.color||r.brand)&&<div className="attributes">{r.color&&<span>Màu: <strong>{r.color}</strong></span>}{r.brand&&<span>Hãng: <strong>{r.brand}</strong></span>}</div>}
 <div className="privacycallout"><ShieldCheck size={21}/><p>{fullBackend?'Bằng chứng nhận đồ sẽ chỉ hiển thị cho người giữ đồ và quản trị viên. Điểm gợi ý không thay thế bước xác minh.':'Bạn có thể trao đổi riêng qua Hộp thư Sprint 2. Yêu cầu xác minh và bàn giao nâng cao đang chờ backend.'}</p></div>
 {me&&me.id!==r.userId&&r.status==='OPEN'&&<button type="button" className="btn primary" onClick={onChat}><MessageCircle size={16}/> Liên hệ người đăng qua hộp thư</button>}
 {!me&&<button type="button" className="btn outline" onClick={onAuth}>Đăng nhập để liên hệ</button>}
 {fullBackend&&me&&<ReportFlag reportId={r.id} request={request} notify={notify}/>}
 {fullBackend&&isFound(r.type)&&r.status==='OPEN'&&me?.id!==r.userId&&(me?<form className="claimbox" onSubmit={submit}><h4>Đây có thể là đồ của bạn?</h4><p>Hãy mô tả đặc điểm bí mật, phụ kiện, vị trí hoặc bằng chứng sở hữu mà bài công khai chưa nêu.</p><textarea required minLength={12} maxLength={4000} rows={3} value={evidence} onChange={e=>setEvidence(e.target.value)} placeholder="Ví dụ: đặc điểm riêng, phụ kiện, ảnh/hóa đơn đã có... (không gửi mật khẩu hoặc OTP)"/><button className="btn primary" type="submit" disabled={sending}><ShieldCheck size={16}/> Gửi yêu cầu xác minh</button></form>:<button className="btn primary" onClick={onAuth}>Đăng nhập để nhận đồ</button>)}
 </div></div></Modal>
}
function ClaimRow({c,inbox,onAct,onRoom}){return <div className="claimrow"><div className="claimicon"><ShieldCheck size={22}/></div><div className="claimcontent"><div className="claimtitle"><strong>{c.reportName||'Đồ nhặt được'}</strong><span className={'status '+c.status.toLowerCase()}>{c.status}</span></div><p><strong>Bằng chứng riêng:</strong> {c.evidence}</p><small>{time(c.createdAt)} {c.handedOverAt?' · Đã nhận lại':''}</small></div>{inbox&&c.status==='PENDING'&&<div className="rowbuttons"><button className="btn small primary" onClick={()=>onAct('approve')}><Check size={14}/> Duyệt</button><button className="btn small outline" onClick={()=>onAct('reject')}><X size={14}/> Từ chối</button></div>}{c.status==='APPROVED'&&!c.handedOverAt&&<button className="btn small primary" onClick={onRoom}><MessagesSquare size={15}/> Mở phòng bàn giao</button>}{!inbox&&['PENDING','APPROVED'].includes(c.status)&&<button className="btn small outline" onClick={()=>onAct('withdraw')}>Rút yêu cầu</button>}{inbox&&c.status==='APPROVED'&&!c.handedOverAt&&<button className="btn small outline" onClick={()=>onAct('revoke')}>Hủy chấp thuận</button>}{c.handedOverAt&&<span className="status returned">Đã hoàn tất</span>}</div>}

// Preserve the verified #15 live API adapter and #14 presentation inside the Pro layout.
function ContactsScreen({user, initialReport=null, onClose=null}) {
  const chat = useChatLogic({user,initialReport});
  return <Sprint2ContactsView initialReport={initialReport} onClose={onClose} live={chat}/>;
}

createRoot(document.getElementById('root')).render(<App/>);
