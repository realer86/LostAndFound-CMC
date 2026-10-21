import React, { useState, useEffect } from 'react';

const PostForm = () => {
  // 1. Khởi tạo State lưu danh sách Dropdown và Dữ liệu Form
  const [categories, setCategories] = useState([]);
  const [locations, setLocations] = useState([]);
  const [message, setMessage] = useState(''); // Chứa thông báo thành công/lỗi
  
  const [formData, setFormData] = useState({
    itemName: '',
    description: '',
    categoryId: '',
    locationId: '',
    type: 'LOST' // LOST (mất đồ) hoặc FOUND (nhặt được)
  });

  // 2. Logic GET API: Lấy dữ liệu Danh mục và Khu vực khi vừa vào trang
  useEffect(() => {
    // Lưu ý: Đổi URL này cho khớp với port Backend chạy thật của nhóm bạn
    fetch('http://localhost:5000/api/categories')
      .then(res => res.json())
      .then(data => setCategories(data))
      .catch(err => console.log("Lỗi fetch categories", err));

    fetch('http://localhost:5000/api/locations')
      .then(res => res.json())
      .then(data => setLocations(data))
      .catch(err => console.log("Lỗi fetch locations", err));
  }, []);

  // Hàm cập nhật dữ liệu khi người dùng gõ/chọn
  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  // 3. Logic POST API: Bấm nút Submit
  const handleSubmit = async (e) => {
    e.preventDefault();

    // Validation: Không cho bỏ trống (Tiêu chí nghiệm thu)
    if (!formData.itemName || !formData.categoryId || !formData.locationId) {
      setMessage('❌ Vui lòng điền đầy đủ Tên đồ vật, Danh mục và Khu vực!');
      return;
    }

    try {
      const response = await fetch('http://localhost:5000/api/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData) // Gửi dữ liệu form xuống Backend
      });

      if (response.status === 201) {
        setMessage('✅ Đăng tin thành công!'); // Hiển thị Toast/Popup
        // Code làm sạch form ở đây nếu cần
      } else {
        setMessage('❌ Đăng tin thất bại, vui lòng kiểm tra lại.');
      }
    } catch (error) {
      setMessage('❌ Lỗi kết nối Server.');
    }
  };

  // 4. UI: Giao diện Form (Sử dụng CSS thuần hoặc TailwindCSS để Responsive)
  return (
    <div style={{ maxWidth: '500px', margin: '0 auto', padding: '20px' }}>
      <h2>Đăng tin thất lạc</h2>
      
      {message && (
        <div style={{ padding: '10px', marginBottom: '15px', backgroundColor: message.includes('✅') ? '#d4edda' : '#f8d7da' }}>
          {message}
        </div>
      )}

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
        
        {/* Tên đồ vật */}
        <div>
          <label>Tên đồ vật *</label>
          <input type="text" name="itemName" value={formData.itemName} onChange={handleChange} style={{ width: '100%', padding: '8px' }} />
        </div>

        {/* Dropdown Danh mục */}
        <div>
          <label>Danh mục *</label>
          <select name="categoryId" value={formData.categoryId} onChange={handleChange} style={{ width: '100%', padding: '8px' }}>
            <option value="">-- Chọn danh mục --</option>
            {categories.map(cat => (
              <option key={cat.id} value={cat.id}>{cat.name}</option>
            ))}
          </select>
        </div>

        {/* Dropdown Khu vực */}
        <div>
          <label>Tòa nhà / Khu vực *</label>
          <select name="locationId" value={formData.locationId} onChange={handleChange} style={{ width: '100%', padding: '8px' }}>
            <option value="">-- Chọn khu vực --</option>
            {locations.map(loc => (
              <option key={loc.id} value={loc.id}>{loc.name}</option>
            ))}
          </select>
        </div>

        {/* Mô tả */}
        <div>
          <label>Mô tả chi tiết</label>
          <textarea name="description" value={formData.description} onChange={handleChange} rows="4" style={{ width: '100%', padding: '8px' }}></textarea>
        </div>

        <button type="submit" style={{ padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}>
          Đăng Tin
        </button>
      </form>
    </div>
  );
};

export default PostForm;