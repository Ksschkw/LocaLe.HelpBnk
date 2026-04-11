import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000 // 15s timeout
})

api.interceptors.response.use(
  res => res,
  err => {
    if (err.message === 'Network Error' || err.code === 'ECONNABORTED' || !err.response) {
      window.dispatchEvent(new CustomEvent('locale-server-down'))
    } else if (err.response && err.response.data) {
        const data = err.response.data;
        let errorMsg = data.error || data.Error || data.message || data.Message || data.detail || data.title;
        if (data.errors && typeof data.errors === 'object') {
            const firstKey = Object.keys(data.errors)[0];
            if (firstKey && Array.isArray(data.errors[firstKey]) && data.errors[firstKey].length > 0) {
                errorMsg = data.errors[firstKey][0];
            }
        }
        if (errorMsg) {
            err.message = errorMsg;
        }
    }
    return Promise.reject(err)
  }
)

// --- Auth / Users ---
export const authApi = {
  login:    (data) => api.post('/users/login', data),
  register: (data) => api.post('/users', data),
  logout:   ()     => api.post('/users/logout'),
  me:       ()     => api.get('/users/me'),
}

export const usersApi = {
  getPublicProfile: (id) => api.get(`/users/${id}`),
  updateProfile:    (data) => api.put('/users/me', data),
  deleteAccount:    () => api.delete('/users/me'),
  purchaseBadge:    () => api.post('/users/me/purchase-badge')
}

// --- Admin ---
export const adminApi = {
  getUsers:        (page = 1, pageSize = 20) => api.get('/Admin/users', { params: { page, pageSize } }),
  createAdmin:     (data) => api.post('/Admin/users', data),
  getUserDetails:  (id) => api.get(`/Admin/users/${id}`),
  deleteUser:      (id) => api.delete(`/Admin/users/${id}`),
  setRole:         (id, role) => api.put(`/Admin/users/${id}/role`, { role }),
  getJobs:         () => api.get('/Admin/jobs'),
  getJobTimeline:  (id) => api.get(`/Admin/jobs/${id}/timeline`),
  exportMetrics:   () => api.get('/Admin/metrics/export'),
  getDisputes:     () => api.get('/Admin/disputes'),
  resolveDispute:  (id, resolution) => api.put(`/Admin/disputes/${id}/resolve`, { resolution }),
  broadcast:       (message) => api.post('/Admin/broadcast', { message }),
  getEarnings:     () => api.get('/Admin/platform-earnings'),
  getPayments:     () => api.get('/Admin/payments'),
  refundPayment:   (id) => api.post(`/Admin/payments/${id}/refund`),
  toggleDiscovery: (id, enable) => api.put(`/Admin/services/${id}/discovery`, null, { params: { enable } }),
}

// --- Bookings ---
export const bookingsApi = {
  apply:       (jobId) => api.post(`/Bookings/apply/${jobId}`),
  confirm:     (bookingId) => api.post(`/Bookings/${bookingId}/confirm`),
  mine:        () => api.get('/Bookings/mine'),
  setStatus:   (id, status) => api.put(`/Bookings/${id}/status`, status, { headers: { 'Content-Type': 'application/json' } }),
  delete:      (id) => api.delete(`/Bookings/${id}`),
}

// --- Chats ---
export const chatsApi = {
  getMessages:    (jobId) => api.get(`/Chats/${jobId}`),
  sendMessage:    (jobId, data) => api.post(`/Chats/${jobId}`, data),
  getPinned:      (jobId) => api.get(`/Chats/${jobId}/pinned`),
  togglePin:      (jobId, messageId) => api.post(`/Chats/${jobId}/messages/${messageId}/pin`),
  deleteMessage:  (jobId, messageId) => api.delete(`/Chats/${jobId}/messages/${messageId}`),
}

// --- Disputes ---
export const disputesApi = {
  raise:       (jobId, reason) => api.post(`/disputes/jobs/${jobId}/dispute`, reason, { headers: { 'Content-Type': 'application/json' } }),
  respond:     (disputeId, data) => api.post(`/disputes/${disputeId}/respond`, data),
  myDisputes:  () => api.get('/disputes/my-disputes'),
  getDetails:  (disputeId) => api.get(`/disputes/${disputeId}`),
}

