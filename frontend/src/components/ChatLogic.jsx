import React, { useState, useEffect } from 'react';

const ChatLogic = () => {
  const [contacts, setContacts] = useState([]);
  const [chatHistory, setChatHistory] = useState([]);
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  // 1. Fetch dữ liệu Danh bạ và Lịch sử chat khi vừa vào trang
  useEffect(() => {
    const fetchData = async () => {
      try {
        // Lưu ý: Đổi URL_BACKEND thành endpoint thực tế của Backend
        // Lấy danh bạ
        const resContacts = await fetch('http://localhost:5000/api/contacts');
        if (resContacts.ok) {
          const dataContacts = await resContacts.json();
          setContacts(dataContacts);
        }

        // Lấy lịch sử chat
        const resChats = await fetch('http://localhost:5000/api/chats');
        if (resChats.ok) {
          const dataChats = await resChats.json();
          setChatHistory(dataChats);
        }
      } catch (error) {
        console.error("Lỗi kết nối Backend:", error);
      }
    };

    fetchData();
  }, []);

  // 2. Bắt sự kiện nút "Gửi tin nhắn" và gọi API POST
  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!message.trim()) return; // Không cho gửi tin nhắn trống

    setIsLoading(true);
    
    // Gói dữ liệu tin nhắn gửi xuống DB
    const newMessagePayload = {
      content: message,
      timestamp: new Date().toISOString()
    };

    try {
      const response = await fetch('http://localhost:5000/api/chats', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newMessagePayload)
      });

      if (response.ok) {
        // Cập nhật UI mượt mà: Thêm luôn tin nhắn vào danh sách hiện tại mà không cần fetch lại toàn bộ
        setChatHistory(prevHistory => [...prevHistory, { ...newMessagePayload, id: Date.now(), isMine: true }]);
        setMessage(''); // Xóa ô nhập text
      } else {
        alert("Gửi tin nhắn thất bại!");
      }
    } catch (error) {
      console.error("Lỗi khi gửi tin:", error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', gap: '20px', maxWidth: '800px', margin: '20px auto', fontFamily: 'sans-serif' }}>
      
      {/* Cột Danh bạ */}
      <div style={{ flex: 1, border: '1px solid #ccc', borderRadius: '8px', padding: '10px' }}>
        <h3>Danh bạ</h3>
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {contacts.length > 0 ? contacts.map((contact) => (
            <li key={contact.id} style={{ padding: '8px', borderBottom: '1px solid #eee' }}>
              {contact.name}
            </li>
          )) : <p style={{ fontSize: '14px', color: '#666' }}>Đang tải danh bạ...</p>}
        </ul>
      </div>

      {/* Cột Lịch sử Chat & Form Gửi */}
      <div style={{ flex: 2, border: '1px solid #ccc', borderRadius: '8px', padding: '10px', display: 'flex', flexDirection: 'column' }}>
        <h3>Khung Chat</h3>
        
        {/* Lịch sử chat */}
        <div style={{ flex: 1, minHeight: '300px', maxHeight: '300px', overflowY: 'auto', border: '1px solid #eee', marginBottom: '10px', padding: '10px' }}>
          {chatHistory.length > 0 ? chatHistory.map((chat) => (
            <div key={chat.id} style={{ textAlign: chat.isMine ? 'right' : 'left', marginBottom: '10px' }}>
              <span style={{ 
                display: 'inline-block', padding: '8px 12px', borderRadius: '15px', 
                backgroundColor: chat.isMine ? '#007bff' : '#e9ecef',
                color: chat.isMine ? 'white' : 'black' 
              }}>
                {chat.content}
              </span>
            </div>
          )) : <p style={{ textAlign: 'center', color: '#888' }}>Chưa có tin nhắn nào.</p>}
        </div>

        {/* Khung nhập tin nhắn */}
        <form onSubmit={handleSendMessage} style={{ display: 'flex', gap: '10px' }}>
          <input 
            type="text" 
            value={message} 
            onChange={(e) => setMessage(e.target.value)} 
            placeholder="Nhập tin nhắn..." 
            style={{ flex: 1, padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
          />
          <button type="submit" disabled={isLoading} style={{ padding: '10px 20px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
            {isLoading ? 'Đang gửi...' : 'Gửi'}
          </button>
        </form>
      </div>

    </div>
  );
};

export default ChatLogic;