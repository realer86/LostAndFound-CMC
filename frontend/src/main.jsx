import ChatLogic from './components/ChatLogic';
import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Search, Plus, MapPin, ArrowUpRight, ArrowRight, PackageOpen, PackageCheck, ShieldCheck,
  LogOut, X, Images, LockKeyhole, CalendarDays } from 'lucide-react';
import './style.css';

// Giữ nguyên helper fetch/cookie từ Final; Vite proxy giúp dùng cùng origin.
async function api(path, options={}) {
  const isForm=options.body instanceof FormData;
  const headers=isForm?{}:{'Content-Type':'application/json'};
  const response=await fetch('/api'+path,{credentials:'same-origin',...options,headers:{...headers,...options.headers}});
  const body=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(body.message||`Lỗi HTTP ${response.status}`);
  return body;
}
const time=x=>new Date(x).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'});
const isFound=x=>x==='FOUND';
const emptyReport=()=>({type:'LOST',itemName:'',description:'',color:'',brand:'',categoryId:'',locationId:'',locationDetail:'',privateIdentifier:'',eventTime:new Date(Date.now()-new Date().getTimezoneOffset()*60000).toISOString().slice(0,16)});

function App(){
  const [me,setMe]=useState(null);
  const [reports,setReports]=useState([]),[mine,setMine]=useState([]);
  const [catalog,setCatalog]=useState({categories:[],locations:[]});
  const [selected,setSelected]=useState(null),[showAuth,setShowAuth]=useState(false),[showPost,setShowPost]=useState(null);
  const [tab,setTab]=useState('discover'),[filter,setFilter]=useState('ALL'),[search,setSearch]=useState('');
  const [busy,setBusy]=useState(false),[notice,setNotice]=useState(null);
  const notify=(message,error=false)=>setNotice({message,error});
  const request=async(fn)=>{setBusy(true);try{await fn()}catch(err){notify(err.message,true)}finally{setBusy(false)}};
  const load=async()=>{
    const response=await api('/items?pageSize=40');setReports(response.items);
    if(me){setMine(await api('/items/mine'))}else{setMine([])}
  };
  useEffect(()=>{
    api('/auth/me').then(r=>setMe(r.user)).catch(()=>{});
    api('/items/catalog').then(setCatalog).catch(e=>notify(e.message,true));
  },[]);
  useEffect(()=>{load().catch(e=>notify(e.message,true))},[me]);
  const visible=(tab==='mine'?mine:reports).filter(r=>
    (filter==='ALL'||r.type===filter)&&
    (r.itemName.toLowerCase().includes(search.toLowerCase())||r.description?.toLowerCase().includes(search.toLowerCase())));
  const post=type=>{if(!me){setShowAuth(true);return}setShowPost(type)};
  const logout=()=>request(async()=>{await api('/auth/logout',{method:'POST'});setMe(null);setTab('discover');setSelected(null);notify('Đã đăng xuất')});
  return <div className="app">
    <header className="topbar"><div className="container topinner">
      <button className="brand" onClick={()=>setTab('discover')}><span className="brandmark"><PackageOpen size={22}/></span>found<span className="brandhighlight">ly</span><small> Campus · Sprint 1</small></button>
      <nav className="nav"><button className={tab==='discover'?'active':''} onClick={()=>setTab('discover')}>Trang chủ</button><button className={tab==='mine'?'active':''} onClick={()=>me?setTab('mine'):setShowAuth(true)}>Bài đăng của tôi</button></nav>
      <div className="topactions">{me?<><span>{me.fullName}</span><button className="btn outline" disabled={busy} onClick={logout}><LogOut size={16}/> Đăng xuất</button></>:<button className="btn outline" onClick={()=>setShowAuth(true)}>Đăng nhập / Đăng ký</button>}</div>
    </div></header>
    <main className="container"><section className="hero"><span className="kicker">FOUNDLY · LOST & FOUND</span><h1>Đăng tin và tìm đồ thất lạc</h1><p>Demo Sprint 1: cơ sở dữ liệu, API đăng tin và Form với danh mục / khu vực.</p><div className="hero-actions"><button className="btn primary" onClick={()=>post('LOST')}><Plus size={17}/> Tôi bị mất đồ</button><button className="btn outline" onClick={()=>post('FOUND')}><PackageCheck size={17}/> Tôi nhặt được đồ</button></div></section>
      <section className="feed"><div className="sectionheading"><div><span className="kicker">{tab==='mine'?'CỦA BẠN':'BÀI ĐĂNG MỚI'}</span><h2>{tab==='mine'?'Bài đăng của tôi':'Khám phá bài đăng'}</h2></div><button className="btn outline" onClick={()=>request(load)}>Làm mới</button></div>
        <div className="filters"><label><Search size={16}/><input placeholder="Tìm kiếm vật phẩm..." value={search} onChange={e=>setSearch(e.target.value)}/></label><select value={filter} onChange={e=>setFilter(e.target.value)}><option value="ALL">Tất cả loại tin</option><option value="LOST">Đồ bị mất</option><option value="FOUND">Đồ nhặt được</option></select></div>
        <div className="cardgrid">{visible.map(r=><ReportCard key={r.id} report={r} onClick={()=>setSelected(r)}/>)}</div>
        {!visible.length&&<p className="emptystate">Chưa có bài đăng phù hợp. Hãy thử tạo bài mới.</p>}
      </section>
    </main>
    {showAuth&&<AuthModal onClose={()=>setShowAuth(false)} onSuccess={u=>{setMe(u);setShowAuth(false);notify('Đăng nhập thành công')}} notify={notify} request={request}/>}
    {showPost&&<PostModal type={showPost} catalog={catalog} onClose={()=>setShowPost(null)} notify={notify} request={request} onCreated={async()=>{setShowPost(null);await load()}}/>}
    {selected&&<DetailModal report={selected} me={me} onClose={()=>setSelected(null)} onAuth={()=>{setSelected(null);setShowAuth(true)}}
      onMarkReturned={()=>request(async()=>{
        if(!window.confirm('Xác nhận bạn đã nhận/trả lại vật phẩm?'))return;
        const result=await api(`/items/${selected.id}/status`,{method:'PATCH'});
        setSelected(prev=>prev?{...prev,status:result.status}:null);
        await load();notify('Bài đăng đã được đánh dấu Đã trả');
      })}/> }
    {notice&&<div className={'toast '+(notice.error?'error':'')}>{notice.message}<button onClick={()=>setNotice(null)}><X size={16}/></button></div>}
    {busy&&<span className="working">Đang xử lý...</span>}
  </div>
}

