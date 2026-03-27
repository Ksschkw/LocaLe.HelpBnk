const API_URL = "http://localhost:5012/api";

// State
let currentUser = null;
let currentToken = null;

// DOM Elements
const authSection = document.getElementById("auth-section");
const escrowSection = document.getElementById("escrow-section");
const userInfoEl = document.getElementById("user-info");
const walletInfoEl = document.getElementById("wallet-info");
const logoutBtn = document.getElementById("logout-btn");
const loginForm = document.getElementById("login-form");
const registerForm = document.getElementById("register-form");
const searchBtn = document.getElementById("search-btn");
const loadCatsBtn = document.getElementById("load-cats-btn");
const topupBtn = document.getElementById("topup-btn");
const loadJobsBtn = document.getElementById("load-jobs-btn");
const loadBookingsBtn = document.getElementById("load-bookings-btn");
const postJobBtn = document.getElementById("post-job-btn");

// Initialization
updateUIState();

// Helper for API calls
async function apiCall(endpoint, method = "GET", body = null) {
    const headers = { "Content-Type": "application/json" };
    if (currentToken) headers["Authorization"] = `Bearer ${currentToken}`;
    
    const options = { method, headers };
    if (body) options.body = JSON.stringify(body);

    try {
        const response = await fetch(`${API_URL}${endpoint}`, options);
        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.Error || err.title || "API request failed");
        }
        if (response.status === 204) return true;
        return await response.json();
    } catch (e) {
        alert(e.message);
        throw e;
    }
}

// UI State Management
function updateUIState() {
    if (currentToken) {
        authSection.style.display = "none";
        escrowSection.style.display = "block";
        userInfoEl.textContent = `Logged in as: ${currentUser.name} (${currentUser.role})`;
        logoutBtn.style.display = "inline-block";
        fetchWallet();
    } else {
        authSection.style.display = "block";
        escrowSection.style.display = "none";
        userInfoEl.textContent = "Not logged in";
        walletInfoEl.textContent = "";
        logoutBtn.style.display = "none";
    }
}

// --- AUTHENTICATION ---
loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const email = document.getElementById("login-email").value;
    const password = document.getElementById("login-password").value;
    
    const data = await apiCall("/auth/login", "POST", { email, password, rememberMe: true });
    currentToken = data.token;
    currentUser = data;
    updateUIState();
});

registerForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const name = document.getElementById("reg-name").value;
    const email = document.getElementById("reg-email").value;
    const password = document.getElementById("reg-password").value;
    
    const data = await apiCall("/auth/register", "POST", { name, email, password });
    currentToken = data.token;
    currentUser = data;
    updateUIState();
});

logoutBtn.addEventListener("click", () => {
    currentToken = null;
    currentUser = null;
    updateUIState();
});

// --- WALLET ---
async function fetchWallet() {
    if (!currentToken) return;
    try {
        const wallet = await apiCall("/wallet");
        walletInfoEl.textContent = `| Balance: ₦${wallet.balance.toLocaleString()}`;
    } catch {}
}

topupBtn.addEventListener("click", async () => {
    const amount = parseFloat(document.getElementById("topup-amount").value);
    if (!amount) return;
    await apiCall("/wallet/topup", "POST", { amount });
    fetchWallet();
    alert(`Topped up ₦${amount}`);
});

// --- CATALOG ---
loadCatsBtn.addEventListener("click", async () => {
    const cats = await apiCall("/catalog/categories");
    const container = document.getElementById("categories-container");
    container.innerHTML = cats.map(c => `
        <div class="card">
            <h3>${c.iconUrl || ''} ${c.name}</h3>
            <p>${c.description || ''}</p>
        </div>`).join("");
});

searchBtn.addEventListener("click", async () => {
    const query = document.getElementById("search-input").value;
    const services = await apiCall(`/catalog/services?search=${encodeURIComponent(query)}`);
    const container = document.getElementById("services-container");
    
    if(services.length === 0) {
        container.innerHTML = "<p>No services found</p>";
        return;
    }

    container.innerHTML = services.map(s => `
        <div class="card">
            <h3>${s.title}</h3>
            <p>${s.categoryName}</p>
            <p>${s.description}</p>
            <p class="price">₦${s.basePrice}</p>
            <p><small>Provider: ${s.providerName}</small></p>
            <button onclick="applyToService(${s.id})">Book Service (Mock)</button>
        </div>`).join("");
});

