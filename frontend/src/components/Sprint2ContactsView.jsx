import React, { useState } from 'react';
import { ArrowLeft, BookUser, Building2, Clock3, MapPin, MessageCircle, Search, Send, ShieldCheck, UserRound } from 'lucide-react';
import './sprint2Contacts.css';

// Sprint 2 / Issue #14: visual-only demo. Issue #15 will replace mock data with API responses.
// Uses the private conversation concept and message bubble styling from Foundly Pro's handover UI.
const demoThreads = [
  { id: 'demo-01', title: 'Balo màu xanh', person: 'Người đăng tin', context: 'Đồ nhặt được · Thư viện',
    messages: [
      { id: 'sample-1', mine: false, text: 'Chào bạn, mình muốn hỏi thêm về chiếc balo.', at: '09:20' },
      { id: 'sample-2', mine: true, text: 'Bạn có thể cho mình biết khu vực đã tìm thấy không?', at: '09:21' },
    ] },
  { id: 'demo-02', title: 'Chìa khóa', person: 'Người đăng tin', context: 'Đồ thất lạc · Căn tin',
    messages: [{ id: 'sample-3', mine: false, text: 'Bạn có thể mô tả chùm chìa khóa không?', at: '10:04' }] }
];
const demoDirectory = [
  { id: 1, name: 'Phòng Bảo vệ (demo)', category: 'Bảo vệ', location: 'Sảnh tòa nhà A (demo)', internalPhone: '1001', officeHours: 'Theo ca trực' },
  { id: 2, name: 'Ban Quản lý (demo)', category: 'Quản lý', location: 'Tầng 1 (demo)', internalPhone: '1002', officeHours: 'Giờ hành chính' },
];

