const API_BASE = '/api';

const State = {
    token: localStorage.getItem('locale_token') || '',
    userId: localStorage.getItem('locale_uid') || '',
    userEmail: localStorage.getItem('locale_email') || '',
    walletBalance: 0,
    currentEscrowView: null
};

const showToast = (msg, type='info') => {
    const cont = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = msg;
    cont.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
}

const UI = {
    _modal: (title, msg, type = 'alert', icon = '⚠️', defaultValue = '') => {
        return new Promise((resolve) => {
            const overlay = document.getElementById('global-modal-overlay');
            const box = document.getElementById('global-modal-box');
            
            document.getElementById('global-modal-title').innerText = title; 
            document.getElementById('global-modal-msg').innerHTML = msg.replace(/\n/g, '<br>'); 
            document.getElementById('global-modal-icon').innerText = icon;
            
            const iCont = document.getElementById('global-modal-input-container');
            const inp = document.getElementById('global-modal-input');
            if (type === 'prompt') {
                iCont.style.display = 'block'; inp.value = defaultValue;
                setTimeout(() => inp.focus(), 100);
            } else iCont.style.display = 'none';
            
            const btnCancel = document.getElementById('global-modal-cancel');
            const btnConfirm = document.getElementById('global-modal-confirm');
            btnCancel.style.display = type === 'alert' ? 'none' : 'block';
            
            const cleanup = () => {
                overlay.classList.remove('active');
                setTimeout(() => box.style.display = 'none', 200);
                btnConfirm.onclick = null; btnCancel.onclick = null;
            };
            
            btnConfirm.onclick = () => { cleanup(); resolve(type === 'prompt' ? inp.value : true); };
            btnCancel.onclick = () => { cleanup(); resolve(type === 'prompt' ? null : false); };
            
            box.style.display = 'flex';
            void overlay.offsetWidth;
            overlay.classList.add('active');
        });
    },
    alert: (msg, title = 'Notice') => UI._modal(title, msg, 'alert', 'ℹ️'),
    confirm: (msg, title = 'Confirm Action') => UI._modal(title, msg, 'confirm', '⚠️'),
    prompt: (msg, title = 'Input Required', def = '') => UI._modal(title, msg, 'prompt', '✏️', def)
};

const appNavigation = {
    current: 'startup',
    showView: function(viewId, subAction=null) {
        document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
        document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
        
        const target = document.getElementById(`view-${viewId}`);
        if(target) target.classList.add('active');

        // Manage Sidebar/BottomNav
        const mainFrame = document.getElementById('main-dashboard-frame');
        if (['landing', 'auth', 'startup'].includes(viewId)) {
            mainFrame.classList.remove('active');
        } else {
            mainFrame.classList.add('active');
            const navLink = document.querySelector(`.nav-item[onclick*="${viewId}"]`);
            if (navLink) navLink.classList.add('active');
        }

        if (viewId === 'auth') {
            document.getElementById('form-login').style.display = subAction === 'login' ? 'block' : 'none';
            document.getElementById('form-register').style.display = subAction === 'register' ? 'block' : 'none';
            document.getElementById('auth-title').innerText = subAction === 'login' ? 'Log In' : 'Create Account';
        }

        if (viewId === 'discover') fetchDiscover();
        if (viewId === 'jobs') fetchJobsBoard();
        if (viewId === 'activity') fetchMyActivity();
        if (viewId === 'wallet') fetchWallet();
        if (viewId === 'profile') fetchProfile();
        if (viewId === 'admin') filterAdmin('users');

        // Start notification polling on any authenticated view
        if (!['landing', 'auth', 'startup'].includes(viewId) && !notifInterval) {
            notifInterval = setInterval(pollNotifications, 15000);
            pollNotifications();
        }
    }
};

const openSheet = (id) => {
    document.getElementById('sheet-overlay').classList.add('active');
    document.getElementById(id).classList.add('active');
}
const closeAllSheets = () => {
    document.getElementById('sheet-overlay').classList.remove('active');
    document.querySelectorAll('.action-sheet').forEach(s => s.classList.remove('active'));
    stopScanner(); 
}

function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    input.type = input.type === 'password' ? 'text' : 'password';
}

// ==========================================
// RESILIENT API CLIENT
// ==========================================
async function apiCall(method, endpoint, body) {
    const headers = { 'Content-Type': 'application/json' };
    if (State.token) {
        headers['Authorization'] = `Bearer ${State.token}`;
    }

    try {
        const response = await fetch(`${API_BASE}${endpoint}`, {
            method, headers, body: body ? JSON.stringify(body) : null
        });

        const contentType = response.headers.get("content-type");
        const isJson = contentType && contentType.indexOf("application/json") !== -1;
        const isProblemJson = contentType && contentType.indexOf("application/problem+json") !== -1;

        if (!response.ok) {
            if (isJson || isProblemJson) {
                const err = await response.json();
                let errorMsg = err.error || err.Error || err.message || err.Message || err.detail || err.title;
                
                if (err.errors && typeof err.errors === 'object') {
                    const firstKey = Object.keys(err.errors)[0];
                    if (firstKey && Array.isArray(err.errors[firstKey]) && err.errors[firstKey].length > 0) {
                        errorMsg = err.errors[firstKey][0];
                    }
                }
                
                throw new Error(errorMsg || `Server returned ${response.status}`);
            } else {
                const text = await response.text();
                console.error("Non-JSON Error:", text);
                throw new Error(`Server Error ${response.status}: The request failed natively.`);
            }
        }

        if (response.status === 204) return true;
        if (isJson) return await response.json();
        return true;
    } catch (e) {
        showToast(e.message, 'error');
        throw e;
    }
}

// ==========================================
// AUTHENTICATION
// ==========================================
async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;
    const rememberMe = document.getElementById('login-remember').checked;
    
    try {
        const data = await apiCall('POST', '/users/login', { email, password, rememberMe });
        bindSession(data);
    } catch(err) {} 
}

async function handleRegister(e) {
    e.preventDefault();
    const name = document.getElementById('reg-name').value;
    const email = document.getElementById('reg-email').value;
    const password = document.getElementById('reg-password').value;
    const confirm = document.getElementById('reg-confirm').value;
    
    if (password !== confirm) return showToast("Passwords do not match", "error");

    try {
        const data = await apiCall('POST', '/users', { name, email, password });
        bindSession(data);
    } catch(err) {}
}

function bindSession(data) {
    State.token = data.token;
    State.userEmail = data.email;
    localStorage.setItem('locale_token', State.token);
    
    try {
        const payload = JSON.parse(atob(State.token.split('.')[1]));
        State.userId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload['nameid'];
        localStorage.setItem('locale_uid', State.userId);
    } catch(e) {}
    
    showToast(`Welcome back, ${data.name || ''}!`, 'success');
    appNavigation.showView('discover');
}

async function handleLogout() {
    try { await apiCall('POST', '/users/logout'); } catch(e) {}
    State.token = ''; State.userId = '';
    localStorage.removeItem('locale_token');
    localStorage.removeItem('locale_uid');
    appNavigation.showView('landing');
}

// ==========================================
// 1. DISCOVER & SERVICES (SearchController)
// ==========================================

let searchState = {
    lat: null,
    lon: null,
    radiusKm: null,
    isRemote: null
};

function toggleFilters() {
    const el = document.getElementById('search-filters');
    el.style.display = el.style.display === 'none' ? 'flex' : 'none';
}

