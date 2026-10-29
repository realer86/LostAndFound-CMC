import React,{useCallback,useEffect,useState} from 'react';
import {RefreshCw,Send} from 'lucide-react';
import './supportAdmin.css';

async function call(path,options={}){
 const r=await fetch('/api'+path,{credentials:'same-origin',...options,headers:{'Content-Type':'application/json',...options.headers}});
 const json=await r.json().catch(()=>({}));if(!r.ok)throw new Error(json.message||`HTTP ${r.status}`);return json;
}
const labels={ADMIN:'Admin - phản ánh',SECURITY:'Bảo vệ',LIBRARY:'Thư viện',DORM:'Ký túc xá'};
const when=v=>new Date(v).toLocaleString('vi-VN');
export default function SupportAdminInbox({user}){
 const [filter,setFilter]=useState(user?.role==='STAFF'?(user.departmentCode||''):''),[threads,setThreads]=useState([]),[id,setId]=useState(null),[messages,setMessages]=useState([]),[draft,setDraft]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false);
 const load=useCallback(async()=>{try{setError('');const q=filter?'?department='+encodeURIComponent(filter):'';const r=await call('/admin/support/threads'+q);setThreads(r.items||[]);setId(old=>r.items?.some(x=>x.id===old)?old:(r.items?.[0]?.id||null))}catch(e){setError(e.message)}},[filter]);
 useEffect(()=>{load()},[load]);
 const history=useCallback(async()=>{if(!id){setMessages([]);return;}try{const r=await call(`/admin/support/threads/${id}/messages`);setMessages(r.items||[])}catch(e){setError(e.message)}},[id]);
 useEffect(()=>{history()},[history]);
 async function send(e){e.preventDefault();if(!id||!draft.trim()||busy)return;setBusy(true);try{await call(`/admin/support/threads/${id}/messages`,{method:'POST',body:JSON.stringify({content:draft.trim()})});setDraft('');await history();await load()}catch(e){setError(e.message)}finally{setBusy(false)}}
 const active=threads.find(t=>t.id===id);
 return <section className="office-inbox"><div className="office-toolbar"><label>Bộ phận <select value={filter} onChange={e=>setFilter(e.target.value)} disabled={user?.role==='STAFF'}><option value="">Tất cả</option>{Object.entries(labels).map(([key,label])=><option key={key} value={key}>{label}</option>)}</select></label><button onClick={()=>{load();history()}}><RefreshCw size={16}/> Làm mới</button></div>
 {error&&<p role="alert" className="office-error">{error}</p>}
 <div className="office-grid"><aside>{threads.map(t=><button key={t.id} className={t.id===id?'active':''} onClick={()=>setId(t.id)}><strong>{t.title} · {labels[t.department]}</strong><small>{t.userName} · {when(t.lastMessageAt)}</small></button>)}{!threads.length&&<p>Chưa có yêu cầu trong mục này.</p>}</aside>
 <div className="office-panel"><header><h3>{active?labels[active.department]+' · '+active.userName:'Chọn hồ sơ hỗ trợ'}</h3><p>Tin nhắn chỉ hiển thị cho người yêu cầu và phía xử lý được phân quyền. Admin có thể điều phối các quầy.</p></header><div className="office-messages">{messages.map(m=><div key={m.id} className={'office-bubble '+(m.mine?'user':'admin')}><span>{m.content}</span><small>{m.mine?'Người gửi yêu cầu':m.senderType==='STAFF'?'Nhân viên phòng ban':'Admin'} · {when(m.createdAt)}</small></div>)}{!messages.length&&<p>Chưa có nội dung trao đổi.</p>}</div>
 {active&&<form onSubmit={send}><input value={draft} maxLength={1500} onChange={e=>setDraft(e.target.value)} placeholder="Phản hồi người gửi..." aria-label="Tin nhắn hỗ trợ"/><button disabled={busy||!draft.trim()}><Send size={16}/> {busy?'Đang gửi':'Gửi phản hồi'}</button></form>}</div></div></section>
}
