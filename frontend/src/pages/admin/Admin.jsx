import { useState, useCallback, useEffect } from 'react'
import { adminApi, seedApi } from '../../api'
import { useAuth } from '../../contexts/AuthContext'
import { useDialog } from '../../contexts/DialogContext'
import { useToast, timeAgo, fmt } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import styles from './Admin.module.css'

export default function AdminPage() {
  const { user } = useAuth()
  const toast = useToast()
  const { confirm, prompt } = useDialog()
  
  const [tab, setTab] = useState('users') // users, disputes, metrics, payments
  const [data, setData] = useState([])
  const [extraData, setExtraData] = useState(null)
  const [loading, setLoading] = useState(true)

  // Easter Egg: SuperAdmin Konami code trigger
  const [konamiIdx, setKonamiIdx] = useState(0)

  // Standard Konami: up, up, down, down, left, right, left, right, b, a
  const konami = ["ArrowUp", "ArrowUp", "ArrowDown", "ArrowDown", "ArrowLeft", "ArrowRight", "ArrowLeft", "ArrowRight", "b", "a"]
  useEffect(() => {
    const handleKeydown = (e) => {
      if (e.key === konami[konamiIdx]) {
        if (konamiIdx === konami.length - 1) {
          toast('🕹️ God Mode Unlocked (But you still need SuperAdmin backend privileges!)', 'success')
          setKonamiIdx(0)
        } else {
          setKonamiIdx(konamiIdx + 1)
        }
      } else {
        setKonamiIdx(0)
      }
    }
    window.addEventListener('keydown', handleKeydown)
    return () => window.removeEventListener('keydown', handleKeydown)
  }, [konamiIdx, toast])

  const load = useCallback(async () => {
    setLoading(true)
    setData([])
    setExtraData(null)
    try {
      if (tab === 'users') {
        // Backend returns paged result
        const res = await adminApi.getUsers(1, 50) 
        setData(res.data?.items || [])
      } else if (tab === 'disputes') {
        const res = await adminApi.getDisputes()
        setData(res.data)
      } else if (tab === 'metrics') {
        const [earnRes, jobsRes] = await Promise.all([
          adminApi.getEarnings(), 
          adminApi.getJobs()
        ])
        setExtraData({ earnings: earnRes.data, jobs: jobsRes.data })
      } else if (tab === 'payments') {
        const res = await adminApi.getPayments()
        setData(res.data)
      }
    } catch {
      toast('Failed to load admin scope', 'error')
    } finally {
      setLoading(false)
    }
  }, [tab, toast])

  useEffect(() => { load() }, [load])

  if (!user || (user.role !== 'Admin' && user.role !== 'SuperAdmin')) return (
    <div style={{ padding: 40, textAlign: 'center', color: 'red' }}>Access Denied. You are not privileged enough!</div>
  )

  const isSuperAdmin = user.role === 'SuperAdmin'

  const handleDeleteUser = async (id) => {
    const isOk = await confirm({ title: 'Delete User', message: 'NUKE THIS USER? Permanent action.', danger: true })
    if (!isOk) return
    try {
      await adminApi.deleteUser(id)
      toast('Target destroyed.', 'success')
      load()
    } catch { toast('Removal failed', 'error') }
  }

  const handleResolveDispute = async (id) => {
    const resolution = await prompt({ title: 'Resolve Dispute', placeholder: 'Enter detailed resolution terms (Admin Notes):' })
    if (!resolution) return
    try {
      await adminApi.resolveDispute(id, resolution)
      toast('Dispute firmly resolved.', 'success')
      load()
    } catch { toast('Resolution rejected by system', 'error') }
  }

  const handleToggleDiscovery = async (id, currentValue) => {
    try {
      await adminApi.toggleDiscovery(id, !currentValue)
      toast('Service discovery updated', 'success')
      load()
    } catch { toast('Action blocked', 'error') }
  }

  const handleBroadcast = async () => {
    const msg = await prompt({ title: 'Global Broadcast', placeholder: 'Enter broadcast message' })
    if(!msg) return
    try {
      await adminApi.broadcast(msg)
      toast('Broadcast delivered to all connected clients!', 'success')
    } catch { toast('Broadcast failed', 'error')}
  }

  const handleSeed = async () => {
    const isOk = await confirm({ title: 'Database Seed', message: 'Plant emergency backend seed?', danger: true })
    if (!isOk) return
    try {
      await seedApi.seed()
      toast('Environment seeded!', 'success')
      load()
    } catch { toast('Seed injection failed', 'error')}
  }

  return (
    <div className={styles.page}>
      <TopBar title="The Watchtower (Admin)" />
      
      <div className={styles.tabs} style={{ padding: '0 16px', overflowX: 'auto' }}>
        <button className={[styles.tab, tab === 'users' ? styles.active : ''].join(' ')} onClick={() => setTab('users')}>Users</button>
        <button className={[styles.tab, tab === 'disputes' ? styles.active : ''].join(' ')} onClick={() => setTab('disputes')}>Disputes</button>
        <button className={[styles.tab, tab === 'payments' ? styles.active : ''].join(' ')} onClick={() => setTab('payments')}>Payments</button>
        <button className={[styles.tab, tab === 'metrics' ? styles.active : ''].join(' ')} onClick={() => setTab('metrics')}>Analytics</button>
      </div>

      <div className={styles.content} style={{ padding: 16 }}>
        
        {/* Quick Actions Bar */}
        {isSuperAdmin && (
          <Card style={{ marginBottom: 16, background: '#1a0505', borderColor: '#4a0000', display: 'flex', gap: 12 }}>
            <strong style={{ color: '#ff4444', alignSelf: 'center' }}>SuperAdmin Override:</strong>
            <Button size="sm" variant="danger" onClick={handleBroadcast}>Global Broadcast</Button>
            <Button size="sm" variant="outline" onClick={handleSeed}>Inject DB Seed</Button>
            <Button size="sm" variant="outline" onClick={() => adminApi.exportMetrics().then(()=>toast('CSV Export logic triggered'))}>Export Dump</Button>
          </Card>
        )}

        {loading ? (
          <div>Decrypting system data...</div>
        ) : (
          <>
            {tab === 'users' && data.map(u => (
              <Card key={u.id} className={styles.cardRow} style={{ marginBottom: 8, display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <strong>{u.name}</strong> {u.role === 'SuperAdmin' && '👑'} {u.role === 'Admin' && '🛡️'}
                  <div style={{ fontSize: '0.8rem', color: 'gray' }}>{u.email} | {u.tier} ({u.trustScore} TP)</div>
                </div>
                {isSuperAdmin && u.id !== user.userId && (
                  <Button size="sm" variant="danger" onClick={() => handleDeleteUser(u.id)}>Delete</Button>
                )}
              </Card>
            ))}

            {tab === 'disputes' && (data.length === 0 ? <p>No Active Disputes</p> : data.map(d => (
              <Card key={d.id} className={styles.cardRow} style={{ marginBottom: 8 }}>
                 <div>
                    <strong>Dispute against Job {d.jobId?.split('-')[0]}</strong> - <span style={{ color: d.status === 'Resolved' ? 'green' : 'red' }}>{d.status}</span>
                    <div style={{ fontSize: '0.8rem', color: 'gray' }}>Raised By: {d.raisedByName}</div>
                    <p style={{ margin: '8px 0', fontSize: '0.9rem', fontStyle: 'italic' }}>"{d.reason}"</p>
                 </div>
                 {d.status !== 'Resolved' && (
                   <Button size="sm" onClick={() => handleResolveDispute(d.id)}>Pass Judgement</Button>
                 )}
              </Card>
            )))}

            {tab === 'payments' && (data.length === 0 ? <p>No Payments Logged</p> : data.map(p => (
              <Card key={p.id || Math.random()} style={{ marginBottom: 8 }}>
                 <div>ID: {p.id}</div>
              </Card>
            )))}

            {tab === 'metrics' && extraData && (
              <div style={{ display: 'grid', gap: 16, gridTemplateColumns: '1fr 1fr' }}>
                 <Card glow>
                   <h3 style={{ opacity: 0.7 }}>Platform Earnings</h3>
                   <h1 style={{ color: 'var(--success-color)', fontSize: '2.5rem' }}>
                     {fmt(extraData.earnings?.totalCollectedEarningFees || 0)}
                   </h1>
                 </Card>
                 <Card glow>
                   <h3 style={{ opacity: 0.7 }}>Total Marketplace Jobs</h3>
                   <h1 style={{ color: 'var(--brand-primary)', fontSize: '2.5rem' }}>
                     {extraData.jobs?.length || 0}
                   </h1>
                 </Card>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
