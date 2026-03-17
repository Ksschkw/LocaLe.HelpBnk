/* ================================================
   LocaLe Landing Page — JavaScript
   Features:
   - Navbar scroll effect
   - Modal open/close
   - UTM parameter tracking
   - Passive tracking: device, OS, browser, network, scroll, time-on-page, session
   - Browser Geolocation + IP fallback
   - Google Sheets form submission (no backend)
   ================================================ */

// ================================================
// 🔧 CONFIGURATION
// ================================================
const GOOGLE_SCRIPT_URL = 'https://script.google.com/macros/s/AKfycbwGwyr_63LfB5gnVM0eryVnK5FHxKg_H64YvFMo9bHQdJFfA45cbALt0nVWUswJuENlTg/exec';
const WHATSAPP_NUMBER = '2349019549473';

// ================================================
// PAGE LOAD TIME (for "time on page before signup")
// ================================================
const PAGE_LOAD_TIME = Date.now();

// ================================================
// PASSIVE TRACKING — collected silently
// ================================================

/** Session ID: stored in localStorage so repeat visitors are identified */
function getOrCreateSessionId() {
  let sid = localStorage.getItem('locale_sid');
  if (!sid) {
    sid = 'SID-' + Math.random().toString(36).substr(2, 10).toUpperCase() + '-' + Date.now();
    localStorage.setItem('locale_sid', sid);
  }
  return sid;
}

/** First vs returning visitor */
function getVisitorStatus() {
  const visitCount = parseInt(localStorage.getItem('locale_visits') || '0') + 1;
  localStorage.setItem('locale_visits', visitCount);
  return visitCount === 1 ? 'new' : `returning (visit #${visitCount})`;
}

/** Parse User-Agent into a readable string: Device · OS · Browser */
function parseUserAgent() {
  const ua = navigator.userAgent;
  let device = 'Desktop';
  let os = 'Unknown OS';
  let browser = 'Unknown Browser';

  // Device
  if (/Mobi|Android|iPhone|iPad|iPod/i.test(ua)) {
    device = /iPad/i.test(ua) ? 'Tablet' : 'Mobile';
  }

  // OS
  if (/Android/i.test(ua)) {
    os = 'Android ' + (ua.match(/Android\s([\d.]+)/) || ['',''])[1];
  } else if (/iPhone|iPad|iPod/i.test(ua)) {
    os = 'iOS ' + (ua.match(/OS\s([\d_]+)/) || ['',''])[1].replace(/_/g, '.');
  } else if (/Windows NT/i.test(ua)) {
    os = 'Windows ' + (ua.match(/Windows NT\s([\d.]+)/) || ['',''])[1];
  } else if (/Mac OS X/i.test(ua)) {
    os = 'macOS';
  } else if (/Linux/i.test(ua)) {
    os = 'Linux';
  }

  // Browser
  if (/Edg\//i.test(ua)) {
    browser = 'Edge ' + (ua.match(/Edg\/([\d.]+)/) || ['',''])[1];
  } else if (/OPR|Opera/i.test(ua)) {
    browser = 'Opera';
  } else if (/Chrome/i.test(ua)) {
    browser = 'Chrome ' + (ua.match(/Chrome\/([\d.]+)/) || ['',''])[1];
  } else if (/Firefox/i.test(ua)) {
    browser = 'Firefox ' + (ua.match(/Firefox\/([\d.]+)/) || ['',''])[1];
  } else if (/Safari/i.test(ua)) {
    browser = 'Safari ' + (ua.match(/Version\/([\d.]+)/) || ['',''])[1];
  }

  return `${device} · ${os} · ${browser}`;
}

/** Network / connection type (WiFi, 4G, 3G, 2G, etc.) */
function getConnectionInfo() {
  const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
  if (!conn) return 'Unknown';
  const parts = [];
  if (conn.effectiveType) parts.push(conn.effectiveType.toUpperCase());
  if (conn.type && conn.type !== 'unknown') parts.push(conn.type);
  if (conn.downlink) parts.push(`down:${conn.downlink}Mbps`);
  return parts.join(' · ') || 'Unknown';
}

/** Screen and viewport dimensions */
function getScreenInfo() {
  return `${screen.width}x${screen.height} (viewport ${window.innerWidth}x${window.innerHeight})`;
}

