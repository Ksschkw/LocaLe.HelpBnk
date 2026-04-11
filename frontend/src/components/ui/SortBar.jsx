import { useMemo } from 'react'

export const SORT_OPTIONS = [
  { value: 'newest',  label: '🕒 Newest' },
  { value: 'oldest',  label: '📅 Oldest' },
  { value: 'az',      label: '🔤 A → Z' },
  { value: 'za',      label: '🔤 Z → A' },
  { value: 'highest', label: '💰 Highest' },
  { value: 'lowest',  label: '💸 Lowest' },
]

export function sortItems(items, sort, amountKey = 'amount', titleKey = 'title', dateKey = 'createdAt') {
  const arr = [...items]
  switch (sort) {
    case 'oldest':  return arr.sort((a, b) => new Date(a[dateKey]) - new Date(b[dateKey]))
    case 'az':      return arr.sort((a, b) => (a[titleKey] || '').localeCompare(b[titleKey] || ''))
    case 'za':      return arr.sort((a, b) => (b[titleKey] || '').localeCompare(a[titleKey] || ''))
    case 'highest': return arr.sort((a, b) => (b[amountKey] || 0) - (a[amountKey] || 0))
    case 'lowest':  return arr.sort((a, b) => (a[amountKey] || 0) - (b[amountKey] || 0))
    default:        return arr.sort((a, b) => new Date(b[dateKey]) - new Date(a[dateKey])) // newest
  }
}

export default function SortBar({ sortBy, onChange, style }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 8,
      overflowX: 'auto', scrollbarWidth: 'none', padding: '6px 12px',
      borderBottom: '1px solid var(--border-color)',
      ...style
    }}>
      <span style={{ fontSize: '0.72rem', color: 'var(--text-secondary)', flexShrink: 0, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>Sort:</span>
      {SORT_OPTIONS.map(o => (
        <button
          key={o.value}
          onClick={() => onChange(o.value)}
          style={{
            padding: '4px 12px', borderRadius: 20, border: '1px solid',
            borderColor: sortBy === o.value ? 'var(--brand-primary)' : 'var(--border-color)',
            background: sortBy === o.value ? 'rgba(0,136,255,0.12)' : 'transparent',
            color: sortBy === o.value ? 'var(--brand-primary)' : 'var(--text-secondary)',
            fontWeight: sortBy === o.value ? '700' : '400',
            cursor: 'pointer', fontSize: '0.75rem', whiteSpace: 'nowrap', flexShrink: 0,
            transition: 'all 0.15s'
          }}
        >{o.label}</button>
      ))}
    </div>
  )
}
