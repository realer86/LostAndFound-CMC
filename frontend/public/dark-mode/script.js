const toggleBtn = document.getElementById('theme-toggle');
const htmlElement = document.documentElement;

// Hàm cập nhật giao diện và nội dung nút bấm
function applyTheme(theme) {
  htmlElement.setAttribute('data-theme', theme);
  localStorage.setItem('theme', theme);
  
  if (theme === 'dark') {
    toggleBtn.textContent = '☀️ Chế độ sáng';
  } else {
    toggleBtn.textContent = '🌙 Chế độ tối';
  }
}

// Khởi tạo theme khi tải trang
function initTheme() {
  const savedTheme = localStorage.getItem('theme');
  const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

  if (savedTheme) {
    applyTheme(savedTheme);
  } else if (systemPrefersDark) {
    applyTheme('dark');
  } else {
    applyTheme('light');
  }
}

// Xử lý sự kiện click nút chuyển đổi
toggleBtn.addEventListener('click', () => {
  const currentTheme = htmlElement.getAttribute('data-theme');
  const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
  applyTheme(newTheme);
});

// Tự động đổi theme nếu người dùng đổi cài đặt trên Windows/macOS/iOS
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
  // Chỉ tự động đổi nếu người dùng chưa tự thiết lập thủ công
  if (!localStorage.getItem('theme')) {
    applyTheme(e.matches ? 'dark' : 'light');
  }
});

// Chạy khởi tạo
initTheme();