/** Timezone — strong regional signal, zero permission needed */
function getTimezone() {
  return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

/** Browser language */
function getLanguage() {
  return (navigator.languages || [navigator.language]).join(', ');
}

/** Full referrer URL */
function getReferrer() {
  return document.referrer || 'direct';
}

/** Build the full passive data package */
function buildPassiveData() {
  return {
    sessionId:     getOrCreateSessionId(),
    visitorStatus: getVisitorStatus(),
    deviceInfo:    parseUserAgent(),
    screenInfo:    getScreenInfo(),
    language:      getLanguage(),
    timezone:      getTimezone(),
    connection:    getConnectionInfo(),
    referrerUrl:   getReferrer(),
  };
}

// ================================================
// SCROLL DEPTH TRACKING
// ================================================
let maxScrollDepth = 0;

window.addEventListener('scroll', () => {
  const scrolled = window.scrollY;
  const total = document.documentElement.scrollHeight - window.innerHeight;
  const depth = total > 0 ? Math.round((scrolled / total) * 100) : 0;
  if (depth > maxScrollDepth) maxScrollDepth = depth;
}, { passive: true });

// ================================================
// NAVBAR SCROLL
// ================================================
const nav = document.getElementById('nav');
window.addEventListener('scroll', () => {
  nav.classList.toggle('scrolled', window.scrollY > 60);
}, { passive: true });

// ================================================
// MODAL
// ================================================
const overlay = document.getElementById('modalOverlay');

function openModal(source) {
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
  const source   = params.get('utm_source') || params.get('ref') || detectSource();
  const medium   = params.get('utm_medium') || '';
  const campaign = params.get('utm_campaign') || '';

  document.getElementById('utmSource').value   = source;
  document.getElementById('utmMedium').value   = medium;
  document.getElementById('utmCampaign').value = campaign;
}

function detectSource() {
  const ref = document.referrer;
  if (!ref) return 'direct';
  if (ref.includes('whatsapp')) return 'whatsapp';
  if (ref.includes('google'))   return 'google';
  if (ref.includes('facebook') || ref.includes('fb.')) return 'facebook';
  if (ref.includes('instagram')) return 'instagram';
  if (ref.includes('twitter') || ref.includes('t.co')) return 'twitter';
  if (ref.includes('tiktok'))   return 'tiktok';
  try { return 'referral:' + new URL(ref).hostname; } catch (e) { return 'referral'; }
}

// ================================================
// GEOLOCATION (GPS → IP fallback)
// ================================================
async function captureLocation() {
  if (document.getElementById('locationField').value) return;

  if ('geolocation' in navigator) {
    try {
      const pos = await new Promise((resolve, reject) =>
        navigator.geolocation.getCurrentPosition(resolve, reject, {
          timeout: 5000,
          maximumAge: 60000
        })
      );
      const lat = pos.coords.latitude.toFixed(5);
      const lon = pos.coords.longitude.toFixed(5);
      document.getElementById('locationField').value = `GPS: ${lat},${lon}`;
      return;
    } catch (_) {}
  }

  try {
    const res = await fetch('https://ipapi.co/json/', { signal: AbortSignal.timeout(5000) });
    if (res.ok) {
      const d = await res.json();
      document.getElementById('locationField').value =
        [d.city, d.region, d.country_name, `(IP: ${d.ip})`].filter(Boolean).join(', ');
    }
  } catch (_) {
    document.getElementById('locationField').value = 'Could not capture';
  }
}

// ================================================
// FORM SUBMISSION → GOOGLE SHEETS
// ================================================
async function submitForm(event) {
  event.preventDefault();

  const form        = document.getElementById('waitlistForm');
  const submitBtn   = document.getElementById('submitBtn');
  const submitLabel = document.getElementById('submitLabel');
  const submitLoader= document.getElementById('submitLoader');

  // Timestamp (Nigeria time)
  document.getElementById('signupTimestamp').value = new Date().toLocaleString('en-NG', {
    timeZone: 'Africa/Lagos',
    dateStyle: 'medium',
    timeStyle: 'short'
  });

  // Behavioral signals
  const timeOnPage = Math.round((Date.now() - PAGE_LOAD_TIME) / 1000);
  document.getElementById('timeOnPage').value  = `${timeOnPage}s`;
  document.getElementById('scrollDepth').value = `${maxScrollDepth}%`;

  // Passive device/session data
  const passive = buildPassiveData();
  document.getElementById('sessionId').value     = passive.sessionId;
  document.getElementById('visitorStatus').value = passive.visitorStatus;
  document.getElementById('deviceInfo').value    = passive.deviceInfo;
  document.getElementById('screenInfo').value    = passive.screenInfo;
  document.getElementById('timezone').value      = passive.timezone;
  document.getElementById('language').value      = passive.language;
  document.getElementById('connection').value    = passive.connection;
  document.getElementById('referrerUrl').value   = passive.referrerUrl;

  // Loading state
  submitLabel.style.display = 'none';
  submitLoader.style.display = 'inline';
  submitBtn.disabled = true;

  // Collect all fields
  const data = {};
  new FormData(form).forEach((v, k) => { data[k] = v; });

  if (!GOOGLE_SCRIPT_URL || GOOGLE_SCRIPT_URL === 'YOUR_GOOGLE_SCRIPT_URL_HERE') {
    console.log('📋 Form data (dev mode):', data);
    showSuccess();
    return;
  }

  try {
    await fetch(GOOGLE_SCRIPT_URL, {
      method: 'POST',
      mode: 'no-cors',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    showSuccess();
  } catch (err) {
    console.error('Submission error:', err);
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
      window.scrollTo({
        top: target.getBoundingClientRect().top + window.pageYOffset - 80,
        behavior: 'smooth'
      });
    }
  });
});