function ReportCard({report:r,onClick}){const cover=r.images?.[0];return <button className={'reportcard '+(r.status==='RETURNED'?'returned-card':'')} onClick={onClick}><div className={'cardphoto '+(isFound(r.type)?'aqua':'sand')}>{cover?<img src={cover} alt={r.itemName} loading="lazy"/>:<div className="itememoji">{r.category==='Điện tử'?'📱':r.category==='Giấy tờ'?'🪪':r.category==='Ví & túi'?'👛':r.category==='Chìa khóa'?'🔑':'🎒'}</div>}<span className={'reportbadge '+(r.status==='RETURNED'?'returned':(isFound(r.type)?'found':'lost'))}>{r.status==='RETURNED'?'ĐÃ TRẢ':(isFound(r.type)?'ĐÃ NHẶT ĐƯỢC':'ĐANG TÌM')}</span></div><div className="cardbody"><div className="cardmeta"><span>{r.category||'Vật dụng khác'}</span><span>·</span><span>{time(r.createdAt).split(',')[0]}</span></div><h3>{r.itemName}</h3><p>{r.description||'Chưa có mô tả bổ sung.'}</p><div className="cardbottom"><span><MapPin size={15}/> {r.location||r.locationDetail||'Chưa rõ khu vực'}</span><ArrowUpRight size={17}/></div></div></button>}

