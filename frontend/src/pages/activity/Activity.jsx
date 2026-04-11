import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { jobsApi, bookingsApi } from '../../api'
import { useAuth } from '../../contexts/AuthContext'
import { useToast, fmt, timeAgo } from '../../hooks/useUtils'
import Card from '../../components/ui/Card'
import TopBar from '../../components/layout/TopBar'
import SortBar, { sortItems } from '../../components/ui/SortBar'
import styles from './Activity.module.css'

export default function ActivityPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()

  const [tab, setTab] = useState('hiring')
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [sortBy, setSortBy] = useState('newest')

  const load = useCallback(async () => {
    if (!user) return
    setLoading(true)
    try {
      const [reqRes, svcReqRes, bookRes] = await Promise.all([
        jobsApi.myRequests(),
        jobsApi.myServiceRequests(),
        bookingsApi.mine()
      ])

      const myJobs     = reqRes.data  || []
      const mySvcJobs  = svcReqRes.data || []
      const allBookings = bookRes.data || []

      if (tab === 'hiring') {
        setItems(myJobs)
      } else if (tab === 'delivering') {
        const providerBookings = allBookings.filter(b => b.providerId === user?.userId && b.status !== 'Pending')
        const map = new Map()
        mySvcJobs.forEach(j => map.set(j.id, { ...j, _type: 'svcReq' }))
        providerBookings.forEach(b => {
          if (!map.has(b.jobId)) {
            map.set(b.jobId, {
              id: b.jobId,
              title: b.jobTitle,
              amount: b.jobAmount,
              status: b.status,
              createdAt: b.createdAt,
              _type: 'booking',
              _bookingId: b.id
            })
          }
        })
        setItems(Array.from(map.values()))
      } else if (tab === 'offers') {
        const pending = allBookings.filter(b => b.providerId === user?.userId && b.status === 'Pending')
        setItems(pending.map(b => ({
          id: b.jobId,
          title: b.jobTitle,
          amount: b.jobAmount,
          status: b.status,
          _type: 'pending',
          _bookingId: b.id,
          createdAt: b.createdAt
        })))
      }
    } catch (e) {
      toast('Failed to load activity', 'error')
    } finally {
      setLoading(false)
    }
  }, [tab, toast, user])

  useEffect(() => { load() }, [load])

  // Reset sort when switching tabs
  useEffect(() => { setSortBy('newest') }, [tab])

  const sorted = useMemo(() => sortItems(items, sortBy), [items, sortBy])

  const statusMeta = (status) => {
    const s = (status || '').toLowerCase()
    if (s === 'open')     return { label: 'Open',     color: '#888' }
    if (s === 'assigned') return { label: 'Assigned', color: '#FFD700' }
    if (s === 'active')   return { label: 'Active',   color: '#FFD700' }
    if (s === 'completed' || s === 'released' || s === 'finalized') return { label: 'Closed', color: '#4CAF50' }
    if (s === 'cancelled') return { label: 'Cancelled', color: '#FF4444' }
    if (s === 'indispute' || s === 'disputed') return { label: 'In Dispute', color: '#FF6B00' }
    if (s === 'pending')  return { label: 'Pending Approval', color: '#0088FF' }
    return { label: status, color: '#888' }
  }

  const TABS = [
    { id: 'hiring',    label: '🛒 Hiring' },
    { id: 'delivering', label: '🔧 Delivering' },
    { id: 'offers',    label: '📋 My Offers' },
  ]

  return (
    <div className={styles.page}>
      <TopBar back={false} title="Active Contracts" />

      <div className={styles.tabs}>
        {TABS.map(t => (
          <button
            key={t.id}
            className={[styles.tab, tab === t.id ? styles.tabActive : ''].join(' ')}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Sort bar — always visible for all tabs */}
      <SortBar sortBy={sortBy} onChange={setSortBy} />

      <div className={styles.content}>
        {loading ? (
          Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="skeleton" style={{ height: 110, marginBottom: 12 }} />
          ))
        ) : sorted.length === 0 ? (
          <div className={styles.empty}>
            <div style={{ fontSize: '3rem' }}>📋</div>
            <p>
              {tab === 'hiring'
                ? "You haven't posted any jobs yet. Use Request Help on the Jobs page."
                : tab === 'delivering'
                ? 'No active gigs yet.'
                : 'No pending applications.'}
            </p>
          </div>
        ) : sorted.map(j => {
          const { label: statusLabel, color: statusColor } = statusMeta(j.status)
          const jobId = j.id

          return (
            <Card
              key={j.id + (j._bookingId || '')}
              className={styles.bookingCard}
              onClick={() => navigate(`/chat/${jobId}`)}
              style={{ cursor: 'pointer' }}
            >
              <div className={styles.bookingTop}>
                <h3 className={styles.jobTitle}>{j.title || 'Untitled'}</h3>
                <span style={{
                  padding: '3px 10px', borderRadius: 12, fontSize: '0.72rem',
                  fontWeight: 'bold', background: 'rgba(0,0,0,0.35)', color: statusColor,
                  border: `1px solid ${statusColor}40`
                }}>
                  {statusLabel}
                </span>
              </div>

              <div className={styles.bookingMeta}>
                <span style={{ fontWeight: 700, color: '#4CAF50' }}>{fmt(j.amount)}</span>
                {j.createdAt && <span style={{ fontSize: '0.75rem', opacity: 0.6 }}>{timeAgo(j.createdAt)}</span>}
                <span style={{ fontSize: '0.8rem', opacity: 0.5 }}>Tap to open Contract Room ›</span>
              </div>
            </Card>
          )
        })}
      </div>
    </div>
  )
}
