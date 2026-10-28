import React, { useState } from 'react';
import { GoogleOAuthProvider, GoogleLogin } from '@react-oauth/google';

const GOOGLE_CLIENT_ID = "263595904082-d6nu20kmorf7amnff0pcfiqqc5mec1jg.apps.googleusercontent.com";

export function AuthModal({ isOpen, onClose, onLoginSuccess }) {
  const [tab, setTab] = useState('login'); // 'login' | 'register'
  const [step, setStep] = useState(1);     // Bước 1: Nhập thông tin, Bước 2: Nhập OTP
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [password, setPassword] = useState('');
  const [otp, setOtp] = useState('');
  const [statusMsg, setStatusMsg] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  // Xử lý gửi mã OTP
  const handleSendOtp = async (e) => {
    e.preventDefault();
    setLoading(true);
    setStatusMsg('');
    try {
      const res = await fetch('/api/auth/send-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Lỗi gửi OTP');
      setStep(2);
      setStatusMsg(data.message);
    } catch (err) {
      setStatusMsg(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Xử lý xác thực OTP và hoàn tất đăng ký
  const handleVerifyOtp = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await fetch('/api/auth/verify-register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, fullName, password, otp })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Xác thực thất bại');
      onLoginSuccess(data.user);
      onClose();
    } catch (err) {
      setStatusMsg(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Xử lý khi bấm Đăng nhập bằng Google
  const handleGoogleSuccess = async (credentialResponse) => {
    try {
      const res = await fetch('/api/auth/google-login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ credential: credentialResponse.credential })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message);
      onLoginSuccess(data.user);
      onClose();
    } catch (err) {
      setStatusMsg(err.message);
    }
  };

  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <div className="modal-backdrop" style={{
        position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
      }}>
        <div className="modal-card" style={{
          background: '#fff', padding: '24px', borderRadius: '16px', width: '400px', maxWidth: '90%'
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '16px' }}>
            <h3 style={{ margin: 0 }}>{tab === 'login' ? 'Đăng nhập Foundly' : 'Đăng ký Xác thực CMC'}</h3>
            <button onClick={onClose} style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: '18px' }}>✕</button>
          </div>

          {/* Chuyển đổi qua lại giữa Đăng nhập và Đăng ký */}
          <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
            <button onClick={() => { setTab('login'); setStatusMsg(''); }}
              style={{ flex: 1, padding: '8px', borderRadius: '8px', border: '1px solid #0d9488', background: tab === 'login' ? '#0d9488' : '#fff', color: tab === 'login' ? '#fff' : '#0d9488' }}>
              Đăng nhập
            </button>
            <button onClick={() => { setTab('register'); setStep(1); setStatusMsg(''); }}
              style={{ flex: 1, padding: '8px', borderRadius: '8px', border: '1px solid #0d9488', background: tab === 'register' ? '#0d9488' : '#fff', color: tab === 'register' ? '#fff' : '#0d9488' }}>
              Đăng ký (OTP)
            </button>
          </div>

          {statusMsg && (
            <div style={{ padding: '10px', background: '#fef2f2', color: '#dc2626', borderRadius: '8px', marginBottom: '12px', fontSize: '13px' }}>
              {statusMsg}
            </div>
          )}

          {/* LUỒNG ĐĂNG KÝ KÈM OTP */}
          {tab === 'register' && step === 1 && (
            <form onSubmit={handleSendOtp} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <input type="text" placeholder="Họ và tên sinh viên / giảng viên" required value={fullName} onChange={e => setFullName(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }} />
              <input type="email" placeholder="Email trường (@student.cmc.edu.vn / @cmc.edu.vn)" required value={email} onChange={e => setEmail(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }} />
              <input type="password" placeholder="Mật khẩu" required value={password} onChange={e => setPassword(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }} />
              <button type="submit" disabled={loading} style={{ padding: '10px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer' }}>
                {loading ? 'Đang gửi mã OTP...' : 'Gửi mã xác minh OTP'}
              </button>
            </form>
          )}

          {tab === 'register' && step === 2 && (
            <form onSubmit={handleVerifyOtp} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <input type="text" placeholder="Nhập mã OTP 6 số trong Email" required value={otp} onChange={e => setOtp(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #0d9488', textAlign: 'center', letterSpacing: '4px', fontSize: '16px' }} />
              <button type="submit" disabled={loading} style={{ padding: '10px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer' }}>
                {loading ? 'Đang kiểm tra...' : 'Xác nhận & Tạo tài khoản'}
              </button>
            </form>
          )}

          {/* LUỒNG ĐĂNG NHẬP */}
          {tab === 'login' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <input type="email" placeholder="Email trường của bạn" value={email} onChange={e => setEmail(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }} />
              <input type="password" placeholder="Mật khẩu" value={password} onChange={e => setPassword(e.target.value)} style={{ padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }} />
              <button style={{ padding: '10px', background: '#0d9488', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer' }}>
                Đăng nhập
              </button>
            </div>
          )}

          <hr style={{ margin: '18px 0', border: 'none', borderTop: '1px solid #eee' }} />

          {/* NÚT ĐĂNG NHẬP BẰNG GOOGLE */}
          <div style={{ display: 'flex', justifyContent: 'center' }}>
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={() => setStatusMsg('Đăng nhập Google thất bại!')}
            />
          </div>
        </div>
      </div>
    </GoogleOAuthProvider>
  );
}