/* ============================================
   LocaLe Escrow Flow Prototype — App Logic
   ============================================ */

// --- Mock Data ---
const MOCK_PROVIDERS = [
  { id: 1, name: 'Chidi Okeke', initials: 'CO', distance: '0.3 km', rating: 4.8, reviews: 23, vouch: 'Emeka A.', specialty: 'Fast turnaround' },
  { id: 2, name: 'Amara Eze', initials: 'AE', distance: '0.7 km', rating: 4.6, reviews: 15, vouch: 'Kelechi N.', specialty: 'Highly rated' },
  { id: 3, name: 'Tunde Balogun', initials: 'TB', distance: '1.2 km', rating: 4.9, reviews: 41, vouch: 'Ifeanyi O.', specialty: 'Top rated' }
];

const SERVICES = [
  { id: 'laundry', name: 'Laundry / Wash & Iron', icon: '👕', desc: 'Pickup, wash, iron & deliver', priceRange: '₦2,000 – ₦5,000' },
  { id: 'cleaning', name: 'Room Cleaning', icon: '🧹', desc: 'Deep clean, mopping & arranging', priceRange: '₦3,000 – ₦8,000' },
  { id: 'errand', name: 'Errand / Pickup & Delivery', icon: '📦', desc: 'Buy, pick up or deliver items', priceRange: '₦500 – ₦3,000' }
];

const DEMO_QR_CODE = '482915';
const ESCROW_REF = 'LCL-' + String(Math.floor(Math.random() * 9000) + 1000);

// --- Session State ---
function getState() {
  try {
    return JSON.parse(sessionStorage.getItem('locale_state') || '{}');
  } catch { return {}; }
}

function setState(updates) {
  const state = { ...getState(), ...updates };
  sessionStorage.setItem('locale_state', JSON.stringify(state));
  return state;
}

function initState() {
  if (!getState().escrowRef) {
    setState({
      escrowRef: ESCROW_REF,
      qrCode: DEMO_QR_CODE,
      selectedService: null,
      selectedPrice: null,
      selectedProvider: null,
      paymentMethod: 'transfer',
      timestamp: new Date().toISOString()
    });
  }
}

// --- Navigation ---
function navigateTo(page) {
  window.location.href = page;
}

// --- Price Selection ---
function selectPrice(amount, element) {
  document.querySelectorAll('.price-option').forEach(el => el.classList.remove('active'));
  element.classList.add('active');
  setState({ selectedPrice: amount });
  
  // Update continue button if disabled
  const continueBtn = document.querySelector('.btn--primary');
  if (continueBtn) {
    continueBtn.style.opacity = '1';
    continueBtn.style.pointerEvents = 'auto';
  }
}

// --- Payment Selection ---
function selectPayment(method, element) {
  document.querySelectorAll('.payment-option').forEach(el => el.classList.remove('active'));
  element.classList.add('active');
  setState({ paymentMethod: method });
  
  // Show/hide transfer details
  const transferDetails = document.getElementById('transferDetails');
  if (transferDetails) {
    transferDetails.style.display = method === 'transfer' ? 'block' : 'none';
  }
}

// --- Provider Selection ---
function selectProvider(id, element) {
  document.querySelectorAll('.card').forEach(el => {
    el.classList.remove('selected');
    el.style.borderColor = '';
  });
  element.classList.add('selected');
  element.style.borderColor = 'var(--green-primary)';
  
  const provider = MOCK_PROVIDERS.find(p => p.id === id);
  setState({ selectedProvider: provider ? provider.name : 'Provider' });
  
  const continueBtn = document.querySelector('.btn--primary');
  if (continueBtn) {
    continueBtn.style.opacity = '1';
    continueBtn.style.pointerEvents = 'auto';
  }
}

// --- QR Code Generation (simple SVG placeholder) ---
function generateQRCode(containerId, code) {
  const container = document.getElementById(containerId);
  if (!container) return;
  
  // Generate a simple QR-like pattern as SVG
  const size = 180;
  const cellSize = 6;
  const cells = Math.floor(size / cellSize);
  
  // Use the code to seed a pattern
  let seed = 0;
  for (let i = 0; i < code.length; i++) {
    seed += code.charCodeAt(i);
  }
  
  let svgContent = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">`;
  svgContent += `<rect width="${size}" height="${size}" fill="white"/>`;
  
  // QR finder patterns (the three big squares)
  const drawFinder = (x, y) => {
    const s = cellSize;
    svgContent += `<rect x="${x}" y="${y}" width="${7*s}" height="${7*s}" fill="black"/>`;
    svgContent += `<rect x="${x+s}" y="${y+s}" width="${5*s}" height="${5*s}" fill="white"/>`;
    svgContent += `<rect x="${x+2*s}" y="${y+2*s}" width="${3*s}" height="${3*s}" fill="black"/>`;
  };
  
  drawFinder(0, 0);
  drawFinder((cells - 7) * cellSize, 0);
  drawFinder(0, (cells - 7) * cellSize);
  
  // Generate pseudo-random data pattern
  const rng = (n) => {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff;
    return seed % n;
  };
  
  for (let y = 0; y < cells; y++) {
    for (let x = 0; x < cells; x++) {
      // Skip finder pattern areas
      if ((x < 8 && y < 8) || (x >= cells - 8 && y < 8) || (x < 8 && y >= cells - 8)) continue;
      
      if (rng(3) === 0) {
        svgContent += `<rect x="${x * cellSize}" y="${y * cellSize}" width="${cellSize}" height="${cellSize}" fill="black"/>`;
      }
    }
  }
  
  svgContent += '</svg>';
  container.innerHTML = svgContent;
}

