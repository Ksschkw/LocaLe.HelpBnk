import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell, CheckCircle, Info, ShieldAlert, CreditCard } from 'lucide-react'
import { notificationsApi } from '../../api'
import { timeAgo, useToast } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import styles from './Notifications.module.css'

export default function NotificationsPage() {
  const navigate = useNavigate()
  const toast = useToast()
  
  const [notifications, setNotifications] = useState([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await notificationsApi.getAll()
      setNotifications(res.data)
    } catch {
      toast('Failed to load notifications', 'error')
    } finally {
      setLoading(false)
    }
  }, [toast])

  useEffect(() => { load() }, [load])

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllRead()
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
      navigate('/activity')
    } catch {
      toast('Network error', 'error')
    }
  }

  const handleReadSingle = async (n) => {
    if (!n.isRead) {
      try {
        await notificationsApi.markRead(n.id)
        setNotifications(prev => prev.map(x => x.id === n.id ? { ...x, isRead: true } : x))
      } catch {}
    }
    // Navigate logic based on notification type
    if (n.type === 'JobBooking' || n.type === 'EscrowReleased' || n.type === 'MessageReceived') {
       if (n.relatedEntityId) navigate(`/chat/${n.relatedEntityId}`)
    } else if (n.type === 'WalletDeposit' || n.type === 'PlatformFeeRecorded') {
       navigate('/wallet')
    }
  }

  const getIcon = (type) => {
    if (!type) return <Info size={18} />
    if (type.includes('Escrow') || type.includes('Dispute')) return <ShieldAlert size={18} color="#FFD700" />
    if (type.includes('Wallet') || type.includes('Fee')) return <CreditCard size={18} color="#00EAFF" />
    if (type.includes('Booking') || type.includes('Message')) return <Bell size={18} color="#0088FF" />
    return <Info size={18} />
  }

  return (
    <div className={styles.page}>
      <TopBar title="Notification Center" />
      <div className={styles.content}>
        <div className={styles.headerRow}>
          <h2 className={styles.title}>Recent Activity</h2>
          {notifications.some(n => !n.isRead) && (
            <Button size="sm" variant="outline" onClick={handleMarkAllRead}>
              <CheckCircle size={14} style={{ marginRight: 6 }} /> Mark all read
            </Button>
          )}
        </div>

        {loading ? (
          <div>Syncing updates...</div>
        ) : notifications.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 40, color: 'gray' }}>
            <Bell size={48} style={{ opacity: 0.2, marginBottom: 16 }} />
            <div>You're all caught up!</div>
          </div>
        ) : (
          notifications.map(n => (
            <Card key={n.id} className={[styles.card, !n.isRead ? styles.unread : ''].join(' ')} onClick={() => handleReadSingle(n)}>
              <div className={styles.iconWrap}>
                {getIcon(n.type)}
              </div>
              <div className={styles.info}>
                <h4>{n.title || (n.type ? (typeof n.type === 'string' ? n.type.replace(/([A-Z])/g, ' $1').trim() : 'System Alert') : 'Notification')}</h4>
                <p>{n.body || n.message}</p>
                <div className={styles.time}>{timeAgo(n.createdAt)}</div>
              </div>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}
