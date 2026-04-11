import { useState, useEffect, useCallback } from 'react'

// --- Global Toast ---
let toastTimer = null
export function useToast() {
  const show = useCallback((msg, type = 'default', duration = 2800) => {
    const existing = document.querySelector('.toast')
    if (existing) existing.remove()
    clearTimeout(toastTimer)
    const el = document.createElement('div')
    el.className = `toast ${type}`
    el.textContent = msg
    document.body.appendChild(el)
    requestAnimationFrame(() => el.classList.add('show'))
    toastTimer = setTimeout(() => {
      el.classList.remove('show')
      setTimeout(() => el.remove(), 300)
    }, duration)
  }, [])
  return show
}

// --- Async data fetcher ---
export function useAsync(fn, deps = []) {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const run = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await fn()
      setData(res.data)
    } catch (e) {
      setError(e?.response?.data?.message || e.message || 'Something went wrong')
    } finally {
      setLoading(false)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  useEffect(() => { run() }, [run])

  return { data, loading, error, refetch: run }
}

// --- Currency formatter ---
export const fmt = (n) => `₦${Number(n || 0).toLocaleString('en-NG')}`

// --- Tier color ---
export const tierColor = (tier) => ({
  Bronze: '#CD7F32', Silver: '#A8A9AD', Gold: '#FFD700', Platinum: '#E5E4E2'
}[tier] || '#8B96B0')

// --- Tier badge ---
export const tierEmoji = (tier) => ({
  Bronze: '🥉', Silver: '🥈', Gold: '🥇', Platinum: '💎'
}[tier] || '⭐')

// --- Relative time ---
export function timeAgo(date) {
  const s = Math.floor((Date.now() - new Date(date)) / 1000)
  if (s < 60) return 'just now'
  if (s < 3600) return `${Math.floor(s / 60)}m ago`
  if (s < 86400) return `${Math.floor(s / 3600)}h ago`
  return `${Math.floor(s / 86400)}d ago`
}
