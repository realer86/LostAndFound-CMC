import React, { useEffect, useState } from 'react';
import { ArrowLeft, BookUser, Building2, Clock3, MapPin, MessageCircle, Search, Send, ShieldCheck, UserRound, RefreshCw } from 'lucide-react';
import './sprint2Contacts.css';

// Issue #14 remains usable as an isolated visual demo.
// Issue #15 supplies an optional `live` adapter; this view never fetches directly.
const demoThreads = [
  { id: 'demo-01', title: 'Balo màu xanh', person: 'Người đăng tin', context: 'Đồ nhặt được · Thư viện',
    messages: [
      { id: 'sample-1', mine: false, text: 'Chào bạn, mình muốn hỏi thêm về chiếc balo.', at: '09:20' },
      { id: 'sample-2', mine: true, text: 'Bạn có thể cho mình biết khu vực đã tìm thấy không?', at: '09:21' },
    ] },
  { id: 'demo-02', title: 'Chìa khóa', person: 'Người đăng tin', context: 'Đồ thất lạc · Căn tin',
    messages: [{ id: 'sample-3', mine: false, text: 'Bạn có thể mô tả chùm chìa khóa không?', at: '10:04' }] },
];
const demoDirectory = [
  { id: 1, name: 'Phòng Bảo vệ (demo)', category: 'Bảo vệ', location: 'Sảnh tòa nhà A (demo)', internalPhone: '1001', officeHours: 'Theo ca trực' },
  { id: 2, name: 'Ban Quản lý (demo)', category: 'Quản lý', location: 'Tầng 1 (demo)', internalPhone: '1002', officeHours: 'Giờ hành chính' },
];

const clock = value => value ? new Date(value).toLocaleTimeString('vi-VN', {
  hour: '2-digit', minute: '2-digit',
}) : '';
const includesQuery = (value, query) => String(value ?? '').toLocaleLowerCase('vi-VN')
  .includes(query.trim().toLocaleLowerCase('vi-VN'));