function applyLocationFilter() {
    if (navigator.geolocation) {
        showToast('Acquiring precision coordinates...', 'info');
        navigator.geolocation.getCurrentPosition(
            (position) => {
                searchState.lat = position.coords.latitude;
                searchState.lon = position.coords.longitude;
                searchState.radiusKm = document.getElementById('filter-distance').value || 10;
                searchState.isRemote = document.getElementById('filter-remote').checked ? true : null;
                showToast(`Location locked. Max distance: ${searchState.radiusKm}km`, 'success');
                handleSearch(new Event('submit'));
            },
            (error) => { showToast('Location access denied or unavailable.', 'error'); }
        );
    } else {
        showToast('Geolocation is not supported by this browser.', 'error');
    }
}

async function fetchDiscover() {
    try {
        const cats = await apiCall('GET', '/Services/categories');
        const listCats = document.getElementById('list-categories');
        listCats.innerHTML = `<div class="card col" onclick="fetchDiscover()" style="min-width:70px; margin:0 10px 0 0; padding:0.8rem; align-items:center; text-align:center; cursor:pointer; border:1px solid var(--brand-primary);"><h4 style="margin:0; font-size:0.8rem; color:var(--brand-primary);">✦ All</h4></div>` +
            cats.map(c => `
            <div class="card col" onclick="fetchServicesByCategory('${c.id}','${c.name.replace(/'/g, "\\'")}')"
                style="min-width: 140px; margin: 0 10px 0 0; padding: 1rem; align-items:center; text-align:center; cursor:pointer; transition:border-color 0.2s;"
                onmouseover="this.style.borderColor='var(--brand-primary)'" onmouseout="this.style.borderColor=''">
                <h4 style="margin:0; font-size: 0.9rem;">${c.name}</h4>
                <div style="font-size: 0.75rem; color: var(--text-secondary); margin-top: 5px;">${c.serviceCount || 0} providers</div>
            </div>
        `).join('');

        const queryParams = new URLSearchParams();
        if (searchState.lat) queryParams.append('lat', searchState.lat);
        if (searchState.lon) queryParams.append('lon', searchState.lon);
        if (searchState.radiusKm) queryParams.append('radiusKm', searchState.radiusKm);
        if (searchState.isRemote !== null) queryParams.append('isRemote', searchState.isRemote);

        const url = queryParams.toString() ? `/Search/services?query=&${queryParams.toString()}` : '/Services';
        const svcs = await apiCall('GET', url);
        renderServicesList(svcs.slice().reverse(), 'list-services');
    } catch(e) {}
}

async function handleSearch(e) {
    if(e && e.preventDefault) e.preventDefault();
    const q = document.getElementById('search-query').value;
    
    // Always applying remote filter if it's explicitly checked during search
    const remoteChecked = document.getElementById('filter-remote').checked;
    if (remoteChecked) searchState.isRemote = true;
    else if (searchState.isRemote != null && !remoteChecked) searchState.isRemote = null; // reset if unchecked without loc update
    
    // If no query and no location/remote filters, fetch normal feed
    if(!q && searchState.lat == null && searchState.isRemote == null) return fetchDiscover();

    try {
        const queryParams = new URLSearchParams();
        queryParams.append('query', q || "");
        if (searchState.lat) queryParams.append('lat', searchState.lat);
        if (searchState.lon) queryParams.append('lon', searchState.lon);
        if (searchState.radiusKm) queryParams.append('radiusKm', searchState.radiusKm);
        if (searchState.isRemote !== null) queryParams.append('isRemote', searchState.isRemote);

        const results = await apiCall('GET', `/Search/services?${queryParams.toString()}`);
        document.getElementById('list-categories').innerHTML = '<p>Search Active</p>';
        renderServicesList(results, 'list-services');
        if(results.length===0) document.getElementById('list-services').innerHTML = `<p>No matches found.</p>`;
    } catch(e) {}
}

async function handleSearchAutocomplete(e) {
    const q = e.target.value;
    if(q.length < 2) return;
    try {
        const res = await apiCall('GET', `/Search/autocomplete/services?query=${encodeURIComponent(q)}`);
        const dl = document.getElementById('discover-suggestions');
        if(dl) dl.innerHTML = res.map(str => `<option value="${str}">`).join('');
    } catch(err) {}
}

function toggleCustomCategory(val) {
    const box = document.getElementById('sv-custom-category-box');
    if (box) box.style.display = (val === 'NEW') ? 'block' : 'none';
    const nameInput = document.getElementById('sv-custom-category-name');
    if (nameInput) nameInput.required = (val === 'NEW');
}

function renderServicesList(svcs, containerId) {
    const listSvcs = document.getElementById(containerId);
    if (!svcs || svcs.length === 0) { listSvcs.innerHTML = '<p>No active services found.</p>'; return; }

    listSvcs.innerHTML = svcs.map(s => {
        const isOwner = s.providerId === State.userId;
        const vouchBtn = isOwner ? '' : `<button class="vouch-btn" onclick="vouchForService('${s.id}')">👍 Vouch (${s.trustPoints})</button>`;
        const actionArea = isOwner 
            ? `<span style="font-size:0.85rem; font-weight:600; color:var(--text-secondary);">Your Listing</span>` 
            : `<button class="btn btn-primary btn-sm" onclick="requestDirectService('${s.id}', '${s.title.replace(/'/g, "\\'")}', ${s.basePrice})">Book</button>`;

        return `
        <div class="card col gap-2" style="margin-bottom: 0;">
            <div class="row">
                <h3 style="margin:0;">${s.title} ${s.isRemote ? '<span style="font-size:0.6rem; background:var(--brand-primary); padding:2px 6px; border-radius:10px; vertical-align:middle; margin-left:5px; color:#000;">REMOTE</span>' : ''}</h3>
                <div style="font-weight: 700; color: var(--success);">₦${s.basePrice}</div>
            </div>
            ${s.areaName && !s.isRemote ? `<div style="font-size:0.75rem; color:var(--text-secondary);">📍 ${s.areaName}</div>` : ''}
            <p style="margin:0; font-size:0.9rem;">${s.description}</p>
            <div class="row-start" style="margin-top: 0.5rem;">
                ${vouchBtn}
            </div>
            <div class="row" style="margin-top: 1rem; border-top: 1px solid var(--border-color); padding-top: 0.8rem;">
                <span style="font-size:0.8rem; color:var(--brand-primary); cursor:pointer; font-weight:600;" onclick="viewPublicProfile('${s.providerId}')">👤 ${s.providerName}</span>
                <div class="row-start">
                    ${actionArea}
                </div>
            </div>
        </div>`;
    }).join('');
}

// ─── VOUCHING ───
async function vouchForService(serviceId) {
    try {
        await apiCall('POST', `/Vouchers/${serviceId}`, "Great service!");
        showToast('You vouched for this provider +1', 'success');
        fetchDiscover(); 
    } catch(e) {}
}

async function requestDirectService(serviceId, title, defaultAmount) {
    if(await UI.confirm(`Hire directly for ₦${defaultAmount}?`)) {
        try {
            await apiCall('POST', `/Jobs/services/${serviceId}/request`, {
                title: title, 
                description: `Hiring direct direct request.`, 
                amount: defaultAmount
            });
            showToast('Service Booked! Awaiting acceptance.', 'success');
            appNavigation.showView('activity');
        } catch(e) {}
    }
}

