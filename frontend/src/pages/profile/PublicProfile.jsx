import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { usersApi, servicesApi } from '../../api'
import { useToast, tierColor, tierEmoji } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import styles from './Profile.module.css'

export default function PublicProfilePage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const toast = useToast()
  
  const [user, setUser] = useState(null)
  const [services, setServices] = useState([])
  const [loading, setLoading] = useState(true)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const [uRes, sRes] = await Promise.all([
        usersApi.getPublicProfile(id),
        servicesApi.getBySeller(id)
      ])
      setUser(uRes.data)
      setServices(sRes.data.filter(s => s.isDiscoveryEnabled) || [])
    } catch {
      toast('Failed to load user profile.', 'error')
      navigate(-1)
    } finally {
      setLoading(false)
    }
  }, [id, navigate, toast])

  useEffect(() => { loadData() }, [loadData])

  if (loading || !user) return (
    <div className={styles.page}>
      <TopBar back title="User Profile" />
      <div className={styles.content} style={{ display: 'flex', justifyContent: 'center', padding: 40, height: 'auto' }}>
        <div className="spinner" />
      </div>
    </div>
  )

  const initials = (user.name || 'U').split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase()

  return (
    <div className={styles.page}>
      <TopBar back title={`${user.name}'s Profile`} />

      <div className={styles.content}>
        <div className={styles.profileHero}>
          <div className={styles.glow} />
          <div className={styles.avatar}>{initials}</div>
          <h1 className={styles.name}>{user.name}</h1>
          <p className={styles.email} style={{ marginTop: 8 }}>{user.bio || 'Trustworthy member of the LocaLe Network.'}</p>

          <div className={styles.tierBadge} style={{ color: tierColor(user.tier) }}>
            {tierEmoji(user.tier)} {user.tier || 'Bronze'}
          </div>
        </div>

        <div className={styles.trustGrid}>
          <div className={styles.trustStat}>
            <span className={styles.trustVal} style={{ color: 'var(--brand-primary)' }}>{user.trustScore ?? 0}</span>
            <span className={styles.trustLabel}>Trust Score</span>
          </div>
          <div className={styles.trustDivider} />
          <div className={styles.trustStat}>
            <span className={styles.trustVal}>{user.totalVouchPoints ?? 0}</span>
            <span className={styles.trustLabel}>Vouches</span>
          </div>
          <div className={styles.trustDivider} />
          <div className={styles.trustStat}>
            <span className={styles.trustVal} style={{ color: 'var(--info)' }}>{user.jobsCompleted ?? 0}</span>
            <span className={styles.trustLabel}>Jobs Done</span>
          </div>
        </div>

        <h3 className={styles.sectionTitle}>Provided Services</h3>
        {services.length === 0 ? (
          <div className={styles.empty}>No active services listed by this provider.</div>
        ) : services.map(s => (
          <Card key={s.id} className={styles.serviceRow} onClick={() => navigate(`/service/${s.id}`)} style={{ cursor: 'pointer', marginBottom: 12 }}>
             <div style={{ flex: 1 }}>
               <div style={{ fontWeight: 700, fontSize: '1.05rem', color: 'var(--brand-primary)' }}>{s.title}</div>
               <div style={{ fontSize: '0.85rem', color: 'var(--text-2)', marginTop: 4 }}>
                 {s.trustPoints} Trust Points
               </div>
             </div>
             <Button variant="ghost" size="sm">View →</Button>
          </Card>
        ))}
      </div>
    </div>
  )
}