function Modal({children,onClose,wide}){useEffect(()=>{const f=e=>{if(e.key==='Escape')onClose()};document.addEventListener('keydown',f);document.body.style.overflow='hidden';return()=>{document.removeEventListener('keydown',f);document.body.style.overflow=''}},[onClose]);return <div className="overlay" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}><div className={'modal '+(wide?'wide':'')}><button className="modalclose" onClick={onClose}><X size={19}/></button>{children}</div></div>}
function AuthModal({onClose,onSuccess,notify,request}){
 const [mode,setMode]=useState('login'),[email,setEmail]=useState(''),[password,setPassword]=useState(''),[fullName,setFullName]=useState(''),[code,setCode]=useState('');
 const submit=e=>{e.preventDefault();request(async()=>{
   if(mode==='login'){const data=await api('/auth/login',{method:'POST',body:JSON.stringify({email,password})});onSuccess(data.user)}
   if(mode==='register'){const data=await api('/auth/register',{method:'POST',body:JSON.stringify({email,password,fullName})});setCode(data.devVerificationCode||'');setMode('verify');notify('Đã tạo tài khoản. Hãy xác minh email.');}
   if(mode==='verify'){await api('/auth/verify',{method:'POST',body:JSON.stringify({email,code})});notify('Xác minh thành công!');setMode('login');}
 })};
 return <Modal onClose={onClose}><div className="modalhead"><span className="modalsymbol"><ShieldCheck size={22}/></span><span className="kicker">CỘNG ĐỒNG FOUNDLY</span><h2>{mode==='login'?'Chào mừng trở lại':mode==='register'?'Tạo tài khoản mới':'Xác minh email'}</h2><p>{mode==='verify'?'Trong localhost, mã thử nghiệm được hiển thị bên dưới.':'Kết nối cùng cộng đồng trường học để tìm lại đồ.'}</p></div><form onSubmit={submit} className="form">
 {mode==='register'&&<label>Họ và tên<input required minLength={2} maxLength={120} value={fullName} onChange={e=>setFullName(e.target.value)} placeholder="Nguyễn Văn A"/></label>}
 <label>Email{mode==='login'?'':' của bạn'}<input required type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="sinhvien@campus.edu" readOnly={mode==='verify'}/></label>
 {mode==='verify'?<label>Mã xác minh<input value={code} onChange={e=>setCode(e.target.value)} required pattern="[0-9]{6}" maxLength={6}/><small>Chế độ Development: mã do API trả về khi đăng ký. Hệ thống chưa gửi email thật.</small></label>:<label>Mật khẩu<input type="password" value={password} onChange={e=>setPassword(e.target.value)} required minLength={mode==='register'?10:1} placeholder="••••••••••"/></label>}
 <button className="btn primary submit" type="submit">{mode==='login'?'Đăng nhập':mode==='register'?'Tạo tài khoản':'Xác minh'} <ArrowRight size={17}/></button>
 </form>{mode!=='verify'&&<p className="modalswitch">{mode==='login'?'Chưa có tài khoản?':'Đã có tài khoản?'} <button onClick={()=>setMode(mode==='login'?'register':'login')}>{mode==='login'?'Đăng ký ngay':'Đăng nhập'}</button></p>}
 <div className="demobox"><strong>Tài khoản mẫu để thử</strong><small>Người mất: minh@campus.edu / Demo12345!</small><small>Người nhặt: lan@campus.edu / Demo12345!</small></div></Modal>
}