// ─── CREATE CUSTOM SERVICE (HOSTING) ───
async function openCreateServiceSheet() {
    try {
        const cats = await apiCall('GET', '/Services/categories');
        const sel = document.getElementById('sv-category');
        sel.innerHTML = '<option value="">-- Select Category --</option>' + 
            cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('') +
            '<option value="NEW">Other (Create New)</option>';
        openSheet('sheet-create-service');
        toggleCustomCategory('');
    } catch(e) {}
}

function toggleLocationFields() {
    const isRemote = document.getElementById('sv-isremote').checked;
    const locBox = document.getElementById('sv-location-box');
    const areaInput = document.getElementById('sv-area');
    if (isRemote) {
        locBox.style.opacity = '0.3';
        areaInput.required = false;
    } else {
        locBox.style.opacity = '1';
        areaInput.required = true;
    }
}

function detectUserLocation() {
    if (navigator.geolocation) {
        document.getElementById('sv-coords-text').innerText = "Locating...";
        navigator.geolocation.getCurrentPosition(
            (position) => {
                document.getElementById('sv-lat').value = position.coords.latitude;
                document.getElementById('sv-lon').value = position.coords.longitude;
                document.getElementById('sv-coords-text').innerText = `Target locked ✅ (${position.coords.latitude.toFixed(4)}, ${position.coords.longitude.toFixed(4)})`;
            },
            (error) => { document.getElementById('sv-coords-text').innerText = "Access denied or unavailable"; }
        );
    }
}

async function handleCreateService(e) {
    e.preventDefault();
    try {
        let catId = document.getElementById('sv-category').value;
        if(catId === 'NEW') {
            const newName = document.getElementById('sv-custom-category-name').value;
            const newCat = await apiCall('POST', '/Services/categories', { name: newName });
            catId = newCat.id;
        }

        const isRemote = document.getElementById('sv-isremote').checked;
        const payload = {
            categoryId: catId,
            title: document.getElementById('sv-title').value,
            description: document.getElementById('sv-desc').value,
            basePrice: Number(document.getElementById('sv-base').value),
            hourlyRate: Number(document.getElementById('sv-hourly').value || 0),
            isRemote: isRemote,
            areaName: isRemote ? null : document.getElementById('sv-area').value,
            latitude: document.getElementById('sv-lat').value ? Number(document.getElementById('sv-lat').value) : null,
            longitude: document.getElementById('sv-lon').value ? Number(document.getElementById('sv-lon').value) : null
        };

        const svcRes = await apiCall('POST', '/Services', payload);

        // Activate immediately so public sees it
        await apiCall('POST', `/Services/${svcRes.id}/activate`);
        showToast('Service published and active.', 'success');
        closeAllSheets();
        fetchDiscover();
    } catch(e) {}
}

// ==========================================
// 2. JOBS BOARD (JobsController)
// ==========================================
async function fetchJobsBoard() {
    try {
        const jobs = await apiCall('GET', '/Jobs');
        const openJobs = jobs.filter(j => j.status === 'Open' && j.creatorId !== State.userId);
        const list = document.getElementById('list-jobs');
        
        if (openJobs.length===0) { list.innerHTML = `<p style="text-align:center;">No open requests right now.</p>`; return; }

        list.innerHTML = openJobs.map(j => `
            <div class="card col gap-2" style="margin-bottom:0;">
                <div class="row"><h3 style="margin:0;">${j.title}</h3><div style="font-weight: 700; color: var(--success);">₦${j.amount}</div></div>
                <p style="margin:0; font-size:0.9rem;">${j.description}</p>
                <div class="row" style="margin-top: 0.5rem; justify-content: space-between; align-items:flex-end;">
                    <span style="font-size:0.75rem; color:var(--text-tertiary);">Requested by Buyer</span>
                    <button class="btn btn-primary btn-sm" onclick="applyToJob('${j.id}')">Apply to Fulfill</button>
                </div>
            </div>
        `).join('');
    } catch(e) {}
}

async function handlePostJob(e) {
    e.preventDefault();
    const title = document.getElementById('post-title').value;
    const desc = document.getElementById('post-desc').value;
    const amount = Number(document.getElementById('post-amount').value);

    try {
        await apiCall('POST', '/Jobs', { title, description: desc, amount });
        showToast('Request Broadcasted!', 'success');
        closeAllSheets();
        appNavigation.showView('activity');
        filterActivity('client');
    } catch(e) {}
}

async function applyToJob(jobId) {
    try {
        await apiCall('POST', `/Bookings/apply/${jobId}`);
        showToast('Application sent to Buyer!', 'success');
        appNavigation.showView('activity');
        filterActivity('provider');
    } catch(e) {}
}

// ==========================================
// 3. ACTIVITY (Bookings, Escrow)
// ==========================================
let currentActivityMode = 'client';
function filterActivity(mode) {
    currentActivityMode = mode;
    ['client','provider'].forEach(m => document.getElementById(`btn-act-${m}`).classList.toggle('active', mode === m));
    fetchMyActivity();
}

