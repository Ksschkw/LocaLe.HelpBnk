import { WifiOff, ServerCrash } from 'lucide-react'
import Button from '../ui/Button'

export default function OfflineFallback({ type, onRetry }) {
  const isNetwork = type === 'network'

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      background: 'rgba(10, 14, 26, 0.95)',
      backdropFilter: 'blur(10px)',
      display: 'flex', flexDirection: 'column',
      justifyContent: 'center', alignItems: 'center',
      zIndex: 9999999,
      color: 'white',
      padding: 24, textAlign: 'center'
    }}>
      <div style={{ background: 'var(--surface-color)', padding: 32, borderRadius: 16, border: '1px solid var(--border-color)', maxWidth: 400, width: '100%' }}>
        {isNetwork ? (
          <WifiOff size={64} style={{ color: 'var(--danger-color)', marginBottom: 24 }} />
        ) : (
          <ServerCrash size={64} style={{ color: 'var(--brand-primary)', marginBottom: 24 }} />
        )}
        
        <h2 style={{ marginBottom: 12 }}>
          {isNetwork ? 'No Internet Connection' : 'Server Unreachable'}
        </h2>
        
        <p style={{ color: 'var(--text-secondary)', marginBottom: 24, lineHeight: 1.5 }}>
          {isNetwork 
            ? "You are currently offline. Please check your internet connection to continue securely managing escrows."
            : "The LocaLe Trust Engine is currently experiencing downtime or is unreachable. Please try again in a moment."}
        </p>

        <Button block size="lg" onClick={onRetry}>
          {isNetwork ? 'Retry Connection' : 'Ping Server'}
        </Button>
      </div>
    </div>
  )
}
