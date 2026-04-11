import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { jobsApi, bookingsApi } from '../../api'
import { useDialog } from '../../contexts/DialogContext'
import { useToast, timeAgo, fmt } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import styles from './Jobs.module.css'

export default function ManageJobPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const toast = useToast()
  const { confirm } = useDialog()

  const [job, setJob] = useState(null)
  const [applicants, setApplicants] = useState([])
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const [jRes, aRes] = await Promise.all([
        jobsApi.getById(id),
        jobsApi.getApplicants(id)
      ])
      setJob(jRes.data)
      setApplicants(aRes.data || [])
    } catch (e) {
      toast(e.message || 'Failed to load job details', 'error')
    } finally {
      setLoading(false)
    }
  }, [id, toast])

  useEffect(() => { loadData() }, [loadData])

  const handleSelectApplicant = async (bookingId, providerName) => {
    const ok = await confirm({
      title: 'Confirm Hire',
      message: `Hire ${providerName}? This will lock ${job?.amount ? fmt(job.amount) : ''} from your wallet into Escrow.`,
      confirmText: 'Pay & Hire'
    })
    if (!ok) return

    setActionLoading(bookingId)
    try {
      await bookingsApi.confirm(bookingId)
      toast(`Escrow Secured! You hired ${providerName}. 🎉`, 'success')
      navigate(`/chat/${id}`)
    } catch (e) {
      toast(e?.response?.data?.message || e?.response?.data?.Error || 'Failed — check wallet balance', 'error')
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) return (
    <div className={styles.page}>
      <TopBar back title="Manage Job" />
      <div className={styles.content} style={{ display: 'flex', justifyContent: 'center', padding: 40, height: 'auto', gridTemplateColumns: '1fr' }}>
        <div className="spinner" />
      </div>
    </div>
  )

  if (!job) return (
    <div className={styles.page}>
      <TopBar back title="Manage Job" />
      <div className={styles.content} style={{ height: 'auto', gridTemplateColumns: '1fr' }}>
        <p style={{ textAlign: 'center', color: '#888', marginTop: 40 }}>Job not found.</p>
      </div>
    </div>
  )

  const statusColors = { Open: '#00EAFF', Assigned: '#FFD700', Completed: '#4CAF50', Closed: '#4CAF50', Cancelled: '#FF4444' }

  return (
    <div className={styles.page}>
      <TopBar back title="Manage Applicants" />

      <div style={{ padding: 16, maxWidth: 700, margin: '0 auto', width: '100%', display: 'flex', flexDirection: 'column', gap: 20 }}>
        {/* Job Card */}
        <Card style={{ border: '1px solid rgba(0,234,255,0.3)', background: 'linear-gradient(145deg,#1a1a1a,#0a0a0a)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
            <div style={{ flex: 1 }}>
              <h2 style={{ marginBottom: 6 }}>{job.title}</h2>
              {job.categoryName && (
                <span style={{ display: 'inline-block', background: 'rgba(0,136,255,0.15)', color: '#0088FF', padding: '2px 10px', borderRadius: 12, fontSize: '0.75rem', fontWeight: 'bold', marginBottom: 8 }}>
                  {job.categoryName}
                </span>
              )}
              <p style={{ fontSize: '0.9rem', color: '#ccc', lineHeight: 1.6 }}>{job.description}</p>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: '1.5rem', fontWeight: 900, color: '#4CAF50' }}>{fmt(job.amount)}</div>
              <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: '0.75rem', fontWeight: 'bold', background: 'rgba(0,0,0,0.4)', color: statusColors[job.status] || '#888' }}>
                {job.status}
              </span>
            </div>
          </div>
        </Card>

        {/* Applicants */}
        <div>
          <h3 style={{ marginBottom: 14, display: 'flex', alignItems: 'center', gap: 10 }}>
            Applicants
            <span style={{ background: '#0088FF', color: '#fff', fontSize: '0.75rem', fontWeight: 'bold', padding: '2px 8px', borderRadius: 12 }}>
              {applicants.length}
            </span>
          </h3>

          {applicants.length === 0 ? (
            <Card style={{ textAlign: 'center', padding: '40px 20px' }}>
              <div style={{ fontSize: '3rem', marginBottom: 12 }}>🕵️‍♂️</div>
              <p style={{ color: '#888' }}>No applicants yet.</p>
              <p style={{ fontSize: '0.8rem', color: '#555', marginTop: 6 }}>You'll be notified when providers apply.</p>
            </Card>
          ) : applicants.map(b => (
            <Card key={b.id} style={{ marginBottom: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
              <div>
                <h4 style={{ fontSize: '1rem', marginBottom: 4 }}>{b.providerName}</h4>
                <span style={{ fontSize: '0.75rem', color: '#888' }}>Applied {timeAgo(b.createdAt)}</span>
              </div>
              <div style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
                <Button variant="secondary" size="sm" onClick={() => navigate(`/profile/${b.providerId}`)}>
                  View Profile
                </Button>
                {job.status === 'Open' && (
                  <Button
                    variant="primary"
                    size="sm"
                    loading={actionLoading === b.id}
                    disabled={!!actionLoading && actionLoading !== b.id}
                    onClick={() => handleSelectApplicant(b.id, b.providerName)}
                  >
                    Choose & Pay
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}
