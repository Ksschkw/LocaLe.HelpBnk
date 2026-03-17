# LocaLe Landing Page — Setup Guide

## What you have

```
landing/
├── index.html    ← Full landing page
├── style.css     ← Design system
├── app.js        ← Form logic, UTM, geolocation
└── SETUP.md      ← This file
```

---

## Step 1: Create your Google Sheet

1. Go to [sheets.google.com](https://sheets.google.com) → **New spreadsheet**
2. Rename it: `LocaLe Waitlist`
3. On **Row 1**, add these headers exactly (copy-paste this whole row):

```
Timestamp | Name | Phone | Neighborhood | Role | Job Type | Price Range | Fee Preference | Biggest Fear | UTM Source | UTM Medium | UTM Campaign | Location | Opened From | Time On Page | Scroll Depth | Session ID | Visitor Status | Device Info | Screen Info | Timezone | Language | Connection | Referrer URL
```

---

## Step 2: Deploy the Google Apps Script

1. In your Google Sheet, click **Extensions → Apps Script**
2. Delete all existing code and paste this:

```javascript
function doPost(e) {
  try {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
    var data = JSON.parse(e.postData.contents);

    sheet.appendRow([
      data.signupTimestamp  || new Date().toLocaleString(),
      data.name             || '',
      data.phone            || '',
      data.neighborhood     || '',
      data.role             || '',
      data.jobType          || '',
      data.priceRange       || '',
      data.feePref          || '',
      data.biggestFear      || '',
      data.utmSource        || 'direct',
      data.utmMedium        || '',
      data.utmCampaign      || '',
      data.location         || '',
      data.openedFrom       || '',
      // Behavioral
      data.timeOnPage       || '',
      data.scrollDepth      || '',
      // Device / session
      data.sessionId        || '',
      data.visitorStatus    || '',
      data.deviceInfo       || '',
      data.screenInfo       || '',
      data.timezone         || '',
      data.language         || '',
      data.connection       || '',
      data.referrerUrl      || ''
    ]);

    return ContentService
      .createTextOutput(JSON.stringify({ status: 'ok' }))
      .setMimeType(ContentService.MimeType.JSON);
  } catch (err) {
    return ContentService
      .createTextOutput(JSON.stringify({ status: 'error', message: err.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}
```

3. Click **Save** (💾)
4. Click **Deploy → New deployment** (or **Manage deployments → Edit** if already deployed, then click **Deploy** again to create a new version)
5. Settings:
   - Type: **Web app**
   - Execute as: **Me**
   - Who has access: **Anyone**
6. Click **Deploy** → Copy the **Web app URL**

> ⚠️ **Important:** Every time you change the Apps Script code, you must create a **new version** under "Manage deployments → Edit → New version" and redeploy — otherwise the old version keeps running.



---

## Step 3: Paste the URL into app.js

Open `landing/app.js` and replace line 14:

```javascript
// BEFORE
const GOOGLE_SCRIPT_URL = 'YOUR_GOOGLE_SCRIPT_URL_HERE';

// AFTER
const GOOGLE_SCRIPT_URL = 'https://script.google.com/macros/s/YOUR_REAL_ID/exec';
```

Also update your WhatsApp number on line 17:
```javascript
const WHATSAPP_NUMBER = '2348012345678'; // your number with country code, no +
```

---

## Step 4: Deploy to Render (Static Site)

1. Push the `landing/` folder to a GitHub repo
2. Go to [render.com](https://render.com) → **New → Static Site**
3. Connect your GitHub repo
4. Settings:
   - **Root Directory**: `landing`  (or wherever your `index.html` is)
   - **Build Command**: *(leave empty)*
   - **Publish Directory**: `.` (dot = current directory)
5. Click **Create Static Site**
6. Render gives you a free URL like `locale-waitlist.onrender.com`

---

## Step 5: UTM Links for sharing

Use these link formats when sharing in different channels:

| Channel | UTM Link |
|---|---|
| WhatsApp groups | `https://your-url.com?utm_source=whatsapp&utm_medium=group&utm_campaign=futo-pilot` |
| Flyer QR code | `https://your-url.com?utm_source=flyer&utm_medium=qr&utm_campaign=futo-pilot` |
| Direct DMs | `https://your-url.com?utm_source=dm&utm_medium=whatsapp&utm_campaign=outreach` |
| Instagram bio | `https://your-url.com?utm_source=instagram&utm_medium=bio` |

These automatically populate the UTM columns in your Google Sheet.

---

## What gets tracked automatically

| Field | How it's captured |
|---|---|
| **UTM Source** | URL parameter `?utm_source=` or auto-detected from `document.referrer` |
| **UTM Medium** | URL parameter `?utm_medium=` |
| **UTM Campaign** | URL parameter `?utm_campaign=` |
| **Location** | Browser GPS (with permission) → IP geolocation fallback via ipapi.co |
| **Opened From** | Which CTA button they clicked (hero / nav / cta) |
| **Timestamp** | Nigeria time (WAT, Africa/Lagos) |

---

## Geolocation — what users see

- If they allow location: you get GPS coordinates (lat/lon)
- If they deny: you get city, region, country from their IP address
- If both fail: you get "Could not capture location"

You are **not** storing IP addresses directly (that's GDPR/NDPR risky). You store the city/region result from the IP lookup.

---

## Quick test checklist (before sharing)

- [ ] Open `index.html` via Live Server or local server (not by double-clicking — geolocation won't work from `file://`)
- [ ] Click "Join Waitlist" — modal opens
- [ ] Fill form → "Secure My Spot" → see success message
- [ ] Check Google Sheet — row was added
- [ ] Test on mobile (Chrome, Safari)
- [ ] Test a UTM link: `?utm_source=test&utm_medium=test`
- [ ] Check that location column fills in

---

## Submission checklist (for milestone)

When you're done testing, your submission PDF or document should include:

1. **Landing page link** (your Render URL)
2. **Waitlist form screenshot** (the modal form)
3. **Results snapshot** — sign-ups, buyer vs provider split, top job types, fee preference %, dates
4. **Outreach evidence** — 3-5 screenshots of WhatsApp posts, flyers, DMs
5. **5 key insights** from signups and feedback (messaging, objections, confusion points)
