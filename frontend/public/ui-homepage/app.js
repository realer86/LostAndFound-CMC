/**
 * Foundly / GitHub Issue #2: presentation-only demo (NO fetch, API, DB, or auth).
 * Test data is intentionally local and temporary. Backend work belongs to Issue #6.
 */
'use strict';

const demoItems = [
  { id:1, type:'LOST', name:'Balo màu kem', location:'Thư viện', date:'20/10/2026', description:'Balo có ngăn laptop, móc khóa hình gấu. Để quên tại khu vực bàn học.', emoji:'🎒', tone:'mint' },
  { id:2, type:'FOUND', name:'Chùm chìa khóa', location:'Nhà A', date:'19/10/2026', description:'Một chùm chìa khóa có móc khóa màu xanh, được nhặt tại hành lang.', emoji:'🔑', tone:'sand' },
  { id:3, type:'LOST', name:'Tai nghe không dây', location:'Nhà B', date:'19/10/2026', description:'Hộp tai nghe màu trắng, có vỏ bảo vệ trong suốt.', emoji:'🎧', tone:'lavender' },
  { id:4, type:'FOUND', name:'Ví đựng thẻ sinh viên', location:'Căng tin', date:'18/10/2026', description:'Ví nhỏ màu nâu, chưa công khai thông tin định danh để bảo vệ chủ sở hữu.', emoji:'👛', tone:'rose' },
  { id:5, type:'LOST', name:'Bình nước giữ nhiệt', location:'Sân trường', date:'17/10/2026', description:'Bình giữ nhiệt màu bạc, có dây cầm, để quên gần ghế đá.', emoji:'🥤', tone:'grey' },
  { id:6, type:'FOUND', name:'Sổ ghi chép', location:'Thư viện', date:'17/10/2026', description:'Cuốn sổ bìa xanh khổ A5, được nhặt ở khu vực phòng đọc.', emoji:'📘', tone:'blue' }
];

const $ = (selector, root=document) => root.querySelector(selector);
const $$ = (selector, root=document) => [...root.querySelectorAll(selector)];
const homeView = $('#home-view');
const postView = $('#post-view');
const nav = $('#primary-navigation');
const menuToggle = $('.menu-toggle');
const grid = $('#item-grid');
const searchInput = $('#search-input');
const locationFilter = $('#location-filter');
const filterButtons = $$('[data-filter]');
const form = $('#post-form');
const dialog = $('#preview-modal');
let activeFilter = 'ALL';
let previewImageUrls = [];

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, c => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[c]));
}
function cardTemplate(item) {
  const badge = item.type === 'LOST' ? 'ĐỒ BỊ MẤT' : 'ĐỒ NHẶT ĐƯỢC';
  const badgeClass = item.type === 'FOUND' ? 'found' : '';
  return `<article class="item-card" aria-label="${escapeHtml(item.name)} - ${badge}">
    <div class="card-image" data-tone="${item.tone}"><span class="type-badge ${badgeClass}">${badge}</span><span class="card-emoji" aria-hidden="true">${item.emoji}</span></div>
    <div class="card-content"><div class="card-meta"><svg class="icon" aria-hidden="true"><use href="#ico-calendar"/></svg> ${item.date}</div>
      <h3>${escapeHtml(item.name)}</h3><p>${escapeHtml(item.description)}</p>
      <div class="card-bottom"><span><svg class="icon" aria-hidden="true"><use href="#ico-location"/></svg> ${escapeHtml(item.location)}</span><b>Minh họa</b></div>
    </div></article>`;
}
function renderCards() {
  const needle = searchInput.value.trim().toLocaleLowerCase('vi');
  const location = locationFilter.value;
  const filtered = demoItems.filter(item =>
    (activeFilter === 'ALL' || item.type === activeFilter) &&
    (location === 'ALL' || item.location === location) &&
    `${item.name} ${item.description} ${item.location}`.toLocaleLowerCase('vi').includes(needle)
  );
  grid.innerHTML = filtered.map(cardTemplate).join('');
  $('#listing-count').textContent = `${filtered.length} tin đăng mẫu`;
  $('#empty-state').hidden = filtered.length !== 0;
}
function setFilter(type) {
  activeFilter = type;
  filterButtons.forEach(button => {
    const isActive = button.dataset.filter === type;
    button.classList.toggle('is-selected', isActive);
    button.setAttribute('aria-pressed', String(isActive));
  });
  renderCards();
}
function setReportType(type) {
  const radio = $(`input[name="reportType"][value="${type === 'FOUND' ? 'FOUND' : 'LOST'}"]`);
  radio.checked = true;
  $$('.kind-option').forEach(label => label.classList.toggle('is-chosen', $('input', label).checked));
}
function closeMenu() {
  nav.classList.remove('is-open');
  menuToggle.setAttribute('aria-expanded', 'false');
  menuToggle.setAttribute('aria-label', 'Mở menu điều hướng');
}
function navigate(destination, reportType) {
  const showPost = destination === 'post';
  homeView.hidden = showPost;
  postView.hidden = !showPost;
  if (showPost && reportType) setReportType(reportType);
  $$('.nav-link').forEach(link => {
    const active = link.dataset.go === (showPost ? 'post' : destination === 'browse' ? 'browse' : 'home');
    link.classList.toggle('is-active', active);
    if (active) link.setAttribute('aria-current', 'page'); else link.removeAttribute('aria-current');
  });
  closeMenu();
  requestAnimationFrame(() => {
    if (!showPost && destination === 'browse') $('#browse').scrollIntoView({behavior:'smooth'});
    else window.scrollTo({top:0,behavior:'instant'});
  });
}

