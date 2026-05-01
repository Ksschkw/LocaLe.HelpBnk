import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Bell } from 'lucide-react'
import { useEffect, useState } from 'react'
import { notificationsApi, adminApi } from '../../api'
import styles from './TopBar.module.css'

export default function TopBar({ title, back, backTo, action }) {
  const navigate = useNavigate()
  const [unreadCount, setUnreadCount] = useState(0)
  const [impersonationActive, setImpersonationActive] = useState(false)

  useEffect(() => {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission()
    }

    const getCount = async () => {
      try {
        const { data } = await notificationsApi.getUnreadCount()
        const count = data.count || 0
        setUnreadCount(prev => {
           if (count > prev && prev > 0 && 'Notification' in window && Notification.permission === 'granted') {
              new Notification('LocaLe', { body: `You have new notifications (${count})` })
           }
           return count
        })
      } catch {}
    }
    getCount()
    const checkImpersonation = async () => {
      try {
        const { data } = await adminApi.isImpersonationActive()
        if (data && data.active) setImpersonationActive(true)
      } catch {}
    }
    checkImpersonation()
    const id = setInterval(getCount, 30000)
    return () => clearInterval(id)
  }, [])

  const handleBack = () => {
    if (backTo) navigate(backTo)
    else navigate(-1)
  }

  return (
    <header className={styles.bar}>
      {back !== false ? (
        <button className={styles.backBtn} onClick={handleBack}>
          <ArrowLeft size={20} />
        </button>
      ) : <div className={styles.spacer} />}

      <h1 className={styles.title}>{title}</h1>

      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        {action && <div className={styles.action}>{action}</div>}
        {impersonationActive && (
          <button onClick={async () => { try { await adminApi.revertImpersonation(); window.location.reload() } catch {} }} style={{ background: 'transparent', border: '1px solid #ff6666', color: '#ff6666', padding: '6px 8px', borderRadius: 6, cursor: 'pointer' }}>
            Revert to Admin
          </button>
        )}
        <button className={styles.bellBtn} onClick={() => navigate('/notifications')} style={{ background: 'transparent', border: 'none', color: 'white', position: 'relative', cursor: 'pointer', padding: '4px' }}>
          <Bell size={20} />
          {unreadCount > 0 && (
             <span style={{ position: 'absolute', top: 0, right: 0, background: 'red', color: 'white', fontSize: '0.6rem', padding: '2px 4px', borderRadius: '50%', fontWeight: 'bold' }}>
               {unreadCount}
             </span>
          )}
        </button>
      </div>
    </header>
  )
}
