import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { servicesApi, vouchApi, jobsApi, waitlistsApi, categoriesApi } from '../../api'
import { useToast, fmt, tierColor, tierEmoji } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Button from '../../components/ui/Button'
import Card from '../../components/ui/Card'
import { MapPin, ShieldCheck, MessageSquare } from 'lucide-react'
import styles from './Service.module.css'

export default function ServicePage() {
  const { id } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()

  const [service, setService] = useState(null)
  const [related, setRelated] = useState([])
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [vouching, setVouching] = useState(false)

  const load = useCallback(async () => {
    try {
      const { data } = await servicesApi.getById(id)
      setService(data)
      try {
        const relRes = await categoriesApi.getServices(data.categoryId)
        setRelated(relRes.data.filter(s => s.id !== id).slice(0, 3))
      } catch (e) {
        // Ignore related fetch failure
      }
    } catch {
      toast('Failed to load service', 'error')
      navigate('/discover')
    } finally {
      setLoading(false)
    }
  }, [id, toast, navigate])

  useEffect(() => { load() }, [load])

  const handleVouch = async () => {
    setVouching(true)
    try {
      await vouchApi.vouch(id, "I vouch for this service!")
      toast('Vouch recorded! Thanks for supporting the community.', 'success')
      load() // Refresh points
    } catch (err) {
      toast(err?.response?.data?.message || 'Failed to vouch', 'error')
    } finally {
      setVouching(false)
    }
  }

  const handleBook = async () => {
    if (!user) return navigate('/auth')
    setActionLoading('book')
    try {
      const res = await jobsApi.requestService(id, { 
         title: `Request for ${service.title}`,
         description: `Direct booking request from catalog.`,
         amount: service.basePrice
      })
      toast('Booking Requested! Directing to Contact Room...', 'success')
      navigate(`/chat/${res.data.id}`)
    } catch (err) {
      toast(err?.response?.data?.message || 'Failed to request booking', 'error')
    } finally {
      setActionLoading(false)
    }
  }

  const handleWaitlist = async () => {
    if (!user) return navigate('/auth')
    setActionLoading('wait')
    try {
      await waitlistsApi.join(id, "Please add me to your waitlist")
      toast('Joined Waitlist successfully! Provider will be notified.', 'success')
    } catch (err) {
      toast(err?.response?.data?.message || 'Failed to join waitlist', 'error')
    } finally {
      setActionLoading(false)
    }
  }

  if (loading || !service) return (
    <div className={styles.page}>
      <TopBar back title="Loading..." />
      <div style={{ padding: 24 }}><div className="skeleton" style={{ height: 300 }} /></div>
    </div>
  )

  const isOwner = user?.id === service.providerId || user?.userId === service.providerId

  return (
    <div className={styles.page}>
      <TopBar back title="Service Details" />

      <div className={styles.content}>
        <Card className={styles.headerCard}>
          <div className={styles.providerMeta}>
            <div className={styles.avatar}>{(service.providerName || 'U').charAt(0)}</div>
            <div>
              <div className={styles.providerName}>{service.providerName}</div>
              <div style={{ color: tierColor(service.providerTier), fontSize: '0.85rem', fontWeight: 600 }}>
                {tierEmoji(service.providerTier)} {service.providerTier} · {service.providerTrustScore} TP
              </div>
            </div>
          </div>
          
          <h1 className={styles.title}>{service.title}</h1>
          
          <div className={styles.tags}>
            {service.isDiscoveryEnabled && <span className={styles.tagBlue}><ShieldCheck size={12}/> Verified</span>}
            {service.isRemote && <span className={styles.tagBlue}>🌐 Remote</span>}
            {service.areaName && <span className={styles.tagGray}><MapPin size={12} /> {service.areaName}</span>}
            <span className={styles.tagGreen}>{service.trustPoints} Trust Points</span>
          </div>
          
          <div className={styles.priceRow}>
            <div className={styles.price}>{fmt(service.basePrice)}</div>
            {service.hourlyRate > 0 && <div className={styles.hourly}>{fmt(service.hourlyRate)}/hr</div>}
          </div>
        </Card>

        <Card className={styles.descCard}>
          <h3>About this service</h3>
          <p>{service.description}</p>
        </Card>

        {/* Action Panel */}
        <div className={styles.actionPanel}>
          {!isOwner && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <Button block variant="primary" size="lg" icon={<MessageSquare size={18} />} loading={actionLoading === 'book'} onClick={handleBook}>
                Contact & Book
              </Button>
              <Button block variant="outline" size="sm" loading={actionLoading === 'wait'} onClick={handleWaitlist}>
                Join Waitlist
              </Button>
            </div>
          )}

          <div className={styles.vouchBox}>
            <div className={styles.vouchInfo}>
              <h4>Community Trust</h4>
              <p>This service has {service.trustPoints} points. Let's keep ranking it up!</p>
            </div>
            {!isOwner && (
              <Button block loading={vouching} onClick={handleVouch}>
                + Vouch for {service.providerName}
              </Button>
            )}
          </div>
        </div>

        {related.length > 0 && (
          <div style={{ marginTop: 32 }}>
             <h3 style={{ marginBottom: 16 }}>Similar Services</h3>
             <div style={{ display: 'grid', gap: 16, gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))' }}>
               {related.map(r => (
                 <Card key={r.id} onClick={() => {
                     navigate(`/service/${r.id}`); window.scrollTo(0,0)
                 }} style={{ cursor: 'pointer' }}>
                   <div style={{ fontWeight: 600, marginBottom: 4 }}>{r.title}</div>
                   <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>{fmt(r.basePrice)}</div>
                 </Card>
               ))}
             </div>
          </div>
        )}
      </div>
    </div>
  )
}