// Router is deliberately tiny: one static HTML file, two visible screens.
$$('[data-go]').forEach(link => link.addEventListener('click', event => {
  event.preventDefault();
  const destination = link.dataset.go;
  if (window.location.hash !== `#${destination}`) history.pushState({}, '', `#${destination}`);
  navigate(destination, link.dataset.type);
}));
window.addEventListener('popstate', () => navigate(window.location.hash.slice(1) || 'home'));
window.addEventListener('hashchange', () => navigate(window.location.hash.slice(1) || 'home'));
menuToggle.addEventListener('click', () => {
  const open = nav.classList.toggle('is-open');
  menuToggle.setAttribute('aria-expanded', String(open));
  menuToggle.setAttribute('aria-label', open ? 'Đóng menu điều hướng' : 'Mở menu điều hướng');
});
filterButtons.forEach(button => button.addEventListener('click', () => setFilter(button.dataset.filter)));
searchInput.addEventListener('input', renderCards);
locationFilter.addEventListener('change', renderCards);
$('#search-button').addEventListener('click', renderCards);
$('#clear-filters').addEventListener('click', () => {
  searchInput.value = '';
  locationFilter.value = 'ALL';
  setFilter('ALL');
  searchInput.focus();
});
$$('input[name="reportType"]').forEach(radio => radio.addEventListener('change', () => setReportType(radio.value)));