export default function Sprint2ContactsView({ initialReport = null, onClose = null }) {
  const [section, setSection] = useState('chats');
  const [query, setQuery] = useState('');
  const [activeId, setActiveId] = useState(initialReport ? `report-${initialReport.id}` : demoThreads[0].id);
  const [messagesByThread, setMessagesByThread] = useState({});
  const [draft, setDraft] = useState('');
  const [feedback, setFeedback] = useState('');
  const reportThread = initialReport ? {
    id: `report-${initialReport.id}`, title: initialReport.itemName,
    person: 'Người đăng tin', context: 'Bài đăng đang xem', messages: []
  } : null;
  const threads = reportThread ? [reportThread, ...demoThreads] : demoThreads;
  const active = threads.find(t => t.id === activeId) || threads[0];
  const messages = [...active.messages, ...(messagesByThread[active.id] || [])];
  const matches = (value) => value.toLocaleLowerCase('vi-VN').includes(query.trim().toLocaleLowerCase('vi-VN'));
  const visibleThreads = threads.filter(t => matches(`${t.title} ${t.person} ${t.context}`));
  const visibleDirectory = demoDirectory.filter(d => matches(`${d.name} ${d.category} ${d.location}`));

  function sendMessage(event) {
    event.preventDefault();
    const text = draft.trim();
    if (!text) return;
    if (text.length > 1500) { setFeedback('Tin nhắn tối đa 1500 ký tự.'); return; }
    setMessagesByThread(previous => ({
      ...previous,
      [active.id]: [...(previous[active.id] || []), {
        id: `local-${Date.now()}-${(previous[active.id] || []).length}`,
        mine: true, text,
        at: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
      }],
    }));
    setDraft('');
    setFeedback('Tin nhắn chỉ được hiển thị trong bản xem trước, chưa gửi tới người đăng.');
  }

  return <div className="s2-screen">
    <div className="s2-heading">
      <div>
        <span className="kicker">FOUNDLY · SPRINT 2</span>
        <h1>Liên hệ & Danh bạ</h1>
        <p>Trao đổi về đồ thất lạc và tìm thông tin liên hệ các bộ phận hỗ trợ trong trường.</p>
      </div>
      {onClose && <button type="button" className="s2-back" onClick={onClose}><ArrowLeft size={17}/> Quay lại bài đăng</button>}
    </div>
    <div role="status" className="s2-demo-banner"><ShieldCheck size={18}/><span><strong>Giao diện demo Issue #14.</strong> Tin nhắn chỉ lưu tạm trên màn hình này. Issue #15 sẽ kết nối API và dữ liệu thật; danh bạ hiện là dữ liệu minh họa, không phải số liên hệ chính thức.</span></div>
    <div className="s2-tabs" role="tablist" aria-label="Loại nội dung liên hệ">
      <button type="button" role="tab" aria-selected={section === 'chats'} className={section === 'chats' ? 'selected' : ''} onClick={() => { setSection('chats'); setQuery(''); }}><MessageCircle size={18}/> Hộp thư</button>
      <button type="button" role="tab" aria-selected={section === 'directory'} className={section === 'directory' ? 'selected' : ''} onClick={() => { setSection('directory'); setQuery(''); }}><BookUser size={18}/> Danh bạ</button>
    </div>
    {section === 'chats' ? <div className="s2-chat-layout">
      <aside className="s2-thread-list" aria-label="Danh sách hội thoại mẫu">
        <label className="s2-search"><Search size={17}/><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Tìm cuộc trò chuyện..." aria-label="Tìm cuộc trò chuyện" /></label>
        <span className="s2-subheading">TIN NHẮN MẪU · {visibleThreads.length}</span>
        {visibleThreads.map(t => <button type="button" key={t.id} className={`s2-thread ${active.id === t.id ? 'active' : ''}`} onClick={() => { setActiveId(t.id); setDraft(''); setFeedback(''); }}>
          <span className="s2-avatar"><UserRound size={20}/></span>
          <span className="s2-thread-info"><strong>{t.title}</strong><small>{t.person}</small><span>{t.context}</span></span>
        </button>)}
        {!visibleThreads.length && <p className="s2-empty-side">Không tìm thấy cuộc trò chuyện.</p>}
      </aside>
      <section className="s2-conversation" aria-label="Khung trò chuyện">
        <div className="s2-chat-header"><span className="s2-avatar"><UserRound size={22}/></span><div><h2>{active.person}</h2><p>Về bài đăng: {active.title}</p></div><span className="s2-private"><ShieldCheck size={14}/> Riêng tư</span></div>
        <div className="s2-chat-note"><ShieldCheck size={15}/> Không chia sẻ thông tin xác minh bí mật hoặc số điện thoại cá nhân.</div>
        <div className="s2-chat-messages" aria-live="polite">
          {!messages.length && <div className="s2-chat-empty"><MessageCircle size={26}/><strong>Chưa có tin nhắn mẫu</strong><span>Viết một tin nhắn để xem trước giao diện gửi tin.</span></div>}
          {messages.map(m => <div key={m.id} className={`s2-bubble ${m.mine ? 'mine' : 'theirs'}`}><span>{m.text}</span><small>{m.mine ? 'Bạn' : active.person} · {m.at}</small></div>)}
        </div>
        <form className="s2-composer" onSubmit={sendMessage}>
          <label htmlFor="s2-draft" className="sr-only">Nội dung tin nhắn demo</label>
          <input id="s2-draft" maxLength={1500} value={draft} onChange={e => { setDraft(e.target.value); if (feedback) setFeedback(''); }} placeholder="Nhập tin nhắn xem trước..." />
          <button type="submit" disabled={!draft.trim()} aria-label="Gửi tin nhắn xem trước"><Send size={17}/><span>Gửi thử</span></button>
        </form>
        <div className="s2-composer-foot"><span>{feedback || 'Bản demo không gửi tin nhắn đến người dùng thật.'}</span><span>{draft.length}/1500</span></div>
      </section>
    </div> : <section className="s2-directory" aria-label="Danh bạ các phòng ban">
      <div className="s2-directory-head"><div><h2>Danh bạ hỗ trợ</h2><p>Tra cứu vị trí, số máy nội bộ và thời gian làm việc của các đơn vị.</p></div><label className="s2-search"><Search size={17}/><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Tìm phòng ban..." aria-label="Tìm phòng ban" /></label></div>
      <div className="s2-directory-grid">{visibleDirectory.map(d => <article key={d.id} className="s2-directory-card">
        <div className="s2-dir-top"><span className="s2-dir-icon"><Building2 size={22}/></span><span className="s2-tag">{d.category}</span></div>
        <h3>{d.name}</h3>
        <p><MapPin size={16}/>{d.location}</p>
        <p><Clock3 size={16}/>{d.officeHours}</p>
        <div className="s2-directory-bottom">Máy nội bộ (demo): <strong>{d.internalPhone}</strong></div>
      </article>)}
      {!visibleDirectory.length && <p className="s2-empty-side">Không tìm thấy phòng ban.</p>}</div>
    </section>}
  </div>;
}