function PostModal({type,catalog,onClose,notify,request,onCreated}){
 const initial=null;
 const [form,setForm]=useState({...emptyReport(),type}),[images,setImages]=useState([]);
 const update=(key,value)=>setForm(x=>({...x,[key]:value}));
 const submit=e=>{e.preventDefault();request(async()=>{
   const body={...form,categoryId:form.categoryId?Number(form.categoryId):null,locationId:form.locationId?Number(form.locationId):null,eventTime:new Date(form.eventTime).toISOString()};
   const result=await api('/items',{method:'POST',body:JSON.stringify(body)});
   const id=result.id;
   for(const file of images){const fd=new FormData();fd.append('file',file);await api(`/uploads/reports/${id}`,{method:'POST',body:fd})}
   notify('Đã đăng tin thành công.');await onCreated(id,form.type);
 })};
 return <Modal wide onClose={onClose}><div className="modalhead left"><span className="kicker">ĐĂNG THÔNG TIN ĐỒ THẤT LẠC</span><h2>{form.type==='LOST'?'Tôi đang tìm đồ':'Tôi đã nhặt được đồ'}</h2><p>Càng mô tả rõ, gợi ý tương đồng càng hữu ích. Tránh công khai các đặc điểm dùng để xác minh chủ sở hữu.</p></div>
 <form className="form postform" onSubmit={submit}><div className="twotabs"><button type="button" className={form.type==='LOST'?'picked':''} onClick={()=>update('type','LOST')}><PackageOpen size={16}/> Báo mất đồ</button><button type="button" className={form.type==='FOUND'?'picked':''} onClick={()=>update('type','FOUND')}><PackageCheck size={16}/> Báo nhặt được</button></div>
 <label className="full">Tên vật phẩm <i>*</i><input required maxLength={140} value={form.itemName} onChange={e=>update('itemName',e.target.value)} placeholder="Ví dụ: Ví da màu đen, điện thoại Samsung..."/></label>
 <label className="full">Mô tả<textarea rows={3} maxLength={3500} value={form.description} onChange={e=>update('description',e.target.value)} placeholder="Đặc điểm bên ngoài, vật dụng liên quan, nơi nhìn thấy gần nhất..."/></label>
 <label>Nhóm vật phẩm<select value={form.categoryId} onChange={e=>update('categoryId',e.target.value)}><option value="">Không rõ / Khác</option>{catalog.categories.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
 <label>Khu vực<select value={form.locationId} onChange={e=>update('locationId',e.target.value)}><option value="">Chưa rõ khu vực</option>{catalog.locations.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
 <label>Vị trí cụ thể<input value={form.locationDetail} maxLength={300} onChange={e=>update('locationDetail',e.target.value)} placeholder="Tầng 2, cạnh thang máy..."/></label>
 <label>Thời gian mất / nhặt <i>*</i><input required type="datetime-local" value={form.eventTime} onChange={e=>update('eventTime',e.target.value)} /></label>
 <label>Màu sắc<input maxLength={80} value={form.color} onChange={e=>update('color',e.target.value)} placeholder="Đen, xanh, trắng..."/></label><label>Thương hiệu<input maxLength={80} value={form.brand} onChange={e=>update('brand',e.target.value)} placeholder="Apple, Samsung..."/></label>
 {form.type==='FOUND'&&<label className="full">Đặc điểm xác minh bí mật <span className="optional">(chỉ người đăng được biết)</span><input maxLength={200} value={form.privateIdentifier} onChange={e=>update('privateIdentifier',e.target.value)} placeholder="Dấu hiệu chỉ chủ sở hữu biết, không công khai"/></label>}
 <label className="full uploadlabel"><Images size={20}/><strong>Ảnh vật phẩm (tối đa 4 ảnh)</strong><small>JPG / PNG / WebP · tối đa 5 MB mỗi ảnh</small><input type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={e=>setImages([...e.target.files].slice(0,4))}/>{images.length>0&&<span>{images.length} ảnh đã chọn</span>}</label>
 <div className="full formfoot"><p><LockKeyhole size={16}/> Không công khai email hoặc thông tin xác minh riêng tư.</p><button type="submit" className="btn primary lg"><Plus size={17}/> Đăng tin ngay</button></div>
 </form></Modal>
}

function DetailModal({report:r,me,onClose,onAuth,onMarkReturned}){
  return <Modal wide onClose={onClose}>
    <div className="detailgrid"><div className="detailvisual">{r.images?.length?<img src={r.images[0]} alt={r.itemName}/>:<div className="detailplaceholder">{isFound(r.type)?'📦':'🎒'}</div>}</div>
      <div className="detailbody"><span className={'status '+r.status.toLowerCase()}>{r.status==='RETURNED'?'ĐÃ TRẢ':r.status}</span><h2>{r.itemName}</h2>
      <p><MapPin size={16}/> {r.location||r.locationDetail||'Chưa rõ địa điểm'}</p>
      <p><CalendarDays size={16}/> {time(r.eventTime)}</p>
      <p><strong>Danh mục:</strong> {r.category||'Khác'}</p><p>{r.description||'Chưa có mô tả bổ sung.'}</p>
      {me?.id===r.userId&&r.status==='OPEN'&&<button className="btn primary" onClick={onMarkReturned}><PackageCheck size={17}/> Đánh dấu đã trả đồ</button>}
      {!me&&<button className="btn outline" onClick={onAuth}>Đăng nhập</button>}
    </div></div>
  </Modal>
}
createRoot(document.getElementById('root')).render(<><App/><ChatLogic/></>);