export default function Sprint2ContactsView({ initialReport = null, onClose = null, live = null }) {
  const [section, setSection] = useState('chats');
  const [query, setQuery] = useState('');
  const [demoActiveId, setDemoActiveId] = useState(initialReport ? `report-${initialReport.id}` : demoThreads[0].id);
  const [messagesByThread, setMessagesByThread] = useState({});
  const [draft, setDraft] = useState('');
  const [feedback, setFeedback] = useState('');
  const reportThread = initialReport ? {
    id: `report-${initialReport.id}`, title: initialReport.itemName,
    person: 'Người đăng tin', context: 'Bài đăng đang xem', messages: [],
  } : null;
  const demoItems = reportThread ? [reportThread, ...demoThreads] : demoThreads;
  const threads = live ? live.contacts.map(c => ({
    id: c.id, title: c.reportTitle, person: c.otherDisplayName,
    context: 'Hội thoại riêng tư', messages: [],
  })) : demoItems;
  const activeId = live ? live.activeId : demoActiveId;
  const active = threads.find(t => t.id === activeId) || null;
  const messages = live ? live.messages.map(m => ({
    id: m.id, mine: m.senderId === live.user?.id, text: m.content,
    at: clock(m.createdAt),
  })) : active ? [...active.messages, ...(messagesByThread[active.id] || [])] : [];
  const directory = live ? live.directory : demoDirectory;
  const visibleThreads = threads.filter(t => includesQuery(`${t.title} ${t.person} ${t.context}`, query));
  const visibleDirectory = directory.filter(d => includesQuery(`${d.name} ${d.category} ${d.location}`, query));
  const messageBusy = Boolean(live?.loadingMessages || live?.sending);
  const error = live?.messageError || feedback;

  useEffect(() => { setDraft(''); setFeedback(''); }, [activeId]);

  async function sendMessage(event) {
    event.preventDefault();
    const text = draft.trim();
    if (!text || !active) return;
    if (text.length > 1500) { setFeedback('Tin nhắn tối đa 1500 ký tự.'); return; }
    if (live) {
      const sent = await live.sendMessage(text);
      if (sent) { setDraft(''); setFeedback('Đã gửi tin nhắn.'); }
      return;
    }
    setMessagesByThread(previous => ({
      ...previous,
      [active.id]: [...(previous[active.id] || []), {
        id: `local-${Date.now()}-${(previous[active.id] || []).length}`,
        mine: true, text, at: clock(new Date()),
      }],
    }));
    setDraft('');
    setFeedback('Tin nhắn chỉ được hiển thị trong bản xem trước, chưa gửi tới người đăng.');
  }

  return <div className="s2-screen">
    <div className="s2-heading">
      <div><span className="kicker">FOUNDLY · SPRINT 2</span><h1>Liên hệ & Danh bạ</h1>
        <p>Trao đổi về đồ thất lạc và tìm thông tin liên hệ các bộ phận hỗ trợ trong trường.</p></div>
      {onClose && <button type="button" className="s2-back" onClick={onClose}><ArrowLeft size={17}/> Quay lại bài đăng</button>}
    </div>
    {live ? <div role="status" className="s2-demo-banner"><ShieldCheck size={18}/><span>
      <strong>Hộp thư kết nối API.</strong> Tin nhắn được lưu trên hệ thống; chỉ hai người tham gia hội thoại mới xem được.
      Danh bạ là thông tin phòng ban từ máy chủ, có thể gồm dữ liệu demo khi chạy localhost.
    </span></div> : <div role="status" className="s2-demo-banner"><ShieldCheck size={18}/><span>
      <strong>Giao diện demo Issue #14.</strong> Tin nhắn chỉ lưu tạm trên màn hình này.
      Bản demo không gửi tin nhắn đến người dùng thật; danh bạ là dữ liệu minh họa.
    </span></div>}
    <div className="s2-tabs" role="tablist" aria-label="Loại nội dung liên hệ">
      <button type="button" role="tab" aria-selected={section === 'chats'} className={section === 'chats' ? 'selected' : ''} onClick={() => { setSection('chats'); setQuery(''); }}><MessageCircle size={18}/> Hộp thư</button>
      <button type="button" role="tab" aria-selected={section === 'directory'} className={section === 'directory' ? 'selected' : ''} onClick={() => { setSection('directory'); setQuery(''); }}><BookUser size={18}/> Danh bạ</button>
    </div>
    {section === 'chats' ? <div className="s2-chat-layout">
      <aside className="s2-thread-list" aria-label="Danh sách hội thoại">
        <label className="s2-search"><Search size={17}/><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Tìm cuộc trò chuyện..." aria-label="Tìm cuộc trò chuyện"/></label>
        <div className="s2-list-heading"><span className="s2-subheading">{live ? 'HỘI THOẠI' : 'TIN NHẮN MẪU'} · {visibleThreads.length}</span>
          {live?.user && <button type="button" className="s2-refresh" onClick={live.refreshContacts} aria-label="Làm mới danh sách hội thoại" title="Làm mới danh sách hội thoại"><RefreshCw size={16}/></button>}</div>
        {live?.loadingContacts && <p role="status" className="s2-empty-side">Đang tải hội thoại...</p>}
        {live?.contactError && <p role="alert" className="s2-error">{live.contactError}</p>}
        {visibleThreads.map(t => <button type="button" key={t.id} className={`s2-thread ${active?.id === t.id ? 'active' : ''}`}
          onClick={() => { if (live) live.setActiveId(t.id); else setDemoActiveId(t.id); setDraft(''); setFeedback(''); }}>
          <span className="s2-avatar"><UserRound size={20}/></span>
          <span className="s2-thread-info"><strong>{t.title}</strong><small>{t.person}</small><span>{t.context}</span></span>
        </button>)}
        {!visibleThreads.length && !live?.loadingContacts && <p className="s2-empty-side">
          {live && !live.user ? 'Đăng nhập để xem các cuộc trò chuyện.' : 'Chưa có cuộc trò chuyện phù hợp.'}
        </p>}
      </aside>
      <section className="s2-conversation" aria-label="Khung trò chuyện">
        <div className="s2-chat-header"><span className="s2-avatar"><UserRound size={22}/></span>
          <div><h2>{active?.person || 'Hộp thư'}</h2><p>{active ? `Về bài đăng: ${active.title}` : 'Chọn một cuộc trò chuyện để bắt đầu.'}</p></div>
          <span className="s2-private"><ShieldCheck size={14}/> Riêng tư</span>
        </div>
        <div className="s2-chat-note"><ShieldCheck size={15}/> Không chia sẻ thông tin xác minh bí mật hoặc số điện thoại cá nhân.</div>
        <div className="s2-chat-messages" aria-live="polite">
          {live?.loadingMessages && active && <p role="status" className="s2-chat-empty">Đang tải tin nhắn...</p>}
          {!live?.loadingMessages && !messages.length && <div className="s2-chat-empty"><MessageCircle size={26}/>
            <strong>{active ? 'Chưa có tin nhắn' : 'Chưa chọn hội thoại'}</strong>
            <span>{!active ? 'Hãy mở một bài đăng và chọn “Liên hệ người đăng” hoặc chọn hội thoại có sẵn.' :
              live ? 'Gửi lời chào để bắt đầu trao đổi.' : 'Viết một tin nhắn để xem trước giao diện gửi tin.'}</span>
          </div>}
          {!live?.loadingMessages && messages.map(m => <div key={m.id} className={`s2-bubble ${m.mine ? 'mine' : 'theirs'}`}>
            <span>{m.text}</span><small>{m.mine ? 'Bạn' : active?.person} · {m.at}</small>
          </div>)}
        </div>
        {live && active && <div className="s2-history-controls">
          {live.messageTotal > 50 && <span>Đang hiển thị 50 tin gần nhất trên tổng {live.messageTotal} tin.</span>}
          <button type="button" className="s2-refresh" onClick={live.refreshMessages}><RefreshCw size={14}/> Làm mới tin nhắn</button>
        </div>}
        {active && <form className="s2-composer" onSubmit={sendMessage}>
          <label htmlFor="s2-draft" className="sr-only">Nội dung tin nhắn</label>
          <input id="s2-draft" maxLength={1500} value={draft}
            onChange={e => { setDraft(e.target.value); if (feedback) setFeedback(''); }}
            placeholder={live ? 'Nhập tin nhắn...' : 'Nhập tin nhắn xem trước...'} disabled={messageBusy}/>
          <button type="submit" disabled={!draft.trim() || messageBusy} aria-label={live ? 'Gửi tin nhắn' : 'Gửi tin nhắn xem trước'}>
            <Send size={17}/><span>{live?.sending ? 'Đang gửi...' : live ? 'Gửi' : 'Gửi thử'}</span>
          </button>
        </form>}
        <div className="s2-composer-foot"><span role={error ? 'alert' : 'status'}>
          {error || (live ? 'Tin nhắn được gửi qua API và lưu vào cơ sở dữ liệu.' : 'Bản demo không gửi tin nhắn đến người dùng thật.')}
        </span><span>{draft.length}/1500</span></div>
      </section>
    </div> : <section className="s2-directory" aria-label="Danh bạ các phòng ban">
      <div className="s2-directory-head"><div><h2>Danh bạ hỗ trợ</h2><p>Tra cứu vị trí, số máy nội bộ và thời gian làm việc của các đơn vị.</p></div>
        <label className="s2-search"><Search size={17}/><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Tìm phòng ban..." aria-label="Tìm phòng ban"/></label>
      </div>
      {live?.loadingDirectory && <p role="status">Đang tải danh bạ...</p>}
      {live?.directoryError && <p role="alert" className="s2-error">{live.directoryError}</p>}
      <div className="s2-directory-grid">{visibleDirectory.map(d => <article key={d.id} className="s2-directory-card">
        <div className="s2-dir-top"><span className="s2-dir-icon"><Building2 size={22}/></span><span className="s2-tag">{d.category}</span></div>
        <h3>{d.name}</h3><p><MapPin size={16}/>{d.location}</p><p><Clock3 size={16}/>{d.officeHours}</p>
        <div className="s2-directory-bottom">Máy nội bộ: <strong>{d.internalPhone || 'Chưa có'}</strong></div>
      </article>)}
      {!visibleDirectory.length && !live?.loadingDirectory && <p className="s2-empty-side">Không tìm thấy phòng ban.</p>}</div>
    </section>}
  </div>;
}
