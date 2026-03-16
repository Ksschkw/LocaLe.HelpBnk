/* ================================================
   LocaLe Landing Page — JavaScript
   Features:
   - Navbar scroll effect
   - Modal open/close
   - UTM parameter tracking
   - Browser Geolocation + IP fallback
   - Google Sheets form submission (no backend)
   ================================================ */

// ================================================
// 🔧 CONFIGURATION — FILL IN AFTER SETUP
// ================================================

// 1. Deploy a Google Apps Script Web App and paste the URL here.
//    Instructions are in SETUP.md.
const GOOGLE_SCRIPT_URL = 'https://script.google.com/macros/s/AKfycbwGwyr_63LfB5gnVM0eryVnK5FHxKg_H64YvFMo9bHQdJFfA45cbALt0nVWUswJuENlTg/exec';

// 2. Your WhatsApp number (with country code, no + or spaces)
const WHATSAPP_NUMBER = '2349019549473';

// ================================================
// NAVBAR SCROLL
// ================================================
const nav = document.getElementById('nav');
window.addEventListener('scroll', () => {
  nav.classList.toggle('scrolled', window.scrollY > 60);
});

// ================================================
// MODAL
// ================================================
const overlay = document.getElementById('modalOverlay');
let currentOpenSource = '';

function openModal(source) {
  currentOpenSource = source;
  document.getElementById('openedFrom').value = source;
  overlay.classList.add('open');
  document.body.style.overflow = 'hidden';
  captureLocation();
}

function closeModal(event) {
  if (event && event.target !== overlay) return;
  overlay.classList.remove('open');
  document.body.style.overflow = '';
}

// Close on Escape key
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') {
    overlay.classList.remove('open');
    document.body.style.overflow = '';
  }
});

// ================================================
// UTM TRACKING
// ================================================
function parseUTM() {
  const params = new URLSearchParams(window.location.search);
  const source = params.get('utm_source') || params.get('ref') || detectSource();
  const medium = params.get('utm_medium') || '';
  const campaign = params.get('utm_campaign') || '';

  document.getElementById('utmSource').value = source;
  document.getElementById('utmMedium').value = medium;
  document.getElementById('utmCampaign').value = campaign;
}

function detectSource() {
  const ref = document.referrer;
  if (!ref) return 'direct';
  if (ref.includes('whatsapp')) return 'whatsapp';
  if (ref.includes('google')) return 'google';
  if (ref.includes('facebook') || ref.includes('fb.')) return 'facebook';
  if (ref.includes('instagram')) return 'instagram';
  if (ref.includes('twitter') || ref.includes('t.co')) return 'twitter';
  return 'referral:' + new URL(ref).hostname;
}

// ================================================
// GEOLOCATION
// ================================================
let locationData = 'Not captured';

async function captureLocation() {
  // Only try once
  if (document.getElementById('locationField').value) return;

  // Option 1: Browser GPS (precise, but requires user permission)
  if ('geolocation' in navigator) {
    try {
      const pos = await new Promise((resolve, reject) =>
        navigator.geolocation.getCurrentPosition(resolve, reject, {
          timeout: 4000,
          maximumAge: 30000
        })
      );
      const lat = pos.coords.latitude.toFixed(5);
      const lon = pos.coords.longitude.toFixed(5);
      locationData = `GPS: ${lat},${lon}`;
      document.getElementById('locationField').value = locationData;
      return;
    } catch (_) {
      // Permission denied or not available — fall through to IP lookup
    }
  }

  // Option 2: IP-based geolocation (no permission needed, less precise)
  try {
    const res = await fetch('https://ipapi.co/json/', { signal: AbortSignal.timeout(4000) });
    if (res.ok) {
      const data = await res.json();
      locationData = [
        data.city,
        data.region,
        data.country_name,
        `(IP: ${data.ip})`
      ].filter(Boolean).join(', ');
      document.getElementById('locationField').value = locationData;
    }
  } catch (_) {
    document.getElementById('locationField').value = 'Could not capture location';
  }
}

// ================================================
// FORM SUBMISSION → GOOGLE SHEETS
// ================================================
async function submitForm(event) {
  event.preventDefault();

  const form = document.getElementById('waitlistForm');
  const submitBtn = document.getElementById('submitBtn');
  const submitLabel = document.getElementById('submitLabel');
  const submitLoader = document.getElementById('submitLoader');

  // Set timestamp
  document.getElementById('signupTimestamp').value = new Date().toLocaleString('en-NG', {
    timeZone: 'Africa/Lagos',
    dateStyle: 'medium',
    timeStyle: 'short'
  });

  // Show loading state
  submitLabel.style.display = 'none';
  submitLoader.style.display = 'inline';
  submitBtn.disabled = true;

  // Collect form data
  const data = {};
  const formData = new FormData(form);
  formData.forEach((value, key) => { data[key] = value; });

  // Check if script URL is configured
  if (!GOOGLE_SCRIPT_URL || GOOGLE_SCRIPT_URL === 'YOUR_GOOGLE_SCRIPT_URL_HERE') {
    // Dev mode: just log and show success
    console.log('📋 Form data (dev mode — script not configured):', data);
    showSuccess();
    return;
  }

  try {
    // Send to Google Apps Script
    await fetch(GOOGLE_SCRIPT_URL, {
      method: 'POST',
      mode: 'no-cors', // required for Apps Script
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    // With no-cors we can't read the response, but if there's no network error
    // we assume success (Google Apps Script always returns 200 for valid requests)
    showSuccess();

  } catch (err) {
    console.error('Submission error:', err);
    // Restore button so user can retry
    submitLabel.style.display = 'inline';
    submitLoader.style.display = 'none';
    submitBtn.disabled = false;
    alert('Something went wrong. Please try again or contact us on WhatsApp.');
  }
}

function showSuccess() {
  document.getElementById('waitlistForm').style.display = 'none';
  document.getElementById('modalSuccess').style.display = 'block';
}

// ================================================
// SHARE ON WHATSAPP
// ================================================
function shareOnWhatsApp() {
  const text = encodeURIComponent(
    "I just joined the LocaLe pilot — an escrow service for neighborhood jobs in FUTO area 🔥\n\n" +
    "No more getting scammed. Money is held until the job is done with QR verification.\n\n" +
    "Join here: " + window.location.href
  );
  window.open(`https://wa.me/?text=${text}`, '_blank');
}

// ================================================
// SMOOTH ANCHOR SCROLLING
// ================================================
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
  anchor.addEventListener('click', function (e) {
    e.preventDefault();
    const target = document.querySelector(this.getAttribute('href'));
    if (target) {
      const offset = 80; // nav height
      window.scrollTo({
        top: target.getBoundingClientRect().top + window.pageYOffset - offset,
        behavior: 'smooth'
      });
    }
  });
});

// ================================================
// INTERSECTION OBSERVER (Scroll animations)
// ================================================
const observer = new IntersectionObserver(
  (entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.style.opacity = '1';
        entry.target.style.transform = 'translateY(0)';
      }
    });
  },
  { threshold: 0.1 }
);

document.querySelectorAll('.step, .service-card, .pricing-card, .zone, .quote-card, .research-stat').forEach((el) => {
  el.style.opacity = '0';
  el.style.transform = 'translateY(24px)';
  el.style.transition = 'opacity 0.5s ease, transform 0.5s cubic-bezier(0.16, 1, 0.3, 1)';
  observer.observe(el);
});

// ================================================
// INIT
// ================================================
document.addEventListener('DOMContentLoaded', () => {
  parseUTM();
});
