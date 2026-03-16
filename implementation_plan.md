# LocaLe Escrow Flow Clickable Prototype — Implementation Plan

## Goal
Build a **clickable web prototype (PWA-style, mobile-first)** of the escrow flow end-to-end for HelpBnk Milestone 3. Also build a **print-friendly walkthrough page** that can be printed to PDF for submission.

## Proposed Changes

### Design System & Shared Assets

#### [NEW] [styles.css](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/styles.css)
- Full mobile-first CSS design system
- Color palette: dark/premium theme with green accents (trust color)
- Shared components: cards, buttons, status badges, timeline, top bar
- Print-friendly styles for walkthrough page
- Responsive (390px base, scales up)

#### [NEW] [app.js](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/app.js)
- Page navigation helpers
- QR code generation (using a simple JS QR library or SVG placeholder)
- Code verification logic (6-digit code match)
- Mock data (providers, job details, prices)
- Session storage for flow state

---

### Prototype Pages (Happy Path)

#### [NEW] [index.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/index.html)
- Home screen with LocaLe branding + bell icon top bar
- Service cards: Laundry/Wash & Iron, Room Cleaning, Errand/Pickup & Delivery
- "Post a job" CTA
- Add-to-home-screen hint banner

#### [NEW] [job.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/job.html)
- Job details form (location auto-filled, description, preferred time)
- Price selector: ₦3,000 / ₦5,000 / ₦10,000 / custom
- "Continue" CTA

#### [NEW] [provider.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/provider.html)
- 2–3 provider cards (name, distance, rating, "vouched by X")
- "Select provider" CTA

#### [NEW] [checkout.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/checkout.html)
- Big trust line: "Your money is held safely until the job is done."
- Payment options: Bank Transfer (default/recommended), Card (secondary)
- Transfer details: mock account number + bank name
- "I've sent the transfer" / "Pay with card" CTAs
- Small print: "Provider cannot receive payment until you scan to release."

#### [NEW] [status.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/status.html)
- Big "Funds Secured" badge
- Timeline: Booked → Funds Secured → In Progress → Job Done → Release
- Escrow ref: LCL-0001
- "Mark job as done" CTA + "Report a problem" secondary

#### [NEW] [release.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/release.html)
- "Release payment" title
- Instructions: "Scan the provider's QR only when you're satisfied."
- "Open scanner" CTA → goes to scan.html
- "Not satisfied? Report issue" secondary → dispute.html

#### [NEW] [provider-qr.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/provider-qr.html)
- Provider-side screen: "Show QR to customer"
- QR code image (generated or placeholder)
- "One-time QR for this job. Expires in 10 minutes."

#### [NEW] [scan.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/scan.html)
- "Scan provider QR" title
- Camera viewfinder mock
- "I scanned successfully" CTA → confirm.html
- "Scan failed" → tips: increase brightness / move closer

#### [NEW] [confirm.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/confirm.html)
- "Confirm release" title
- "You're about to release ₦X to [Provider]."
- Checkbox: "I confirm the job is done."
- "Release payment" CTA → receipt.html

#### [NEW] [receipt.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/receipt.html)
- "Payment released" success badge
- Receipt: Job ID, Amount, Fee (₦100), Time
- "Funds sent to provider. Keep this receipt."
- "Rate provider" / "Done" CTAs

---

### Edge-Case Pages

#### [NEW] [dispute.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/dispute.html)
- "Funds on hold" status
- "Problem reported → Support will review within 24 hours"
- "Contact support" CTA

#### [NEW] [cancel.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/cancel.html)
- "Booking canceled" status
- "Funds returned to your account within 1–2 hours"
- Timeline showing cancellation

---

### Walkthrough (PDF-ready)

#### [NEW] [walkthrough.html](file:///c:/Users/KOSISOCHUKWU/Vscodecheckpoints/LocaLe.HelpBnk/walkthrough.html)
- Print-friendly, paginated layout
- **Page 1**: Cover with LocaLe branding + prototype link placeholder + video link placeholder
- **Pages 2–9**: Embedded screenshots/iframes of each step with captions
- **Page 10**: Test results table (empty template for 10–15 testers)
- **Page 11**: Key insights + next changes template
- Designed to look clean when Chrome "Print → Save as PDF"

---

## Design Direction
- **Dark premium theme** with deep navy/charcoal backgrounds
- **Green accent** (#00C853 or similar) for trust/security states
- **Glassmorphism** cards with subtle backdrop blur
- **Smooth micro-animations** on buttons, transitions, and status changes
- **Google Font**: Inter or Outfit for modern feel
- **Mobile-first**: 390px viewport, centered on desktop

## Verification Plan

### Browser Testing
- Open `index.html` in Chrome browser
- Tap through the entire happy path: Home → Job → Provider → Checkout → Status → Release → Scan → Confirm → Receipt
- Verify all links/buttons navigate correctly
- Test the edge paths: Status → Dispute, and Cancel flow
- Test on mobile viewport (390px width via Chrome DevTools)
- Test the walkthrough.html print layout (Ctrl+P preview)

### Manual Verification
- Open each page and verify layout, copy, and flow
- Check that the QR image displays on provider-qr.html
- Verify the walkthrough.html has proper page breaks for PDF export
