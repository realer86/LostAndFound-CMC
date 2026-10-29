import React, {useCallback, useEffect, useState} from 'react';
import {Building2, Clock3, MapPin, MessageCircle, RefreshCw, Send, ShieldCheck} from 'lucide-react';
import './supportInbox.css';

async function request(path,opts={}) {
  const response=await fetch('/api'+path,{credentials:'same-origin',...opts,headers:{'Content-Type':'application/json',...opts.headers}});
  const data=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(data.message||`Lỗi HTTP ${response.status}`);
  return data;
}
const when=x=>new Date(x).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',hour:'2-digit',minute:'2-digit'});
const dept={ADMIN:'Quản trị viên',SECURITY:'Bộ phận bảo vệ',LIBRARY:'Thư viện',DORM:'Quầy ký túc xá'};

// Support conversations are ONLY created by submitting a case or custody request.
// No arbitrary peer-to-peer room creation is exposed in this screen.
export default function SupportInbox({user}) {
  const [tab,setTab]=useState('inbox'),[threads,setThreads]=useState([]),[directory,setDirectory]=useState([]);
  const [active,setActive]=useState(null),[messages,setMessages]=useState([]),[draft,setDraft]=useState('');
  const [loading,setLoading]=useState(false),[sending,setSending]=useState(false),[error,setError]=useState('');
  const refresh=useCallback(async()=>{
    setLoading(true);setError('');
    try{
      const calls=[request('/directory')];if(user?.id)calls.push(request('/support/threads'));
      const results=await Promise.all(calls);
      setDirectory(results[0].items||[]);
      const rows=results[1]?.items||[];
      setThreads(rows);
      setActive(id=>rows.some(x=>x.id===id)?id:(rows[0]?.id||null));
    }catch(e){setError(e.message)}finally{setLoading(false)}
  },[user?.id]);
  useEffect(()=>{refresh()},[refresh]);
  const getMessages=useCallback(async()=>{
    if(!active){setMessages([]);return;}
    try{const result=await request(`/support/threads/${active}/messages`);setMessages(result.items||[])}
    catch(e){setError(e.message)}
  },[active]);
  useEffect(()=>{getMessages()},[getMessages]);
  const thread=threads.find(t=>t.id===active);
  const send=async e=>{
    e.preventDefault();if(!thread||!draft.trim()||sending)return;
    setSending(true);setError('');
    try{await request(`/support/threads/${active}/messages`,{method:'POST',body:JSON.stringify({content:draft.trim()})});setDraft('');await getMessages();await refresh()}
    catch(e){setError(e.message)}finally{setSending(false)}
  };
  return <div className="support-screen">
    <div className="support-heading"><span className="kicker">FOUNDLY · KÊNH HỖ TRỢ CHÍNH THỨC</span><h1>Hỗ trợ & Danh bạ</h1>
      <p>Trao đổi với bộ phận quản trị khi báo cáo sự cố, hoặc theo dõi hỗ trợ của quầy ký gửi. Chat giữa người dùng chỉ có trong Phòng bàn giao sau khi Claim được duyệt.</p></div>
    <div className="support-notice"><ShieldCheck size={19}/><span>Hội thoại hỗ trợ gắn với hồ sơ báo cáo hoặc ký gửi; không công khai email hay số điện thoại cá nhân. Yêu cầu ký gửi được gửi tới quầy được chọn; nhân viên đã được phân quyền hoặc Admin có thể trả lời.</span></div>
    <div className="support-tabs"><button className={tab==='inbox'?'active':''} onClick={()=>setTab('inbox')}><MessageCircle size={17}/> Hộp thư hỗ trợ</button>
      <button className={tab==='directory'?'active':''} onClick={()=>setTab('directory')}><Building2 size={17}/> Danh bạ phòng ban</button><button onClick={refresh} disabled={loading}><RefreshCw size={16}/> Làm mới</button></div>
    {error&&<p role="alert" className="support-error">{error}</p>}
    {tab==='inbox' ? !user ? <p className="support-empty">Đăng nhập bằng email trường để xem các yêu cầu hỗ trợ của bạn.</p> :
      <div className="support-chat-layout"><aside className="support-threads">
        {loading&&<p>Đang tải...</p>}
        {threads.map(t=><button key={t.id} className={active===t.id?'selected':''} onClick={()=>setActive(t.id)}>
          <strong>{t.title}</strong><small>{dept[t.department]||'Hỗ trợ'} · {when(t.lastMessageAt)}</small></button>)}
        {!loading&&!threads.length&&<p>Chưa có hồ sơ hỗ trợ. Hãy gửi phản ánh trong Phòng bàn giao hoặc tạo yêu cầu ký gửi để bắt đầu.</p>}
      </aside><section className="support-conversation">
        <header><h2>{thread?.title||'Chọn một yêu cầu'}</h2><p>{thread?.routingNote||'Chỉ các hồ sơ hợp lệ mới có quyền trao đổi.'}</p></header>
        <div className="support-messages">{messages.map(m=><article key={m.id} className={'support-bubble '+(m.mine?'mine':'')}>
          <span>{m.content}</span><small>{m.mine?'Bạn':m.senderType==='STAFF'?'Nhân viên phòng ban':'Quản trị viên'} · {when(m.createdAt)}</small></article>)}
          {!messages.length&&<p className="support-empty">{thread?'Chưa có tin nhắn. Bạn có thể hỏi về tiến độ hồ sơ.':'Chọn một hồ sơ ở bên trái.'}</p>}</div>
        {thread&&<form className="support-send" onSubmit={send}><input maxLength={1500} value={draft} onChange={e=>setDraft(e.target.value)} placeholder="Nhập câu hỏi về hồ sơ..." aria-label="Nội dung gửi hỗ trợ" disabled={sending}/>
          <button type="submit" disabled={sending||!draft.trim()}><Send size={16}/>{sending?'Đang gửi':'Gửi'}</button></form>}
        {thread&&<button className="support-refresh-messages" onClick={getMessages}>Làm mới tin nhắn</button>}
      </section></div> : <div className="support-directory">{directory.map(d=><article key={d.id} className="support-office"><Building2 size={22}/><h3>{d.name}</h3>
        <p><MapPin size={15}/>{d.location}</p><p><Clock3 size={15}/>{d.officeHours}</p>
        <small>Máy nội bộ: {d.internalPhone||'Chưa có'}</small>
        <p className="support-directory-hint">Để nhắn về ký gửi, hãy gửi yêu cầu trong mục Hỗ trợ & ký gửi rồi mở Hộp thư hỗ trợ.</p></article>)}
        {!loading&&!directory.length&&<p>Chưa có dữ liệu danh bạ được xác nhận.</p>}</div>}
  </div>;
}