// --- JOBS & ESCROW FLOW ---
postJobBtn.addEventListener("click", async () => {
    const title = document.getElementById("job-title").value;
    const description = document.getElementById("job-desc").value;
    const amount = parseFloat(document.getElementById("job-amount").value);
    
    if(!title || !amount) return alert("Title and amount required.");
    
    await apiCall("/jobs", "POST", { title, description, amount });
    alert("Job posted!");
    loadJobs();
});

loadJobsBtn.addEventListener("click", loadJobs);

async function loadJobs() {
    const jobs = await apiCall("/jobs");
    const container = document.getElementById("jobs-list");
    container.innerHTML = jobs.map(j => `
        <div class="list-item">
            <div>
                <strong>${j.title}</strong> <span class="badge">${j.status}</span><br>
                <small>${j.description}</small><br>
                <small>By: ${j.creatorName}</small>
            </div>
            <div>
                <span class="price">₦${j.amount}</span>
                ${j.status === 'Open' && j.creatorId !== currentUser?.userId ? 
                  `<button onclick="applyToJob(${j.id})">Apply</button>` : ''}
            </div>
        </div>`).join("");
}

window.applyToJob = async (id) => {
    await apiCall(`/bookings/apply/${id}`, "POST");
    alert("Applied successfully!");
    loadBookings();
};

loadBookingsBtn.addEventListener("click", loadBookings);

async function loadBookings() {
    const bookings = await apiCall("/bookings/my");
    const container = document.getElementById("bookings-list");
    
    container.innerHTML = await Promise.all(bookings.map(async b => {
        let actionHtml = '';
        
        if (b.status === "Pending" && b.providerId !== currentUser?.userId) {
            // I am the buyer, provider applied. I can confirm this booking.
            actionHtml = `<button onclick="confirmBooking(${b.id})">Confirm & Lock Escrow</button>`;
        } else if (b.status === "Finalized" || b.status === "Active") {
            // Fetch escrow details to check QR Status
            const escrow = await getEscrowDetails(b.id);
            if(escrow) {
                if(escrow.status === "Secured") {
                     if(b.providerId === currentUser?.userId) {
                         // I am the provider, I need to scan QR to release funds
                         actionHtml = `<button onclick="releaseEscrow(${escrow.id})">Provider: Release Funds</button>`;
                     } else {
                         // I am the buyer, show the QR Code
                         actionHtml = `<strong>[QR CODE TOKEN: ${escrow.qrToken}]</strong> (Show to Provider)`;
                     }
                } else if (escrow.status === "Released") {
                     actionHtml = `<span style="color:green;font-weight:bold;">Funds Released</span>`;
                }
            }
        }
        
        return `
        <div class="list-item">
            <div>
                <strong>${b.jobTitle}</strong> <span class="badge">${b.status}</span><br>
                <small>Provider: ${b.providerName}</small>
            </div>
            <div>${actionHtml}</div>
        </div>`;
    })).then(res => res.join(""));
}

window.confirmBooking = async (id) => {
    await apiCall(`/bookings/${id}/confirm`, "POST");
    alert("Escrow Locked Successfully!");
    fetchWallet(); // Wallet deducted
    loadBookings(); // Refresh view
};

async function getEscrowDetails(bookingId) {
    try {
        return await apiCall(`/escrow/booking/${bookingId}`);
    } catch {
        return null;
    }
}

window.releaseEscrow = async (escrowId) => {
    const token = prompt("Enter the 8-character QR Token shown on the Buyer's phone:");
    if (!token) return;
    
    await apiCall(`/escrow/${escrowId}/release`, "POST", { qrToken: token.toUpperCase() });
    alert("Funds Released to your Wallet!");
    fetchWallet();
    loadBookings();
};