// --- Escrow ---
export const escrowApi = {
  getByBooking:   (bookingId) => api.get(`/Escrow/booking/${bookingId}`),
  release:        (escrowId, qrToken) => api.post(`/Escrow/${escrowId}/release`, { qrToken }),
  dispute:        (escrowId) => api.post(`/Escrow/${escrowId}/dispute`),
  cancel:         (escrowId) => api.post(`/Escrow/${escrowId}/cancel`),
  refreshQr:      (escrowId) => api.post(`/Escrow/${escrowId}/refresh-qr`),
  getAudit:       (escrowId) => api.get(`/Escrow/${escrowId}/audit`),
}

// --- Jobs ---
export const jobsApi = {
  getAll:             () => api.get('/Jobs'),
  create:             (data) => api.post('/Jobs', data),
  getById:            (id) => api.get(`/Jobs/${id}`),
  update:             (id, data) => api.put(`/Jobs/${id}`, data),
  delete:             (id) => api.delete(`/Jobs/${id}`),
  myRequests:         () => api.get('/Jobs/my-requests'),
  myServiceRequests:  () => api.get('/Jobs/my-service-requests'),
  myOffers:           () => api.get('/Jobs/my-offers'),
  confirmCompletion:  (jobId) => api.post(`/Jobs/${jobId}/confirm-completion`),
  accept:             (jobId) => api.post(`/Jobs/${jobId}/accept`),
  requestService:     (serviceId, data) => api.post(`/Jobs/services/${serviceId}/request`, data),
  getApplicants:      (jobId) => api.get(`/Jobs/${jobId}/applicants`),
}

// --- Notifications ---
export const notificationsApi = {
  getAll:       () => api.get('/Notifications'),
  getUnread:    () => api.get('/Notifications/unread-count'),
  markAllRead:  () => api.post('/Notifications/mark-all-read'),
  markRead:     (id) => api.post(`/Notifications/${id}/read`),
}

// --- Search ---
export const searchApi = {
  services:             (params) => api.get('/Search/services', { params }),
  categories:           (query) => api.get('/Search/categories', { params: { query } }),
  autocompleteServices: (query) => api.get('/Search/autocomplete/services', { params: { query } }),
  autocompleteCategories: (query) => api.get('/Search/autocomplete/categories', { params: { query } }),
}

// --- Seed ---
export const seedApi = {
  seed: () => api.post('/Seed'),
}

// --- Services ---
export const servicesApi = {
  getAll:         (params) => api.get('/Services', { params }),
  create:         (data) => api.post('/Services', data),
  getById:        (id) => api.get(`/Services/${id}`),
  update:         (id, data) => api.put(`/Services/${id}`, data),
  delete:         (id) => api.delete(`/Services/${id}`),
  getMyServices:  () => api.get('/Services/my'),
  getBySeller:    (sellerId) => api.get(`/Services/seller/${sellerId}`),
  activate:       (id) => api.post(`/Services/${id}/activate`),
}

// --- Categories (under Services routing logic) ---
export const categoriesApi = {
  getAll:         () => api.get('/Services/categories'),
  create:         (data) => api.post('/Services/categories', data),
  getById:        (id) => api.get(`/Services/categories/${id}`),
  getServices:    (categoryId) => api.get(`/Services/categories/${categoryId}/services`),
}

// --- Vouches ---
export const vouchApi = {
  vouch:      (serviceId, comment) => api.post(`/Vouchers/${serviceId}`, comment, { headers: { 'Content-Type': 'application/json' } }),
  guestVouch: (serviceId, data) => api.post(`/Vouchers/guest/${serviceId}`, data),
  getPoints:  (serviceId) => api.get(`/Vouchers/${serviceId}/points`),
}

// --- Waitlists ---
export const waitlistsApi = {
  join:       (serviceId, message) => api.post(`/Waitlists/${serviceId}`, message, { headers: { 'Content-Type': 'application/json' } }),
  myWaitlists:() => api.get('/Waitlists/my'),
  agreeTerms: (id, data) => api.post(`/Waitlists/${id}/agree`, data),
}

// --- Wallet ---
export const walletApi = {
  get:           () => api.get('/Wallet'),
  topup:         (amount) => api.post('/Wallet/topup', { amount }),
  withdraw:      (amount) => api.post('/Wallet/withdraw', { amount }),
  transactions:  () => api.get('/Wallet/transactions'),
}

export default api