function clearError(fieldId, messageId) {
  const field = document.getElementById(fieldId);
  field.removeAttribute('aria-invalid');
  document.getElementById(messageId).textContent = '';
}
const validators = [
  ['item-name','name-error', input => input.value.trim() ? '' : 'Hãy nhập tên đồ vật.'],
  ['category','category-error', input => input.value ? '' : 'Hãy chọn danh mục.'],
  ['location','location-error', input => input.value ? '' : 'Hãy chọn khu vực.'],
  ['event-time','time-error', input => !input.value ? 'Hãy chọn ngày giờ.' : (new Date(input.value).getTime() > Date.now() ? 'Ngày giờ không thể nằm trong tương lai.' : '')],
  ['description','description-error', input => input.value.trim().length >= 10 ? '' : 'Mô tả cần ít nhất 10 ký tự.']
];
validators.forEach(([id,errorId]) => {
  const input = document.getElementById(id);
  input.addEventListener('input', () => clearError(id,errorId));
  input.addEventListener('change', () => clearError(id,errorId));
});
function releaseImages() { previewImageUrls.forEach(url => URL.revokeObjectURL(url)); previewImageUrls = []; $('#upload-previews').replaceChildren(); }
$('#image-input').addEventListener('change', event => {
  releaseImages();
  const files = [...event.target.files];
  const error = $('#images-error');
  error.textContent = '';
  if (files.length > 3) { error.textContent = 'Chỉ chọn tối đa 3 hình ảnh.'; event.target.value=''; return; }
  if (files.some(file => !['image/jpeg','image/png','image/webp'].includes(file.type) || file.size > 5*1024*1024)) {
    error.textContent = 'Chỉ chọn ảnh JPG, PNG, WEBP không quá 5 MB mỗi ảnh.';
    event.target.value=''; return;
  }
  files.forEach(file => {
    const url = URL.createObjectURL(file);
    previewImageUrls.push(url);
    const image = document.createElement('img');
    image.src = url;
    image.alt = `Ảnh xem trước: ${file.name}`;
    $('#upload-previews').append(image);
  });
});
$('#reset-form').addEventListener('click', () => {
  form.reset();
  setReportType('LOST');
  releaseImages();
  validators.forEach(([id,errorId]) => clearError(id,errorId));
  $('#images-error').textContent = '';
  $('#form-feedback').textContent = '';
});

function previewLine(root, label, value) {
  const row = document.createElement('div');
  const bold = document.createElement('b');
  bold.textContent = label + ': ';
  row.append(bold, document.createTextNode(value));
  root.append(row);
}
form.addEventListener('submit', event => {
  event.preventDefault();
  let firstInvalid = null;
  validators.forEach(([id,errorId,validate]) => {
    const field = document.getElementById(id);
    const error = validate(field);
    $('#'+errorId).textContent = error;
    if (error) { field.setAttribute('aria-invalid','true'); firstInvalid ||= field; }
    else field.removeAttribute('aria-invalid');
  });
  if ($('#images-error').textContent) firstInvalid ||= $('#image-input');
  if (firstInvalid) {
    $('#form-feedback').textContent = 'Vui lòng kiểm tra các thông tin được đánh dấu.';
    firstInvalid.focus();
    return;
  }
  const data = new FormData(form);
  const box = document.createElement('div');
  box.className = 'preview-card';
  const name = document.createElement('strong');
  name.textContent = data.get('itemName').trim();
  box.append(name);
  previewLine(box, 'Loại tin', data.get('reportType') === 'LOST' ? 'Đồ bị mất' : 'Đồ nhặt được');
  previewLine(box, 'Danh mục', data.get('category'));
  previewLine(box, 'Khu vực', data.get('location'));
  previewLine(box, 'Ngày giờ', String(data.get('eventTime')).replace('T',' '));
  if (String(data.get('locationDetail')).trim()) previewLine(box, 'Vị trí cụ thể', String(data.get('locationDetail')).trim());
  const desc = document.createElement('p');
  desc.textContent = String(data.get('description')).trim();
  box.append(desc);
  $('#preview-content').replaceChildren(box);
  $('#form-feedback').textContent = 'Đã mở bản xem trước. Tin chưa được gửi lên hệ thống.';
  dialog.showModal();
});
function closeDialog() { dialog.close(); }
$('.dialog-close').addEventListener('click', closeDialog);
$('.dialog-done').addEventListener('click', closeDialog);
dialog.addEventListener('click', event => { if (event.target === dialog) closeDialog(); });

renderCards();
navigate(['post','browse'].includes(window.location.hash.slice(1)) ? window.location.hash.slice(1) : 'home');
