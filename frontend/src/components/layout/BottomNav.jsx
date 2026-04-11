import { useNavigate, useLocation } from 'react-router-dom'
import { Home, Briefcase, Activity, Wallet, User } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'
import styles from './BottomNav.module.css'

const items = [
  { to: '/discover', icon: Home, label: 'Discover' },
  { to: '/jobs',     icon: Briefcase, label: 'Jobs' },
  { to: '/activity', icon: Activity, label: 'Activity' },
  { to: '/wallet',   icon: Wallet, label: 'Wallet' },
  { to: '/profile',  icon: User, label: 'Profile' },
]

export default function BottomNav() {
  const navigate = useNavigate()
  const location = useLocation()
  const { unreadCount } = useAuth()

  return (
    <nav className={styles.nav}>
      {items.map(({ to, icon: Icon, label }) => {
        const active = location.pathname.startsWith(to)
        return (
          <button key={to} onClick={() => navigate(to)} className={[styles.item, active ? styles.active : ''].join(' ')}>
            <span className={styles.iconWrap}>
              <Icon size={22} strokeWidth={active ? 2.5 : 1.8} />
              {label === 'Activity' && unreadCount > 0 && (
                <span className={styles.badge}>{unreadCount > 9 ? '9+' : unreadCount}</span>
              )}
            </span>
            <span className={styles.label}>{label}</span>
          </button>
        )
      })}
    </nav>
  )
}
