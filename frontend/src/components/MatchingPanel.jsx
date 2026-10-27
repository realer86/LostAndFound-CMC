import React from 'react';
import { ArrowRight, ArrowUpRight, LoaderCircle, RefreshCcw, Search, Sparkles } from 'lucide-react';
import './matching.css';

const names = { LOST: 'BÁO MẤT', FOUND: 'NHẶT ĐƯỢC' };

export default function MatchingPanel({ mine, selectedId, matches, loading, onSelect, onOpen, onExplore }) {
  const eligible = mine.filter(report => report.status === 'OPEN');
  const selected = mine.find(report => report.id === selectedId);
  const type = selected?.type === 'LOST' ? 'NHẶT ĐƯỢC' : 'BÁO MẤT';
  return <section className="container privatepage matching-page">
    <div className="matching-heading">
      <span className="kicker">MATCHING ENGINE</span>
      <h1><Sparkles size={30}/> Gợi ý phù hợp</h1>
      <p>So khớp các bài mất và nhặt đồ theo tên, danh mục, địa điểm, thời gian, màu sắc và thương hiệu. Điểm tương đồng không phải xác suất tìm đúng đồ hay bằng chứng sở hữu.</p>
      <div className="matching-legend"><span className="match-level high">80–100 điểm · Rất phù hợp</span><span className="match-level possible">50–79 điểm · Có thể liên quan</span></div>
    </div>
    <div className="matchinglayout">
      <aside className="reportselector">
        <h3>Bài đăng của bạn</h3>
        <p>Chọn một bài đang mở để tìm các bài đối lập.</p>
        {eligible.map(report => <button key={report.id} type="button" className={selectedId === report.id ? 'chosen' : ''} onClick={() => onSelect(report.id)}>
          <span className={'tinybadge ' + (report.type === 'FOUND' ? 'found' : 'lost')}>{names[report.type]}</span>
          <span>{report.itemName}</span><ArrowRight size={15}/>
        </button>)}
        {!eligible.length && <div className="matching-nodata">Bạn chưa có bài đăng đang mở. Hãy đăng tin để nhận gợi ý.</div>}
      </aside>
      <div className="matchlist" aria-live="polite">
        <div className="matching-results-heading"><div><h3>{selected ? `Các bài ${type} có thể liên quan` : 'Chọn bài đăng để bắt đầu'}</h3><p>{selected ? `${matches.length} kết quả từ 50 điểm trở lên` : 'Mỗi bài được so khớp với loại bài đăng ngược lại.'}</p></div>
          {selected && <button type="button" className="btn outline" onClick={() => onSelect(selectedId)} disabled={loading}><RefreshCcw size={16}/> Cập nhật</button>}
        </div>
        {loading && <div className="matching-loading"><LoaderCircle className="spin" size={24}/> Đang tìm gợi ý...</div>}
        {!loading && selected && matches.map(match => <div key={match.id} className="matchrow matching-result">
          <div className={'matchscore ' + (match.level === 'HIGH' ? 'matching-high' : 'matching-possible')}><strong>{match.score}</strong><small>/100 điểm</small></div>
          <div className="matchdetails"><span className={'match-level ' + (match.level === 'HIGH' ? 'high' : 'possible')}>{match.level === 'HIGH' ? 'Rất phù hợp' : 'Có thể liên quan'}</span>
            <h4>{match.report.itemName}</h4>
            <p>{match.report.location || match.report.locationDetail || 'Chưa rõ địa điểm'} · {new Date(match.report.eventTime).toLocaleDateString('vi-VN')}</p>
            <div className="matching-reasons">{match.reasons.map(reason => <span key={reason}>{reason}</span>)}</div>
            <small>Độ đầy đủ thông tin: {Math.round(match.coverage * 100)}%</small>
          </div>
          <button type="button" className="btn outline" onClick={() => onOpen(match.report)}>Xem bài <ArrowUpRight size={16}/></button>
        </div>)}
        {!loading && selected && matches.length === 0 && <div className="matching-empty"><Search size={30}/><h3>Chưa có gợi ý từ 50 điểm</h3><p>Có thể chưa ai đăng bài tương ứng, hoặc thông tin chưa đủ để đối chiếu. Bạn vẫn có thể tìm kiếm thủ công.</p><button className="btn outline" type="button" onClick={() => onExplore(selected.type)}><Search size={16}/> Xem các bài {type.toLowerCase()}</button></div>}
      </div>
    </div>
  </section>;
}
