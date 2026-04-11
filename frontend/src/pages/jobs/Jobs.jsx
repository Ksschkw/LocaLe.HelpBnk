import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { jobsApi, bookingsApi } from '../../api'
import { useAuth } from '../../contexts/AuthContext'
import { useDialog } from '../../contexts/DialogContext'
import { useToast, fmt, timeAgo } from '../../hooks/useUtils'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import TopBar from '../../components/layout/TopBar'
import SortBar, { sortItems } from '../../components/ui/SortBar'
import styles from './Jobs.module.css'

const PRESET_CATEGORIES = [
  'All', 'Plumbing', 'Electrical', 'Cleaning', 'Delivery',
  'Carpentry', 'Painting', 'Tutoring', 'Security', 'Catering', 'Tech Support', 'Other'
]

export default function JobsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const { confirm } = useDialog()

  const [showPost, setShowPost] = useState(false)
  const [form, setForm] = useState({ title: '', description: '', amount: '', categoryName: '', _customCategory: false })
  const [posting, setPosting] = useState(false)
  const [tab, setTab] = useState('Marketplace')
  const [activeCategory, setActiveCategory] = useState('All')
  const [sortBy, setSortBy] = useState('newest')
  const [allJobs, setAllJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState(null)
  const [withdrawingId, setWithdrawingId] = useState(null)
  const [myBookings, setMyBookings] = useState([])

  const loadJobs = useCallback(async () => {
    setLoading(true)
    try {
      if (tab === 'Marketplace') {
        const [res, bRes] = await Promise.all([
          jobsApi.getAll(),
          bookingsApi.mine().catch(() => ({ data: [] }))
        ])
        const list = (res.data || []).filter(j => j.creatorId !== user?.userId)
        setAllJobs(list)
        setMyBookings(bRes.data || [])
      } else {
        const res = await jobsApi.myRequests()
        setAllJobs(res.data || [])
      }
    } catch (e) {
      toast(e?.response?.data?.Error || 'Failed to load jobs', 'error')
    } finally {
      setLoading(false)
    }
  }, [tab, user, toast])

  useEffect(() => { loadJobs() }, [loadJobs])

  // Client-side category filter + sort
  const jobs = useMemo(() => {
    let list = allJobs
    if (tab === 'Marketplace' && activeCategory !== 'All') {
      list = list.filter(j => j.categoryName === activeCategory)
    }
    return sortItems(list, sortBy)
  }, [allJobs, activeCategory, tab, sortBy])

  // Chips: preset + any extra from the feed
  const availableCategories = useMemo(() => {
    const fromFeed = new Set(allJobs.map(j => j.categoryName).filter(Boolean))
    const extras = [...fromFeed].filter(c => !PRESET_CATEGORIES.includes(c))
    return ['All', ...PRESET_CATEGORIES.filter(c => c !== 'All' && fromFeed.has(c)), ...extras]
  }, [allJobs])

  const set = k => e => setForm(f => ({ ...f, [k]: e.target.value }))

  const handlePost = async (e) => {
    e.preventDefault()
    if (Number(form.amount) < 500) { toast('Minimum budget is ₦500', 'error'); return }
    setPosting(true)
    try {
      await jobsApi.create({
        title: form.title,
        description: form.description,
        amount: Number(form.amount),
        categoryName: form.categoryName || undefined
      })
      toast('Job posted to network! ✓', 'success')
      setShowPost(false)
      setForm({ title: '', description: '', amount: '', categoryName: '', _customCategory: false })
      setTab('My Posted Jobs')
    } catch (e) {
      toast(e?.response?.data?.Error || e?.response?.data?.message || 'Failed to post', 'error')
    } finally { setPosting(false) }
  }

  const handleApply = async (e, jobId) => {
    e.stopPropagation()
    try {
      await bookingsApi.apply(jobId)
      toast('Application sent! ✓', 'success')
      loadJobs()
    } catch (e) {
      toast(e?.response?.data?.Error || e?.response?.data?.message || 'Already applied or not eligible', 'error')
    }
  }

  const handleWithdrawApplication = async (e, jobId) => {
    e.stopPropagation()
    const booking = myBookings.find(b => b.jobId === jobId)
    if (!booking) return
    const ok = await confirm({ title: 'Withdraw Application', message: 'Are you sure you want to withdraw your application for this job?', danger: true })
    if (!ok) return
    setWithdrawingId(jobId)
    try {
      await bookingsApi.delete(booking.id)
      toast('Application withdrawn.', 'success')
      loadJobs()
    } catch (e) {
      toast(e?.response?.data?.Error || e?.response?.data?.message || 'Could not withdraw', 'error')
    } finally { setWithdrawingId(null) }
  }

  const handleDeleteJob = async (e, jobId) => {
    e.stopPropagation()
    const ok = await confirm({ title: 'Delete Job', message: 'Are you sure you want to delete this job post? This cannot be undone.', danger: true })
    if (!ok) return
    setDeletingId(jobId)
    try {
      await jobsApi.delete(jobId)
      toast('Job deleted.', 'success')
      setAllJobs(prev => prev.filter(j => j.id !== jobId))
    } catch (e) {
      toast(e?.response?.data?.Error || e?.response?.data?.message || 'Cannot delete this job', 'error')
    } finally { setDeletingId(null) }
  }

  return (
    <div className={styles.page}>
      <TopBar back={false} title="Open Marketplace" action={
        <Button size="sm" onClick={() => setShowPost(s => !s)}>+ Request Help</Button>
      } />

      {showPost && (
        <div className={styles.postForm}>
          <h3>Describe What You Need</h3>
          <form onSubmit={handlePost} className={styles.form}>
            <input
              className={styles.input}
              placeholder="Job title, e.g. Fix generator"
              value={form.title} onChange={set('title')}
              required minLength={4}
            />
            <textarea
              className={styles.input} rows={3}
              placeholder="Describe what needs doing..."
              value={form.description} onChange={set('description')}
              required minLength={10}
            />
            <input
              className={styles.input} type="number"
              placeholder="Budget (₦ NGN), min ₦500"
              value={form.amount} onChange={set('amount')}
              required min={500}
            />
            <select
              className={styles.input}
              value={form._customCategory ? '__custom__' : form.categoryName}
              onChange={e => {
                if (e.target.value === '__custom__') {
                  setForm(f => ({ ...f, categoryName: '', _customCategory: true }))
                } else {
                  setForm(f => ({ ...f, categoryName: e.target.value, _customCategory: false }))
                }
              }}
              style={{ cursor: 'pointer' }}
            >
              <option value="">Select a Category (optional)</option>
              {PRESET_CATEGORIES.filter(c => c !== 'All').map(c => (
                <option key={c} value={c}>{c}</option>
              ))}
              <option value="__custom__">➕ Create new category…</option>
            </select>
            {form._customCategory && (
              <input
                className={styles.input}
                placeholder="Type your category name"
                value={form.categoryName}
                onChange={e => setForm(f => ({ ...f, categoryName: e.target.value }))}
                autoFocus
              />
            )}
            <Button type="submit" block loading={posting}>Broadcast to Network</Button>
          </form>
        </div>
      )}

      {/* TABS */}
      <div style={{
        display: 'flex', gap: 0, background: 'var(--surface-color)',
        borderBottom: '1px solid var(--border-color)',
        position: 'sticky', top: 60, zIndex: 10
      }}>
        {['Marketplace', 'My Posted Jobs'].map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            style={{
              padding: '12px 20px', background: 'none', border: 'none',
              borderBottom: tab === t ? '3px solid var(--brand-primary)' : '3px solid transparent',
              color: tab === t ? 'var(--brand-primary)' : 'var(--text-secondary)',
              fontWeight: tab === t ? '700' : '400',
              cursor: 'pointer', fontSize: '0.95rem', whiteSpace: 'nowrap'
            }}
          >{t}</button>
        ))}
      </div>

      {/* FILTERS: Category + Sort */}
      <div style={{ borderBottom: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', gap: 0 }}>
        {/* Category chips (marketplace only) */}
        {tab === 'Marketplace' && (
          <div style={{ display: 'flex', gap: 8, overflowX: 'auto', scrollbarWidth: 'none', padding: '8px 12px' }}>
            {availableCategories.map(cat => (
              <button
                key={cat}
                onClick={() => setActiveCategory(cat)}
                style={{
                  padding: '5px 14px', borderRadius: 20, border: '1px solid',
                  borderColor: activeCategory === cat ? 'var(--brand-primary)' : 'var(--border-color)',
                  background: activeCategory === cat ? 'rgba(0,136,255,0.15)' : 'transparent',
                  color: activeCategory === cat ? 'var(--brand-primary)' : 'var(--text-secondary)',
                  fontWeight: activeCategory === cat ? '700' : '400',
                  cursor: 'pointer', fontSize: '0.78rem', whiteSpace: 'nowrap',
                  flexShrink: 0, transition: 'all 0.15s'
                }}
              >{cat}</button>
            ))}
          </div>
        )}
        {/* Shared sort bar — shown for both tabs */}
        <SortBar sortBy={sortBy} onChange={setSortBy} style={{ borderBottom: 'none' }} />
      </div>

      <div className={styles.content}>
        {tab === 'Marketplace' && (
          <p className={styles.sub} style={{ marginBottom: 4, fontSize: '0.82rem' }}>
            {activeCategory === 'All'
              ? 'Browse active requests from buyers.'
              : `Showing jobs tagged "${activeCategory}"`}
          </p>
        )}

        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="skeleton" style={{ height: 110, marginBottom: 12 }} />
          ))
        ) : jobs.length === 0 ? (
          <div className={styles.empty}>
            <div style={{ fontSize: '3rem' }}>📋</div>
            <p>
              {tab === 'Marketplace'
                ? (activeCategory === 'All'
                    ? 'No open jobs right now. Be the first to post!'
                    : `No jobs tagged "${activeCategory}" yet.`)
                : "You haven't posted any jobs yet."}
            </p>
          </div>
        ) : jobs.map(j => {
          const myBooking = tab === 'Marketplace' ? myBookings.find(b => b.jobId === j.id) : null
          const alreadyApplied = !!myBooking

          return (
            <Card
              key={j.id}
              className={styles.jobCard}
              style={{ cursor: tab === 'My Posted Jobs' ? 'pointer' : 'default' }}
              onClick={() => { if (tab === 'My Posted Jobs') navigate(`/jobs/manage/${j.id}`) }}
            >
              <div className={styles.jobTop}>
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' }}>
                    <h3 className={styles.jobTitle} style={{ margin: 0 }}>{j.title}</h3>
                    {j.categoryName && (
                      <span style={{
                        background: 'rgba(0,136,255,0.15)', color: '#0088FF',
                        padding: '1px 8px', borderRadius: 10, fontSize: '0.7rem', fontWeight: 'bold'
                      }}>{j.categoryName}</span>
                    )}
                  </div>
                  <p className={styles.jobDesc}>
                    {j.description?.substring(0, 100)}{j.description?.length > 100 ? '…' : ''}
                  </p>
                  <span style={{ fontSize: '0.72rem', color: 'var(--text-3)' }}>
                    By {j.creatorName} · {timeAgo(j.createdAt)}
                  </span>
                </div>
                <div className={styles.jobAmt}>{fmt(j.amount)}</div>
              </div>
              <div className={styles.jobMeta}>
                {j.applicationCount > 0 && (
                  <span style={{
                    fontSize: '0.72rem', color: '#aaa',
                    background: 'rgba(255,255,255,0.06)',
                    padding: '2px 8px', borderRadius: 8
                  }}>{j.applicationCount} applied</span>
                )}
                {tab === 'Marketplace' ? (
                  alreadyApplied ? (
                    <Button size="sm" variant="outline" style={{ color: '#ff6666', borderColor: '#ff6666' }}
                      loading={withdrawingId === j.id}
                      onClick={(e) => handleWithdrawApplication(e, j.id)}>
                      Withdraw
                    </Button>
                  ) : (
                    <Button size="sm" variant="secondary" onClick={(e) => handleApply(e, j.id)}>
                      Apply Now
                    </Button>
                  )
                ) : (
                  <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                    <span style={{ fontSize: '0.8rem', fontWeight: 'bold', color: 'var(--brand-primary)' }}>
                      {j.applicationCount || 0} Applicant{j.applicationCount !== 1 ? 's' : ''} · Manage →
                    </span>
                    {(j.status === 'Open' || j.status === 'Completed') && (
                      <Button size="sm" variant="outline"
                        style={{ color: '#ff4444', borderColor: '#ff4444', fontSize: '0.72rem', padding: '3px 8px' }}
                        loading={deletingId === j.id}
                        onClick={(e) => handleDeleteJob(e, j.id)}>
                        Delete
                      </Button>
                    )}
                  </div>
                )}
              </div>
            </Card>
          )
        })}
      </div>
    </div>
  )
}
