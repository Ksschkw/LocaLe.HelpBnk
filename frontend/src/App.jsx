import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { DialogProvider } from './contexts/DialogContext'
import BottomNav from './components/layout/BottomNav'
import OfflineFallback from './components/layout/OfflineFallback'

// Pages
import AuthPage from './pages/auth/Auth'
import DiscoverPage from './pages/discover/Discover'
import ActivityPage from './pages/activity/Activity'
import WalletPage from './pages/wallet/Wallet'
import ProfilePage from './pages/profile/Profile'

// Lazy stubs for pages we'll add
import { lazy, Suspense, useEffect, useState, useCallback } from 'react'
import { notificationsApi } from './api'
const JobsPage = lazy(() => import('./pages/jobs/Jobs'))
const ManageJobPage = lazy(() => import('./pages/jobs/ManageJob'))
const AdminPage = lazy(() => import('./pages/admin/Admin'))
const CreateServicePage = lazy(() => import('./pages/profile/CreateService'))
const ServicePage = lazy(() => import('./pages/service/Service'))
const ChatPage = lazy(() => import('./pages/chat/Chat'))
const NotificationsPage = lazy(() => import('./pages/notifications/Notifications'))
const PublicProfilePage = lazy(() => import('./pages/profile/PublicProfile'))

function RequireAuth({ children }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return (
    <div style={{ minHeight: '100dvh', display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column', gap: 16 }}>
      <div style={{ fontSize: '2rem', fontWeight: 900, background: 'linear-gradient(135deg,#0088FF,#00EAFF)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>LocaLe.</div>
      <div className="spinner" />
    </div>
  )

  if (!user) return <Navigate to="/" state={{ from: location }} replace />
  return children
}

const AUTH_PATHS = ['/discover', '/jobs', '/activity', '/wallet', '/profile', '/admin', '/create-service', '/service', '/chat', '/notifications']

function AppShell() {
  const location = useLocation()
  const { user } = useAuth()
  const showNav = user && AUTH_PATHS.some(p => location.pathname.startsWith(p))

  // Network & Offline resilience
  const [isOffline, setIsOffline] = useState(!navigator.onLine)
  const [isServerDown, setIsServerDown] = useState(false)

  useEffect(() => {
    const handleOnline = () => { setIsOffline(false); setIsServerDown(false) }
    const handleOffline = () => setIsOffline(true)
    const handleServerDown = () => setIsServerDown(true)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)
    window.addEventListener('locale-server-down', handleServerDown)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
      window.removeEventListener('locale-server-down', handleServerDown)
    }
  }, [])

  // PWA Install Prompt State
  const [deferredPrompt, setDeferredPrompt] = useState(null)
  
  useEffect(() => {
    const handleBeforeInstall = (e) => {
      e.preventDefault()
      setDeferredPrompt(e)
    }
    window.addEventListener('beforeinstallprompt', handleBeforeInstall)
    return () => window.removeEventListener('beforeinstallprompt', handleBeforeInstall)
  }, [])

  const handleInstallClick = async () => {
    if (!deferredPrompt) return
    deferredPrompt.prompt()
    const { outcome } = await deferredPrompt.userChoice
    if (outcome === 'accepted') setDeferredPrompt(null)
  }

  // Push Notification via SignalR WebSockets (Replaced Polling)
  const [notifiedIds, setNotifiedIds] = useState(new Set())
  
  useEffect(() => {
    if (!user) return
    
    // Request browser notification permission once
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission()
    }

    let connection = null;

    const connectSignalR = async () => {
      // Dynamic import to avoid blowing up the main bundle if offline
      const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
      
      connection = new HubConnectionBuilder()
        .withUrl('/hubs/notifications', {
            // Include credentials so the HTTPOnly JWT cookie is passed
            withCredentials: true 
        })
        .configureLogging(LogLevel.Warning)
        .withAutomaticReconnect()
        .build();

      connection.on("ReceiveNotification", (notification) => {
        if (!notifiedIds.has(notification.id) && Notification.permission === 'granted') {
           new Notification(notification.title || 'LocaLe Update', { body: notification.body || notification.message })
           setNotifiedIds(prev => new Set(prev).add(notification.id))
        }
      });

      try {
        await connection.start();
        console.log("LocaLe SignalR WebSocket Connected.");
      } catch (err) {
        console.error("SignalR WebSocket failed to connect:", err);
      }
    };

    connectSignalR();

    return () => {
      if (connection) {
        connection.stop();
      }
    }
  }, [user, notifiedIds])

  return (
    <>
      {(isOffline || isServerDown) && (
        <OfflineFallback 
          type={isOffline ? 'network' : 'server'} 
          onRetry={() => {
            if (navigator.onLine) setIsOffline(false)
            setIsServerDown(false)
            window.location.reload()
          }} 
        />
      )}
      {deferredPrompt && !isOffline && !isServerDown && (
        <div style={{ background: 'var(--brand-primary)', color: 'white', padding: '12px 16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', zIndex: 99999, position: 'fixed', top: 0, left: 0, right: 0 }}>
          <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>Install LocaLe App for the best experience!</div>
          <button onClick={handleInstallClick} style={{ background: 'white', color: 'var(--brand-primary)', border: 'none', padding: '6px 12px', borderRadius: 16, fontWeight: 'bold', cursor: 'pointer' }}>Install</button>
        </div>
      )}
      {showNav && <BottomNav />}
      <div className="page-main" style={{ marginTop: deferredPrompt ? 48 : 0 }}>
        <Suspense fallback={<div style={{ padding: 24 }}><div className="skeleton" style={{ height: 200 }} /></div>}>
          <Routes>
            <Route path="/" element={<AuthPage />} />
            <Route path="/discover" element={<RequireAuth><DiscoverPage /></RequireAuth>} />
            <Route path="/jobs" element={<RequireAuth><JobsPage /></RequireAuth>} />
            <Route path="/jobs/manage/:id" element={<RequireAuth><ManageJobPage /></RequireAuth>} />
            <Route path="/activity" element={<RequireAuth><ActivityPage /></RequireAuth>} />
            <Route path="/wallet" element={<RequireAuth><WalletPage /></RequireAuth>} />
            <Route path="/profile" element={<RequireAuth><ProfilePage /></RequireAuth>} />
            <Route path="/profile/:id" element={<RequireAuth><PublicProfilePage /></RequireAuth>} />
            <Route path="/admin" element={<RequireAuth><AdminPage /></RequireAuth>} />
            <Route path="/create-service" element={<RequireAuth><CreateServicePage /></RequireAuth>} />
            <Route path="/service/:id" element={<RequireAuth><ServicePage /></RequireAuth>} />
            <Route path="/chat/:id" element={<RequireAuth><ChatPage /></RequireAuth>} />
            <Route path="/chat/booking/:bookingId" element={<RequireAuth><ChatPage /></RequireAuth>} />
            <Route path="/notifications" element={<RequireAuth><NotificationsPage /></RequireAuth>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </div>
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <DialogProvider>
          <AppShell />
        </DialogProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
