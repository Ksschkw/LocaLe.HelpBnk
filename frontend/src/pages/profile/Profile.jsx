import { useState, useCallback, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useDialog } from '../../contexts/DialogContext'
import { useToast, fmt, tierColor, tierEmoji } from '../../hooks/useUtils'
import { servicesApi } from '../../api'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import TopBar from '../../components/layout/TopBar'
import styles from './Profile.module.css'

export default function ProfilePage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const { confirm, prompt } = useDialog()
  const [myServices, setMyServices] = useState([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    if (!user) return
    setLoading(true)
    try {
      const { data } = await servicesApi.getBySeller(user.userId || user.id)
      setMyServices(data)
    } catch { /* silent */ }
    finally { setLoading(false) }
  }, [user])

  useEffect(() => { load() }, [load])

  const handleLogout = async () => {
    await logout()
    navigate('/', { replace: true })
  }

  const editService = async (id) => {
    const val = await prompt({ title: 'Edit Base Price', message: 'Enter new base price (Numbers only):' })
    if(!val || isNaN(val)) return
    try {
      await servicesApi.update(id, { basePrice: Number(val) })
      toast('Service rate updated successfully!', 'success')
      load()
    } catch(e) { toast(e.message || 'Failed to update service', 'error') }
  }

  const activateService = async (id) => {
    try {
      await servicesApi.activate(id)
      toast('Service is now live in Discover!', 'success')
      load()
    } catch(e) { toast(e.message || 'Failed to activate service', 'error') }
  }

  const deleteService = async (id) => {
    if(!await confirm({ title: 'Terminate Listing', message: 'Are you sure you want to completely tear down this service listing?', danger: true, confirmText: 'Terminate' })) return
    try {
      await servicesApi.delete(id)
      toast('Service Terminated.', 'success')
      load()
    } catch(e) { toast(e.message || 'Failed to delete service', 'error') }
  }

  if (!user) return null

  const initials = (user.name || 'U').split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase()

  return (
    <div className={styles.page}>
      <TopBar back={false} title="Profile" action={
        <Button size="sm" variant="danger" onClick={handleLogout}>Sign Out</Button>
      } />

      <div className={styles.content}>
        {/* Profile Hero */}
        <div className={styles.profileHero}>
          <div className={styles.glow} />
          <div className={styles.avatar}>{initials}</div>
          <h1 className={styles.name}>{user.name}</h1>
          <p className={styles.email}>{user.email}</p>

          <div className={styles.tierBadge} style={{ color: tierColor(user.tier) }}>
            {tierEmoji(user.tier)} {user.tier || 'Bronze'}
          </div>
        </div>

        {/* Trust Card */}
        <div className={styles.trustGrid}>
          <div className={styles.trustStat}>
            <span className={styles.trustVal} style={{ color: 'var(--brand-primary)' }}>
              {user.trustScore ?? 0}
            </span>
            <span className={styles.trustLabel}>Trust Score</span>
          </div>
          <div className={styles.trustDivider} />
          <div className={styles.trustStat}>
            <span className={styles.trustVal}>{user.totalVouchPoints ?? 0}</span>
            <span className={styles.trustLabel}>Vouch Points</span>
          </div>
          <div className={styles.trustDivider} />
          <div className={styles.trustStat}>
            <span className={styles.trustVal} style={{ color: 'var(--info)' }}>{user.jobsCompleted ?? 0}</span>
            <span className={styles.trustLabel}>Jobs Done</span>
          </div>
        </div>

        {/* Actions */}
        <div className={styles.actions}>
          <Button block variant="secondary" onClick={() => navigate('/create-service')}>+ Host New Service</Button>
          {user.role === 'Admin' && (
            <Button block variant="danger" onClick={() => navigate('/admin')}>⚡ Admin Console</Button>
          )}
        </div>

        {/* My Services */}
        <h3 className={styles.sectionTitle}>My Hosted Services</h3>

        {loading ? (
          Array.from({ length: 2 }).map((_, i) => (
            <div key={i} className="skeleton" style={{ height: 80, marginBottom: 12 }} />
          ))
        ) : myServices.length === 0 ? (
          <div className={styles.empty}>No services yet. Host your first service!</div>
        ) : myServices.map(s => (
          <Card key={s.id} className={styles.serviceRow} style={{ display: 'flex', flexDirection: 'column', gap: '0.8rem', alignItems: 'stretch' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 700 }}>{s.title}</div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-2)', marginTop: 2 }}>
                  {fmt(s.basePrice)} · {s.trustPoints} pts
                </div>
              </div>
              <span className={s.isDiscoveryEnabled ? styles.tagGreen : styles.tagGray}>
                {s.isDiscoveryEnabled ? '✓ Listed' : '⏳ Pending'}
              </span>
            </div>
            
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginTop: 4 }}>
               <Button size="sm" variant="secondary" onClick={() => editService(s.id)}>✏️ Edit Base Rate</Button>
               {!s.isDiscoveryEnabled && (
                  <Button size="sm" variant="primary" onClick={() => activateService(s.id)}>💡 Activate</Button>
               )}
               <Button size="sm" variant="danger" onClick={() => deleteService(s.id)}>🗑️ Terminate</Button>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}