// ================================================
// INTERSECTION OBSERVER (scroll-in animations)
// ================================================
const animObserver = new IntersectionObserver(
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
  animObserver.observe(el);
});

// ================================================
// CASCADING LOCATION PICKER (via countriesnow API)
// ================================================
async function initLocationPicker() {
  const countryEl = document.getElementById('countrySelect');
  const stateEl   = document.getElementById('stateSelect');
  const cityEl    = document.getElementById('citySelect');
  const stateRow  = document.getElementById('stateRow');
  const areaRow   = document.getElementById('areaRow');

  try {
    // 1. Fetch all countries
    const res = await fetch('https://countriesnow.space/api/v0.1/countries/states');
    const data = await res.json();
    if (!data.error) {
      const countries = data.data;

      // Populate countries
      countries.forEach(c => {
        const opt = document.createElement('option');
        opt.value = c.name; // Use name as value to pass to API later
        opt.textContent = c.name;
        countryEl.appendChild(opt);
      });

      // Pre-select Nigeria
      const ngOpt = Array.from(countryEl.options).find(opt => opt.value === 'Nigeria');
      if (ngOpt) ngOpt.selected = true;

      // When Country changes
      countryEl.addEventListener('change', async () => {
        const selectedCountry = countryEl.value;
        stateEl.innerHTML = '<option value="">Select state / region...</option>';
        cityEl.innerHTML  = '<option value="">Select city...</option>';
        stateRow.style.display = 'none';
        areaRow.style.display  = 'none';

        if (!selectedCountry) return;

        const countryObj = countries.find(c => c.name === selectedCountry);
        if (countryObj && countryObj.states && countryObj.states.length > 0) {
          countryObj.states.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.name;
            opt.textContent = s.name;
            stateEl.appendChild(opt);
          });
          stateRow.style.display = 'grid';
        } else {
          // No states available, go straight to manual area input
          areaRow.style.display = 'block';
        }
      });

      // When State changes -> Fetch cities for that state
      stateEl.addEventListener('change', async () => {
        const selectedCountry = countryEl.value;
        const selectedState = stateEl.value;
        cityEl.innerHTML  = '<option value="">Select city...</option>';
        areaRow.style.display = 'none';

        if (!selectedState) return;

        try {
          const resCities = await fetch('https://countriesnow.space/api/v0.1/countries/state/cities', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ country: selectedCountry, state: selectedState })
          });
          const dataCities = await resCities.json();

          if (!dataCities.error && dataCities.data && dataCities.data.length > 0) {
            dataCities.data.forEach(city => {
              const opt = document.createElement('option');
              opt.value = city;
              opt.textContent = city;
              cityEl.appendChild(opt);
            });
            cityEl.parentElement.style.display = 'block';
          } else {
            // Hide city dropdown if no cities exist, just show manual area
            cityEl.innerHTML = '<option value="N/A">N/A</option>';
            cityEl.parentElement.style.display = 'none';
          }
        } catch (e) {
          console.error('Error fetching cities', e);
          cityEl.innerHTML = '<option value="N/A">N/A</option>';
          cityEl.parentElement.style.display = 'none';
        }
        areaRow.style.display = 'block'; // Always show area box at the end
      });

      // When City changes
      cityEl.addEventListener('change', () => {
        if (cityEl.value) areaRow.style.display = 'block';
      });

      // Trigger initial country selection (Nigeria)
      countryEl.dispatchEvent(new Event('change'));
    }
  } catch (err) {
    console.error('Location picker failed to load:', err);
    // Fallback: just show the free-text Area input if API fails
    countryEl.innerHTML = '<option value="Manual Entry">Manual Entry</option>';
    areaRow.style.display = 'block';
  }
}

// ================================================
// INIT
// ================================================
document.addEventListener('DOMContentLoaded', () => {
  parseUTM();
  getOrCreateSessionId();
  getVisitorStatus();
  initLocationPicker();
});