async function fetchMyActivity() {
    try {
        const list = document.getElementById('list-activity');
        list.innerHTML = `<p style="text-align:center;">Loading activity...</p>`;

        const bookings = await apiCall('GET', '/Bookings/mine');
        
        if (currentActivityMode === 'client') {
            const jobs = await apiCall('GET', '/Jobs/my-requests');
            if (jobs.length === 0) { list.innerHTML = `<p>You haven't requested any jobs.</p>`; return; }
            
            list.innerHTML = jobs.map(j => {
                const bks = bookings.filter(b => b.jobId === j.id);
                const pending = bks.find(b => b.status === 'Pending');
                const accepted = bks.find(b => b.status === 'Active' || b.status === 'Accepted');
                const disputed = bks.find(b => b.status === 'Disputing');

                let statusUI = `<span class="badge bg-gray">Open Request</span>`;
                let safeClientTitle = j.title.replace(/'/g, "\\'");
                let actionUI = `<button class="btn btn-secondary btn-sm" onclick="openContractRoom('${j.id}', '${safeClientTitle}', '${pending?.id || ''}', 'client', '${j.status}')">Contract Room 💬</button>`;

                if (j.status === 'Booked') {
                    statusUI = `<span class="badge bg-blue">Escrow Active</span>`;
                    actionUI += ` <button class="btn btn-secondary btn-sm" onclick="viewEscrow('${accepted?.id}')">Show Security QR</button>`;
                    if (!disputed) actionUI += ` <button class="btn btn-danger btn-sm" style="margin-top: 4px;" onclick="raiseDispute('${j.id}')">Dispute</button>`;
                    else statusUI = `<span class="badge bg-red">In Dispute</span>`;
                } else if (j.status === 'Completed') {
                    statusUI = `<span class="badge bg-green">Completed</span>`; actionUI = '';
                }

                return `<div class="card col gap-2" style="margin-bottom:0;">
                    <h3 style="margin:0;">${j.title}</h3>
                    <div class="row-start">${statusUI}</div>
                    <div class="row-start" style="margin-top:0.5rem; flex-wrap:wrap;">${actionUI}</div>
                </div>`;
            }).join('');
        } 
        else {
            const provBks = bookings.filter(b => b.providerId === State.userId);
            const directRequests = await apiCall('GET', '/Jobs/my-service-requests').catch(() => []);
            
            if (provBks.length === 0 && directRequests.length === 0) { list.innerHTML = `<p>No active gigs yet.</p>`; return; }
            
            let html = directRequests.map(r => {
                let badge = r.status === 'Open' ? `<span class="badge bg-orange">Direct Request - Awaiting You</span>` : `<span class="badge bg-gray">${r.status}</span>`;
                let safeTitle = r.title.replace(/'/g, "\\'");
                let btn = `<button class="btn btn-secondary btn-sm" onclick="openContractRoom('${r.id}', '${safeTitle}', '', 'provider', '${r.status}')">Contract Room 💬</button>`;
                
                return `<div class="card col gap-2" style="border-left: 4px solid var(--warning); margin-bottom:0;">
                    <h3 style="margin:0;">Targeted Request: ${r.title}</h3>
                    <div class="row-start">${badge}</div>
                    <div class="row-start" style="margin-top:0.5rem; flex-wrap:wrap;">${btn}</div>
                </div>`;
            }).join('');

            html += provBks.map(b => {
                let badge = ''; 
                let safeTitle = b.jobTitle.replace(/'/g, "\\'");
                let btn = `<button class="btn btn-secondary btn-sm" onclick="openContractRoom('${b.jobId}', '${safeTitle}', '${b.id}', 'provider', '${b.status}')">Contract Room 💬</button>`;

                if (b.status === 'Pending') badge = `<span class="badge bg-orange">Application Sent - Awaiting Buyer Confirm</span>`;
                else if (b.status === 'Active' || b.status === 'Accepted') {
                    badge = `<span class="badge bg-blue">Job Active</span>`;
                    btn += ` <button class="btn btn-primary btn-sm" onclick="openScanner('${b.id}')">Scan QR (Get Paid)</button>`;
                }
                else if (b.status === 'Completed') badge = `<span class="badge bg-green">Finished</span>`;
                else if (b.status === 'Disputing') badge = `<span class="badge bg-red">Disputed</span>`;
                
                return `<div class="card col gap-2" style="margin-bottom:0;">
                    <h3 style="margin:0;">Delivering: ${b.jobTitle}</h3>
                    <div class="row-start">${badge}</div>
                    <div class="row-start" style="margin-top:0.5rem; flex-wrap:wrap;">${btn}</div>
                </div>`;
            }).join('');
            
            list.innerHTML = html;
        }
    } catch(e) {}
}

async function confirmBooking(bookingId) {
    if(!await UI.confirm("Are you sure you want to lock the agreed terms into Escrow? This instantly deducts funds from your wallet.")) return;
    try {
        await apiCall('POST', `/Bookings/${bookingId}/confirm`);
        showToast('Provider secured! Funds locked.', 'success');
        appNavigation.showView('activity'); // Force refresh
    } catch(e) {}
}

async function raiseDispute(jobId) {
    const reason = await UI.prompt("Describe clearly why you are freezing this Escrow:");
    if(!reason) return;
    try {
        await apiCall('POST', `/disputes/jobs/${jobId}/dispute`, reason);
        showToast('Dispute lodged. Awaiting Admin.', 'success');
        fetchMyActivity();
    } catch(e) {}
}

// ─── CHAT CONTRACT ROOM ───
let currentChatJobId = null;
let chatInterval = null;

async function openContractRoom(jobId, jobTitle, bookingId, role, status) {
    currentChatJobId = jobId;
    document.getElementById('chat-title').innerText = jobTitle;
    document.getElementById('chat-job-status').innerText = status;
    
    // Dynamic Top Action Bar
    const actionArea = document.getElementById('chat-dynamic-action');
    
    if (role === 'provider' && !bookingId && (status === 'Open' || status === 'Pending' || status === 'Assigned')) {
        // Direct Request from Buyer -> Provider needs to Accept
        actionArea.innerHTML = `<button class="btn btn-success btn-block" onclick="acceptDirectRequest('${jobId}')">Accept Request &amp; Demand Escrow</button>`;
    } 
    else if (role === 'client' && bookingId && (status === 'Open' || status === 'Pending')) {
        // Buyer reviewing a Provider's Application
        actionArea.innerHTML = `
            <button class="btn btn-success btn-block" style="margin-bottom:6px;" onclick="confirmBooking('${bookingId}')">✅ Approve Provider &amp; Lock Escrow</button>
            <button class="btn btn-danger btn-block" onclick="rejectBooking('${bookingId}')">✕ Reject Application</button>`;
    } 
    else if (role === 'client' && (status === 'Booked' || status === 'Assigned' || status === 'Active' || status === 'Accepted')) {
        actionArea.innerHTML = `<button class="btn btn-primary btn-block" style="margin-bottom:8px;" onclick="viewEscrow('${bookingId}')">Show Scanner QR Code</button>
                                <button class="btn btn-success btn-block" onclick="confirmJobCompletion('${jobId}')">Mark Job Complete (Remote Escrow Release)</button>`;
    }
    else if (status === 'Booked' || status === 'Assigned' || status === 'Active' || status === 'Accepted') {
        actionArea.innerHTML = `<button class="btn btn-primary btn-block" onclick="openScanner('${bookingId}')">Scan QR &amp; Release Funds</button>
                                <div style="text-align:center; font-size:0.75rem; margin-top:4px; color:var(--success);">Terms Locked &amp; Escrow Active. Proceed with job!</div>`;
    }
    else if (role === 'provider' && bookingId && status === 'Pending') {
        actionArea.innerHTML = `
            <span class="badge bg-orange" style="display:block; margin-bottom:6px;">Application Pending — Awaiting Buyer Approval</span>
            <button class="btn btn-secondary btn-block" style="margin-top:6px;" onclick="withdrawApplication('${bookingId}')">↩ Withdraw Application</button>`;
    }
    else {
        actionArea.innerHTML = `<span class="badge bg-orange">Status: ${status}</span>`;
    }

    appNavigation.showView('contract-room');
    loadChats();
    if(chatInterval) clearInterval(chatInterval);
    chatInterval = setInterval(loadChats, 5000); // Live poll
}

async function confirmJobCompletion(jobId) {
    if(!await UI.confirm("Are you absolutely sure the job is completed? This action will permanently finalize the job.")) return;
    try {
        await apiCall('POST', `/Jobs/${jobId}/confirm-completion`);
        showToast('Job officially completed!', 'success');
        appNavigation.showView('activity'); 
        fetchMyActivity();
    } catch(e) {}
}

async function acceptDirectRequest(jobId) {
    if(!await UI.confirm("Accepting this creates a binding contract and locks the Buyer's funds into Escrow. Proceed?")) return;
    try {
        await apiCall('POST', `/Jobs/${jobId}/accept`);
        showToast('Job Accepted! Buyer Funds Locked.', 'success');
        appNavigation.showView('activity'); 
        fetchMyActivity(); // Refresh list immediately
    } catch(e) {}
}

// ─── ENCRYPTION ENGINE (AES-GCM, client-side only) ───
const E2E = {
    _keyCache: {},
    // Derive a room-specific AES-GCM key from jobId + shared secret
    async getRoomKey(jobId) {
        if (this._keyCache[jobId]) return this._keyCache[jobId];
        const rawKey = `locale-e2e-${jobId}-${State.userId.substring(0,8)}`;
        const keyMaterial = await crypto.subtle.importKey(
            'raw', new TextEncoder().encode(rawKey.padEnd(32, '0').substring(0,32)),
            { name: 'AES-GCM' }, false, ['encrypt', 'decrypt']
        );
        this._keyCache[jobId] = keyMaterial;
        return keyMaterial;
    },
    async encrypt(text, jobId) {
        const key = await this.getRoomKey(jobId);
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const ciphertext = await crypto.subtle.encrypt(
            { name: 'AES-GCM', iv }, key, new TextEncoder().encode(text)
        );
        const combined = new Uint8Array(iv.byteLength + ciphertext.byteLength);
        combined.set(iv, 0);
        combined.set(new Uint8Array(ciphertext), iv.byteLength);
        return btoa(String.fromCharCode(...combined));
    },
    async decrypt(b64, jobId) {
        try {
            const key = await this.getRoomKey(jobId);
            const combined = Uint8Array.from(atob(b64), c => c.charCodeAt(0));
            const iv = combined.slice(0, 12);
            const data = combined.slice(12);
            const plain = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, key, data);
            return new TextDecoder().decode(plain);
        } catch { return '🔒 [Encrypted — cannot decrypt]'; }
    }
};

let isE2EEnabled = false;
let replyingTo = null; // { id, content, senderName }

function toggleE2E() {
    isE2EEnabled = !isE2EEnabled;
    const btn = document.getElementById('btn-toggle-e2e');
    if(btn) btn.innerText = isE2EEnabled ? '🔒 Encrypted' : '🔓 Plaintext';
    showToast(isE2EEnabled ? 'E2E Encryption ON for this session' : 'Encryption OFF', isE2EEnabled ? 'success' : 'error');
}

function setReplyTo(msgId, content, senderName) {
    replyingTo = { id: msgId, content, senderName };
    const bar = document.getElementById('reply-bar');
    if(bar) {
        bar.style.display = 'block';
        document.getElementById('reply-preview').innerText = `Replying to ${senderName}: "${content.substring(0,60)}..."`;
    }
    document.getElementById('chat-input')?.focus();
}

function clearReply() {
    replyingTo = null;
    const bar = document.getElementById('reply-bar');
    if(bar) bar.style.display = 'none';
}

async function loadChats() {
    if(!currentChatJobId) return;
    try {
        const chats = await apiCall('GET', `/Chats/${currentChatJobId}`);
        const cbox = document.getElementById('chat-messages');
        const wasAtBottom = cbox.scrollHeight - cbox.scrollTop <= cbox.clientHeight + 50;

        const rows = await Promise.all(chats.map(async c => {
            const isMe = c.senderId === State.userId;
            let displayContent = c.content;

            if (c.isEncrypted && !c.isDeleted) {
                displayContent = await E2E.decrypt(c.content, currentChatJobId);
            }

            const isPinnedTag = c.isPinned ? `<span style="font-size:0.65rem; color:var(--warning); margin-left:4px;">📌 PINNED</span>` : '';
            const isDeletedStyle = c.isDeleted ? 'opacity:0.5; font-style:italic;' : '';
            const encIcon = c.isEncrypted ? '🔒 ' : '';

            const replySnippet = c.parentContentPreview ? `
                <div style="background: rgba(255,255,255,0.08); border-left: 3px solid var(--brand-primary); padding: 0.3rem 0.6rem; border-radius: 4px; font-size:0.75rem; margin-bottom:4px; color:var(--text-secondary); cursor:pointer;" onclick="scrollToMessage('${c.parentMessageId}')">
                    <strong>${c.parentSenderName || 'User'}:</strong> ${c.parentContentPreview}
                </div>` : '';

            return `
            <div id="msg-${c.id}" style="display:flex; flex-direction:column; align-items: ${isMe ? 'flex-end' : 'flex-start'}; margin-bottom:0.2rem;">
                <span style="font-size:0.68rem; color:var(--text-tertiary); margin-bottom:2px;">${c.senderName} • ${new Date(c.sentAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}${isPinnedTag}</span>
                <div style="background: ${isMe ? 'var(--brand-primary)' : 'var(--bg-card)'}; color: ${isMe ? '#fff' : 'var(--text-primary)'}; padding: 0.7rem 1rem; border-radius: 18px; ${isMe ? 'border-bottom-right-radius:4px;' : 'border-bottom-left-radius:4px;'} max-width: 85%; font-size: 0.9rem; ${isDeletedStyle}">
                    ${replySnippet}
                    ${encIcon}${displayContent}
                </div>
                ${!c.isDeleted ? `
                <div style="display:flex; gap:8px; margin-top:2px; opacity:0.6;">
                    <span style="font-size:0.65rem; cursor:pointer;" onclick="setReplyTo('${c.id}', \`${displayContent.replace(/`/g,"'").substring(0,60)}\`, '${c.senderName}')">↩ Reply</span>
                    <span style="font-size:0.65rem; cursor:pointer;" onclick="pinMessage('${currentChatJobId}', '${c.id}')">${c.isPinned ? '📌 Unpin' : '📌 Pin'}</span>
                    ${isMe ? `<span style="font-size:0.65rem; cursor:pointer; color:var(--error);" onclick="deleteMessage('${currentChatJobId}', '${c.id}')">🗑</span>` : ''}
                </div>` : ''}
            </div>`;
        }));

        cbox.innerHTML = rows.join('');
        if(wasAtBottom) cbox.scrollTop = cbox.scrollHeight;
    } catch(e) {
        clearInterval(chatInterval);
    }
}

function scrollToMessage(msgId) {
    const el = document.getElementById(`msg-${msgId}`);
    if(el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

async function pinMessage(jobId, msgId) {
    try {
        const res = await apiCall('POST', `/Chats/${jobId}/messages/${msgId}/pin`);
        showToast(res.isPinned ? '📌 Message pinned' : 'Unpinned', 'success');
        loadChats();
    } catch(e) {}
}

async function deleteMessage(jobId, msgId) {
    if(!await UI.confirm('Soft-delete this message?')) return;
    try {
        await apiCall('DELETE', `/Chats/${jobId}/messages/${msgId}`);
        loadChats();
    } catch(e) {}
}

async function handleSendChat(e) {
    e.preventDefault();
    const inp = document.getElementById('chat-input');
    const val = inp.value.trim();
    if(!val || !currentChatJobId) return;

    let content = val;
    let isEncrypted = false;

    if(isE2EEnabled) {
        content = await E2E.encrypt(val, currentChatJobId);
        isEncrypted = true;
    }

    inp.value = '';
    const payload = { content, isEncrypted };
    if(replyingTo) payload.parentMessageId = replyingTo.id;
    clearReply();

    try {
        await apiCall('POST', `/Chats/${currentChatJobId}`, payload);
        loadChats();
    } catch(err) {
        inp.value = val;
    }
}

// ─── NOTIFICATION BELL ───
let notifInterval = null;

async function pollNotifications() {
    try {
        const res = await apiCall('GET', '/Notifications/unread-count');
        const count = res.count || 0;
        const badge = document.getElementById('notif-badge');
        if(badge) {
            badge.innerText = count > 0 ? count : '';
            badge.style.display = count > 0 ? 'flex' : 'none';
        }
    } catch(e) {}
}

async function openNotifications() {
    try {
        const notes = await apiCall('GET', '/Notifications');
        const panel = document.getElementById('notif-panel');
        if(!panel) return;
        
        panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
        if(panel.style.display === 'block') {
            panel.innerHTML = notes.length === 0 ? '<p style="padding:1rem; color:var(--text-secondary);">All caught up 🎉</p>' :
                '<div style="padding:0.5rem; display:flex; justify-content:flex-end;"><button class="btn btn-secondary btn-sm" onclick="markAllNotifsRead()">✓ Mark all read</button></div>' +
                notes.map(n => `
                    <div onclick="handleNotifClick('${n.referenceId}', '${n.referenceType}')" style="padding: 0.8rem 1rem; border-bottom: 1px solid var(--border-color); cursor:pointer; opacity:${n.isRead ? 0.6 : 1}; background:${n.isRead ? 'transparent' : 'rgba(0,122,255,0.05)'};">
                        <div style="font-weight:${n.isRead ? 400 : 700}; font-size:0.9rem;">${n.title}</div>
                        <div style="font-size:0.8rem; color:var(--text-secondary); margin-top:2px;">${n.body}</div>
                        <div style="font-size:0.7rem; color:var(--text-tertiary); margin-top:4px;">${new Date(n.createdAt).toLocaleString()}</div>
                    </div>`).join('');
            
            await apiCall('POST', '/Notifications/mark-all-read');
            pollNotifications();
        }
    } catch(e) {}
}

async function markAllNotifsRead() {
    try {
        await apiCall('POST', '/Notifications/mark-all-read');
        pollNotifications();
        openNotifications(); // refresh panel
    } catch(e) {}
}

function handleNotifClick(referenceId, referenceType) {
    if(referenceType === 'Job') {
        // navigate to activity
        appNavigation.showView('activity');
    }
    document.getElementById('notif-panel').style.display = 'none';
}



// ─── ESCROW FLOW ───
async function viewEscrow(bookingId) {
    if(!bookingId) return;
    try {
        const escrow = await apiCall('GET', `/Escrow/booking/${bookingId}`);
        State.currentEscrowView = escrow.id;
        renderQR(escrow.qrToken);
        openSheet('sheet-escrow-qr');
    } catch(e) {}
}

async function refreshQR() {
    try {
        const escrow = await apiCall('POST', `/Escrow/${State.currentEscrowView}/refresh-qr`);
        renderQR(escrow.qrToken);
        showToast('New Code Verified', 'success');
    } catch(e){}
}

function renderQR(token) {
    document.getElementById('escrow-qr-text').innerText = token.substring(0, 4) + " - " + token.substring(4);
    new QRious({ element: document.getElementById('qr-canvas'), value: token, size: 200, background: 'white', foreground: '#000' });
}

// ─── PROVIDER SCANNER ───
let html5QrcodeScanner = null;
async function openScanner(bookingId) {
    try {
        const escrow = await apiCall('GET', `/Escrow/booking/${bookingId}`);
        State.currentEscrowView = escrow.id;
        openSheet('sheet-scanner');
        html5QrcodeScanner = new Html5QrcodeScanner("reader", { fps: 10, qrbox: {width: 250, height: 250} }, false);
        html5QrcodeScanner.render((t) => {
            document.getElementById('manual-scan-code').value = t;
            submitScanCode();
        }, e => {});
    } catch(e) {}
}
function stopScanner() { if(html5QrcodeScanner) { html5QrcodeScanner.clear().catch(e => {}); html5QrcodeScanner = null; } }

async function submitScanCode() {
    const code = document.getElementById('manual-scan-code').value.replace(/\s+/g, '');
    if(!code || code.length !== 8) return showToast('Code must be 8 characters.', 'error');
    try {
        await apiCall('POST', `/Escrow/${State.currentEscrowView}/release`, { qrToken: code });
        showToast('FUNDS RELEASED COMPLETELY', 'success');
        closeAllSheets();
        fetchMyActivity();
    } catch(e) {}
}

// ==========================================
// 4. WALLET & TOPUP 
// ==========================================
async function fetchWallet() {
    try {
        const wallet = await apiCall('GET', '/Wallet');
        State.walletBalance = wallet.balance;
        document.getElementById('wallet-balance').innerText = `₦${wallet.balance.toLocaleString()}`;
        fetchLedger();
    } catch(e) {}
}

async function fetchLedger() {
    const listEl = document.getElementById('list-transactions');
    listEl.innerHTML = `<p style="text-align:center; color:var(--text-secondary); font-size:0.85rem;">Loading ledger...</p>`;
    try {
        const txns = await apiCall('GET', '/Wallet/transactions');
        if (!txns || txns.length === 0) {
            listEl.innerHTML = `<p style="text-align:center; color:var(--text-secondary);">No transactions yet. Top up to get started!</p>`;
            return;
        }
        const actionMeta = {
            'TOP_UP':                { label: 'Wallet Top-Up',       icon: '💳', color: 'var(--success)', sign: '+' },
            'ESCROW_SECURED':        { label: 'Escrow Lock',         icon: '🔒', color: 'var(--warning)', sign: '-' },
            'FULL_RELEASE':          { label: 'Payment Received',    icon: '💸', color: 'var(--success)', sign: '+' },
            'REMOTE_RELEASE':        { label: 'Remote Payment Out',  icon: '✅', color: 'var(--success)', sign: '-' },
            'REMAINING_FUNDS_SECURED':{ label: 'Final Escrow Phase', icon: '🔐', color: 'var(--warning)', sign: '-' },
            'CANCELLED':             { label: 'Escrow Refund',       icon: '↩️', color: 'var(--text-secondary)', sign: '+' },
            'DISPUTE_RAISED':        { label: 'Dispute Raised',      icon: '⚠️', color: 'var(--error)', sign: '' },
            'WITHDRAW':              { label: 'Withdrawal',          icon: '🏦', color: 'var(--error)', sign: '-' },
        };
        listEl.innerHTML = txns.map(t => {
            const meta = { ...actionMeta[t.action] } || { label: t.action, icon: '📋', color: 'var(--text-secondary)', sign: '' };
            if (t.action === 'FULL_RELEASE' && t.actorId !== State.userId) {
                // If the actor of a FULL_RELEASE isn't the logged-in user, they are receiving money from another's release
                meta.sign = '+';
            } else if (t.action === 'FULL_RELEASE' && t.actorId === State.userId) {
                // If they ARE the actor, they scanned the QR to receive money
                meta.sign = '+';
            }
            const amountMatch = t.details.match(/₦([\d,]+\.?\d*)/);
            const amountStr = amountMatch ? `${meta.sign}₦${amountMatch[1]}` : '';
            const dateStr = new Date(t.timestamp).toLocaleString('en-NG', { dateStyle:'medium', timeStyle:'short' });
            return `
            <div style="display:flex; align-items:center; gap:1rem; padding:0.9rem 0; border-bottom:1px solid var(--border-color);">
                <div style="font-size:1.5rem; width:36px; text-align:center;">${meta.icon}</div>
                <div style="flex:1;">
                    <div style="font-weight:600; font-size:0.9rem;">${meta.label}</div>
                    <div style="font-size:0.75rem; color:var(--text-secondary); margin-top:2px;">${t.details}</div>
                    <div style="font-size:0.7rem; color:var(--text-tertiary); margin-top:2px;">${dateStr}</div>
                </div>
                ${amountStr ? `<div style="font-weight:700; color:${meta.color}; white-space:nowrap;">${amountStr}</div>` : ''}
            </div>`;
        }).join('');
    } catch(e) {
        listEl.innerHTML = `<p style="text-align:center; color:var(--error);">Could not load ledger.</p>`;
    }
}

async function handleTopUp(e) {
    e.preventDefault();
    const val = Number(document.getElementById('topup-amount').value);
    if(val < 1000) return showToast("Min topup is 1000", "error");
    try {
        await apiCall('POST', '/Wallet/topup', { amount: val });
        showToast('Bank transfer simulated successfully.', 'success');
        closeAllSheets();
        fetchWallet();
    } catch(e) {}
}

async function handleWithdraw() {
    const val = await UI.prompt('Enter withdraw amount (requires KYC manually later):');
    if (!val || isNaN(val)) return;
    try {
        await apiCall('POST', '/Wallet/withdraw', { amount: Number(val) });
        showToast(`Withdrew ₦${val} cleanly. Check connected bank.`, 'success');
        fetchWallet();
    } catch(e) {}
}

// ==========================================
// 5. PROFILE & ADMIN
// ==========================================

async function viewPublicProfile(userId) {
    try {
        const u = await apiCall('GET', `/users/${userId}`);
        document.getElementById('pub-name').innerText = u.name;
        document.getElementById('pub-tier').innerText = u.tier;
        document.getElementById('pub-trust').innerText = `${u.trustScore} Trust`;
        document.getElementById('pub-bio').innerText = u.bio || "No bio provided.";
        document.getElementById('pub-completed').innerText = u.jobsCompleted || "0";
        document.getElementById('pub-avatar').innerText = (u.name||'U').charAt(0);
        
        const svcs = await apiCall('GET', `/Services/seller/${userId}`);
        renderServicesList(svcs, 'list-pub-services');

        appNavigation.showView('public-profile');
    } catch(e) {}
}

async function fetchProfile() {
    try {
        const u = await apiCall('GET', '/users/me');
        document.getElementById('profile-name').innerText = u.name || 'User';
        document.getElementById('profile-email').innerText = u.email;
        document.getElementById('profile-tier').innerText = u.tier || 'Basic';
        document.getElementById('profile-trust').innerText = u.trustScore || '0';
        document.getElementById('profile-avatar').innerText = (u.name||'U').charAt(0);
        
        const upg = document.getElementById('upgrade-card');
        upg.style.display = (u.tier === 'Basic') ? 'block' : 'none';

        // Unlock Admin capabilities if SuperAdmin/Admin
        const adminBtn = document.getElementById('btn-admin-portal');
        if (adminBtn) {
            const isAdmin = u.role === 'Admin' || u.role === 'SuperAdmin';
            if (isAdmin) {
                adminBtn.style.display = 'block';
            } else {
                adminBtn.style.display = 'none';
            }
        }
        
        fetchMyServices();

    } catch(e) {}
}

async function fetchMyServices() {
    try {
        const list = document.getElementById('list-my-services');
        if(!list) return;
        const res = await apiCall('GET', '/Services/my');
        if(!res || res.length === 0) {
            list.innerHTML = '<p>You have not hosted any services.</p>';
            return;
        }
        
        list.innerHTML = res.map(s => {
            const badge = s.isActive ? `<span class="badge bg-green">Visible</span>` : `<span class="badge bg-gray">Hidden</span>`;
            return `
            <div class="card col gap-2" style="margin-bottom:0;">
                <div class="row">
                    <h3 style="margin:0;">${s.title}</h3>
                    ${badge}
                </div>
                <p style="margin:0; font-size: 0.85rem; color: var(--text-secondary);">${s.description.substring(0,80)}...</p>
                <div class="row-start" style="margin-top:0.5rem; flex-wrap:wrap;">
                    <button class="btn btn-secondary btn-sm" onclick="editMyService('${s.id}')">✏️ Edit Base Rate</button>
                    ${!s.isActive ? `<button class="btn btn-primary btn-sm" onclick="activateMyService('${s.id}')">💡 Activate</button>` : ''}
                    <button class="btn btn-danger btn-sm" onclick="deleteMyService('${s.id}')">🗑️ Terminate</button>
                </div>
            </div>`;
        }).join('');
    } catch(e) {}
}

async function editMyService(id) {
    const newPrice = await UI.prompt("Enter new base price (Numbers only), or leave blank to cancel:");
    if(!newPrice || isNaN(newPrice)) return;
    try {
        await apiCall('PUT', `/Services/${id}`, { basePrice: Number(newPrice) });
        showToast("Service Rate updated successfully!", "success");
        fetchMyServices();
        fetchDiscover();
    } catch(e) {}
}

async function activateMyService(id) {
    try {
        await apiCall('POST', `/Services/${id}/activate`);
        showToast("Service is now live in Discover!", "success");
        fetchMyServices();
        fetchDiscover();
    } catch(e) {}
}

async function deleteMyService(id) {
    if(!await UI.confirm("Are you sure you want to completely tear down this service listing?")) return;
    try {
        await apiCall('DELETE', `/Services/${id}`);
        showToast("Service Terminated.", "success");
        fetchMyServices();
        fetchDiscover();
    } catch(e) {}
}

async function purchaseBadge() {
    if(await UI.confirm('This will instantly deduct ₦5000 from your Wallet via standard Escrow channels. Proceed?')) {
        try {
            await apiCall('POST', '/users/me/purchase-badge');
            showToast('Titanium Elite Badge acquired! Your rank is boosted.', 'success');
            fetchProfile();
        } catch(e) {}
    }
}

async function editProfilePrompt() {
    const newName = await UI.prompt("Enter your new display name:");
    if (!newName) return;
    try {
        await apiCall('PUT', '/users/me', { name: newName });
        showToast("Profile creatively updated!", "success");
        fetchProfile();
    } catch(e) {}
}

async function deleteAccountPrompt() {
    const val = await UI.prompt("Warning: You are about to permanently eradicate your account. Type 'CONFIRM' to proceed:");
    if(val !== 'CONFIRM') return;
    try {
        await apiCall('DELETE', '/users/me');
        await UI.alert("Account purged. We will miss you.");
        handleLogout();
    } catch(e) {}
}

// ==========================================
// 6. ADMIN CONSOLE
// ==========================================
let currentAdminMode = 'users';

async function filterAdmin(mode) {
    currentAdminMode = mode;
    ['users', 'disputes', 'jobs', 'payments'].forEach(m => {
        const btn = document.getElementById(`btn-adm-${m}`);
        if(btn) btn.classList.toggle('active', mode === m);
    });
    
    // Fetch global earnings in parallel
    apiCall('GET', '/Admin/platform-earnings').then(res => {
        document.getElementById('adm-earnings').innerText = `₦${(res.totalEarnings || res.TotalEarnings || res).toLocaleString()}`;
    }).catch(()=>{});

    const list = document.getElementById('list-admin-feed');
    list.innerHTML = '<p style="text-align:center;">Loading admin records...</p>';
    
    try {
        if (mode === 'users') {
            const res = await apiCall('GET', '/Admin/users?page=1&pageSize=50');
            const users = res.items || res; 
            if(!users || users.length === 0) return list.innerHTML = '<p>No users found.</p>';
            
            list.innerHTML = users.map(u => {
                const isAdmin = u.role === 'Admin' || u.role === 'SuperAdmin';
                return `
                <div class="card col gap-2" style="border-left: 4px solid var(--warning);">
                    <div class="row">
                        <h3 style="margin:0;">${u.name} <span class="badge bg-gray" style="font-size:0.7rem;">${u.role}</span></h3>
                        <div style="font-weight:700;">${u.tier}</div>
                    </div>
                    <p style="margin:0; font-size: 0.85rem; color:var(--text-secondary);">${u.email} | ID: ${u.id.substring(0,8)}</p>
                    <div class="row-start" style="margin-top:0.5rem;">
                        ${isAdmin 
                            ? `<button class="btn btn-danger btn-sm" onclick="adminSetRole('${u.id}', 'User')">Revoke Admin</button>` 
                            : `<button class="btn btn-secondary btn-sm" onclick="adminSetRole('${u.id}', 'Admin')">Make Admin</button>`}
                    </div>
                </div>`;
            }).join('');
        }
        else if (mode === 'disputes') {
            const disputes = await apiCall('GET', '/Admin/disputes');
            if(!disputes || disputes.length === 0) return list.innerHTML = '<p>No active disputes.</p>';
            
            list.innerHTML = disputes.map(d => `
            <div class="card col gap-2" style="border-left: 4px solid var(--error);">
                <div class="row">
                    <h3 style="margin:0;">Case: ${d.jobTitle}</h3>
                    <span class="badge bg-red">${d.status}</span>
                </div>
                <p style="margin:0; font-size: 0.85rem; color:var(--text-secondary);">Raised At: ${new Date(d.raisedAt).toLocaleString()}</p>
                <p style="margin:0; font-size: 0.85rem;"><strong>Complaint:</strong> ${d.reason}</p>
                <div class="row-start" style="margin-top:0.5rem;">
                    <button class="btn btn-primary btn-sm" onclick="adminResolveDispute('${d.id}')">Issue Resolution Ruling</button>
                </div>
            </div>`).join('');
        }
        else if (mode === 'jobs') {
            const jobs = await apiCall('GET', '/Admin/jobs');
            if(!jobs || jobs.length === 0) return list.innerHTML = '<p>No jobs found.</p>';
            list.innerHTML = jobs.slice(0, 50).map(j => `
            <div class="card col gap-2">
                <div class="row"><h3 style="margin:0;">${j.title}</h3><span class="badge bg-blue">${j.status}</span></div>
                <p style="margin:0; font-size: 0.85rem;">Escrow: ₦${j.amount}</p>
                <div class="row-start" style="margin-top:0.5rem;">
                    <button class="btn btn-secondary btn-sm" onclick="adminShowTimeline('${j.id}')">View Audit Timeline</button>
                </div>
            </div>`).join('');
        }
        else if (mode === 'payments') {
            const pay = await apiCall('GET', '/Admin/payments');
            if(!pay || pay.length === 0) return list.innerHTML = '<p>No payments recorded.</p>';
            list.innerHTML = pay.map(p => `
            <div class="card col gap-2">
                <div class="row"><h3 style="margin:0;">Payment Focus</h3><span class="badge bg-green">Secured</span></div>
                <p style="margin:0; font-size: 0.85rem;">Amount Captured: ₦${p.amount}</p>
                <div class="row-start" style="margin-top:0.5rem;">
                    <button class="btn btn-danger btn-sm" onclick="adminRefundPayment('${p.id}')">Force System Refund</button>
                </div>
            </div>`).join('');
        }
    } catch(e) {}
}

async function adminRefundPayment(paymentId) {
    if(!await UI.confirm("Are you sure you want to forcibly refund this transaction directly from the central reserve?")) return;
    try {
        await apiCall('POST', `/Admin/payments/${paymentId}/refund`);
        showToast('Refund executed natively.', 'success');
        filterAdmin('payments');
    } catch(e) {}
}

async function adminShowTimeline(jobId) {
    try {
        const logs = await apiCall('GET', `/Admin/jobs/${jobId}/timeline`);
        if(!logs || logs.length === 0) return showToast('No timeline events yet.', 'warning');
        const txt = logs.map(l => `[${new Date(l.timestamp).toLocaleTimeString()}] ${l.action} - ${l.details}`).join('\n');
        await UI.alert("Escrow Audit Trail:\n\n" + txt);
    } catch(e) {}
}

async function adminSetRole(userId, newRole) {
    if(!await UI.confirm(`Are you sure you want to alter permissions to ${newRole}? (Requires SuperAdmin)`)) return;
    try {
        await apiCall('PUT', `/Admin/users/${userId}/role`, { role: newRole });
        showToast(`User ${userId.substring(0,6)} promoted to ${newRole}`, 'success');
        filterAdmin('users');
    } catch(e) {}
}

async function adminResolveDispute(disputeId) {
    const ruling = await UI.prompt("Enter official binding resolution note (e.g., 'Refunding Buyer'):");
    if(!ruling) return;
    try {
        await apiCall('PUT', `/Admin/disputes/${disputeId}/resolve`, { resolution: ruling });
        showToast('Dispute permanently resolved.', 'success');
        filterAdmin('disputes');
    } catch(e) {}
}

// ─── NEW FEATURES WAITING FOR IMPLEMENTATION ───
async function fetchServicesByCategory(catId, catName) {
    try {
        const svcs = await apiCall('GET', `/Services/categories/${catId}/services`);
        renderServicesList(svcs.slice().reverse(), 'list-services');
        document.getElementById('list-services').insertAdjacentHTML('afterbegin', `<h3 style="margin-left:5px; margin-bottom:15px; color:var(--brand-primary);">${catName} Services</h3>`);
    } catch(e) {}
}

async function rejectBooking(bookingId) {
    if(!await UI.confirm("Reject this application completely?")) return;
    try {
        await apiCall('PUT', `/Bookings/${bookingId}/status`, { newStatus: 'Cancelled' });
        showToast("Application rejected.", "success");
        fetchMyActivity();
    } catch(e) {}
}

async function withdrawApplication(bookingId) {
    if(!await UI.confirm("Retract your application for this job?")) return;
    try {
        await apiCall('DELETE', `/Bookings/${bookingId}`);
        showToast("Application withdrawn.", "success");
        fetchMyActivity();
    } catch(e) {}
}

async function joinWaitlist(serviceId, serviceTitle) {
    if(!await UI.confirm(`Join the waitlist for ${serviceTitle}? You will be notified when space opens up.`)) return;
    try {
        await apiCall('POST', `/Waitlists/${serviceId}`);
        showToast("Successfully added to waitlist!", "success");
    } catch(e) {}
}

async function fetchMyWaitlist() {
    try {
        const wl = await apiCall('GET', `/Waitlists/my`);
        const list = document.getElementById('list-my-waitlist');
        if (!list) return;
        if (!wl || wl.length === 0) {
            list.innerHTML = '<p>You are not on any waitlists.</p>';
            return;
        }
        list.innerHTML = wl.map(w => `
            <div class="card col gap-2">
                <div class="row"><h3>${w.serviceTitle}</h3><span class="badge bg-orange">${w.status}</span></div>
                <p>Date Joined: ${new Date(w.createdAt).toLocaleString()}</p>
            </div>
        `).join('');
    } catch (e) {}
}

async function cancelEscrow() {
    if(!State.currentEscrowView) return;
    if(!await UI.confirm("Cancel Escrow? The funds will be instantly refunded to your wallet and the provider will be notified. Proceed?")) return;
    try {
        await apiCall('POST', `/Escrow/${State.currentEscrowView}/cancel`);
        showToast("Escrow Cancelled and refunded.", "success");
        closeAllSheets();
        fetchMyActivity();
    } catch(e) {} // Endpoint automatically falls back to actor tracking internally
}

async function viewAuditTrail() {
    if(!State.currentEscrowView) return;
    try {
        const logs = await apiCall('GET', `/Escrow/${State.currentEscrowView}/audit`);
        if(!logs || logs.length === 0) return showToast('No audit trail available.', 'warning');
        const txt = logs.map(l => `[${new Date(l.timestamp).toLocaleTimeString()}] ${l.action} - ${l.details}`).join('\n');
        await UI.alert("Immutable Escrow Audit Trail:\n\n" + txt);
    } catch(e) {}
}

// ─── INIT ROUTER ───
window.addEventListener('DOMContentLoaded', () => {
    if (State.token) {
        appNavigation.showView('discover');
    } else {
        appNavigation.showView('landing');
    }
});
