import React, { useState } from 'react';

const PostForm = () => {
  // State lưu dữ liệu form
  const [formData, setFormData] = useState({
    itemName: '',
    locationId: '',
    imageFile: null
  });

  // State lưu thông báo lỗi
  const [errors, setErrors] = useState({});

  // Xử lý khi input thay đổi
  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  // Xử lý khi upload file
  const handleFileChange = (e) => {
    setFormData({ ...formData, imageFile: e.target.files[0] });
  };

  // Xử lý sự kiện Submit form
  const handleSubmit = async (e) => {
    e.preventDefault(); // Ngăn form reload lại trang
    let newErrors = {};

    // 1. Validation (Bắt lỗi đầu vào theo yêu cầu)
    if (!formData.itemName.trim()) {
      newErrors.itemName = "Bắt buộc nhập tên đồ vật!";
    }
    
    if (!formData.locationId) {
      newErrors.locationId = "Bắt buộc chọn khu vực/tòa nhà!";
    }

    if (formData.imageFile) {
      // Kiểm tra định dạng file ảnh (chỉ cho phép jpg/png)
      const validTypes = ['image/jpeg', 'image/png'];
      if (!validTypes.includes(formData.imageFile.type)) {
        newErrors.imageFile = "Định dạng ảnh không hợp lệ (chỉ nhận .jpg hoặc .png)!";
      }
    }

    // Nếu có lỗi -> Cập nhật state để hiển thị cảnh báo đỏ và dừng thực thi
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    // Xóa lỗi cũ nếu form hợp lệ
    setErrors({});
    
    // Log ra console chuẩn xác theo tiêu chí nghiệm thu
    console.log("✅ Submit form hợp lệ. Dữ liệu chuẩn bị gửi đi:", formData);

    // 2. Chuẩn bị luồng ghép nối API (Chờ Backend cung cấp Endpoint)
    /* 
    try {
      // Vì có file ảnh, phải dùng FormData thay vì JSON
      const payload = new FormData();
      payload.append('itemName', formData.itemName);
      payload.append('locationId', formData.locationId);
      if (formData.imageFile) {
        payload.append('image', formData.imageFile);
      }

      // Hàm fetch cấu hình sẵn gọi API
      const response = await fetch('URL_BACKEND_API/items', {
        method: 'POST',
        body: payload
      });

      if (response.ok) {
        console.log("Đăng bài thành công!");
      }
    } catch (error) {
      console.error("Lỗi gọi API:", error);
    }
    */
  };

  return (
    <div style={{ maxWidth: '400px', margin: '20px auto', fontFamily: 'sans-serif' }}>
      <h3>Form Đăng Tin (Logic Xử lý)</h3>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
        
        {/* Tên đồ vật */}
        <div>
          <label>Tên đồ vật *</label>
          <input type="text" name="itemName" onChange={handleChange} style={{ width: '100%', padding: '8px' }} />
          {/* Hiển thị cảnh báo đỏ nếu có lỗi */}
          {errors.itemName && <span style={{ color: 'red', fontSize: '12px' }}>{errors.itemName}</span>}
        </div>

        {/* Khu vực/Tòa nhà */}
        <div>
          <label>Khu vực / Tòa nhà *</label>
          <select name="locationId" onChange={handleChange} style={{ width: '100%', padding: '8px' }}>
            <option value="">-- Chọn khu vực --</option>
            <option value="1">Tòa nhà Vạn Phúc</option>
            <option value="2">Khuôn viên Trường ĐH CMC</option>
          </select>
          {errors.locationId && <span style={{ color: 'red', fontSize: '12px' }}>{errors.locationId}</span>}
        </div>

        {/* File ảnh */}
        <div>
          <label>Hình ảnh đính kèm (jpg/png)</label>
          <input type="file" accept="image/png, image/jpeg" onChange={handleFileChange} style={{ width: '100%' }} />
          {errors.imageFile && <span style={{ color: 'red', fontSize: '12px', display: 'block' }}>{errors.imageFile}</span>}
        </div>

        <button type="submit" style={{ padding: '10px', backgroundColor: '#0056b3', color: 'white', border: 'none', cursor: 'pointer' }}>
          Gửi dữ liệu
        </button>
      </form>
    </div>
  );
};

export default PostForm;