// --- Code Verification ---
function verifyCode() {
  const input = document.getElementById('codeInput');
  const errorEl = document.getElementById('codeError');
  const successEl = document.getElementById('codeSuccess');
  
  if (!input) return;
  
  const enteredCode = input.value.trim();
  const state = getState();
  
  if (enteredCode === state.qrCode || enteredCode === DEMO_QR_CODE) {
    if (errorEl) errorEl.style.display = 'none';
    if (successEl) {
      successEl.style.display = 'block';
      successEl.textContent = '✓ Code verified successfully!';
    }
    input.style.borderColor = 'var(--green-primary)';
    
    setTimeout(() => navigateTo('receipt.html'), 1200);
  } else {
    if (successEl) successEl.style.display = 'none';
    if (errorEl) {
      errorEl.style.display = 'block';
      errorEl.textContent = '✗ Invalid code. Please try again.';
    }
    input.style.borderColor = 'var(--red-primary)';
    input.classList.add('shake');
    setTimeout(() => input.classList.remove('shake'), 500);
  }
}

// --- Checkbox Toggle for confirm page ---
function toggleConfirm() {
  const checkbox = document.getElementById('confirmCheck');
  const btn = document.getElementById('releaseBtn');
  if (checkbox && btn) {
    btn.style.opacity = checkbox.checked ? '1' : '0.5';
    btn.style.pointerEvents = checkbox.checked ? 'auto' : 'none';
  }
}

// --- Close A2HS Banner ---
function closeBanner(el) {
  const banner = el.closest('.a2hs-banner');
  if (banner) {
    banner.style.animation = 'none';
    banner.style.transition = 'all 0.3s ease';
    banner.style.opacity = '0';
    banner.style.transform = 'translateY(-12px)';
    setTimeout(() => banner.style.display = 'none', 300);
  }
}

// --- Copy to clipboard ---
function copyText(text, el) {
  navigator.clipboard.writeText(text).then(() => {
    const original = el.textContent;
    el.textContent = 'Copied!';
    el.style.color = 'var(--green-primary)';
    setTimeout(() => {
      el.textContent = original;
      el.style.color = '';
    }, 1500);
  }).catch(() => {
    // fallback
    const temp = document.createElement('textarea');
    temp.value = text;
    document.body.appendChild(temp);
    temp.select();
    document.execCommand('copy');
    document.body.removeChild(temp);
  });
}

// --- Populate dynamic content ---
function populatePage() {
  const state = getState();
  
  // Provider name
  document.querySelectorAll('[data-bind="providerName"]').forEach(el => {
    el.textContent = state.selectedProvider || 'Chidi Okeke';
  });
  
  // Price
  document.querySelectorAll('[data-bind="price"]').forEach(el => {
    el.textContent = state.selectedPrice ? `₦${Number(state.selectedPrice).toLocaleString()}` : '₦5,000';
  });
  
  // Escrow ref
  document.querySelectorAll('[data-bind="escrowRef"]').forEach(el => {
    el.textContent = state.escrowRef || ESCROW_REF;
  });
  
  // QR Code
  document.querySelectorAll('[data-bind="qrCode"]').forEach(el => {
    el.textContent = state.qrCode || DEMO_QR_CODE;
  });
  
  // Fee (₦100 flat)
  document.querySelectorAll('[data-bind="fee"]').forEach(el => {
    el.textContent = '₦100';
  });
  
  // Total  
  document.querySelectorAll('[data-bind="total"]').forEach(el => {
    const price = state.selectedPrice || 5000;
    el.textContent = `₦${(Number(price) + 100).toLocaleString()}`;
  });

  // Time
  document.querySelectorAll('[data-bind="time"]').forEach(el => {
    el.textContent = new Date().toLocaleTimeString('en-NG', { hour: '2-digit', minute: '2-digit' });
  });

  // Date
  document.querySelectorAll('[data-bind="date"]').forEach(el => {
    el.textContent = new Date().toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: 'numeric' });
  });
}

// --- Service Selection ---
function selectService(serviceId) {
  setState({ selectedService: serviceId });
  navigateTo('job.html');
}

// --- Init ---
document.addEventListener('DOMContentLoaded', () => {
  initState();
  populatePage();
  
  // Generate QR where needed
  const qrContainer = document.getElementById('qrCode');
  if (qrContainer) {
    generateQRCode('qrCode', getState().qrCode || DEMO_QR_CODE);
  }
});

// --- Shake animation (injected) ---
const shakeStyle = document.createElement('style');
shakeStyle.textContent = `
  .shake {
    animation: shake 0.4s ease;
  }
  @keyframes shake {
    0%, 100% { transform: translateX(0); }
    25% { transform: translateX(-8px); }
    75% { transform: translateX(8px); }
  }
`;
document.head.appendChild(shakeStyle);
