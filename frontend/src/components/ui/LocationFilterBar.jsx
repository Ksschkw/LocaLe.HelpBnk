import { useState } from 'react'
import { MapPin, Globe, Navigation, Building2, Home, Wifi } from 'lucide-react'
import LocationPicker from './LocationPicker'

/**
 * LocationFilterBar
 * 
 * A collapsible filter bar that lets users choose the scope of job/service discovery.
 * Modes: Global | Country | State | City | Neighbourhood (Radius) | Remote Only
 *
 * Props:
 *   filter   - current filter object
 *   onChange - callback(filterObj) when user changes scope
 */

const SCOPE_OPTIONS = [
  { key: 'global',        label: 'Global',          icon: Globe,       desc: 'All listings everywhere' },
  { key: 'country',       label: 'Country',          icon: Globe,       desc: 'Within your country' },
  { key: 'state',         label: 'State / Region',   icon: Building2,   desc: 'Within your state' },
  { key: 'city',          label: 'City / LGA',       icon: Building2,   desc: 'Within your city or LGA' },
  { key: 'neighbourhood', label: 'Neighbourhood',    icon: Home,        desc: 'Within 10 km of you' },
  { key: 'remote',        label: 'Remote Only',      icon: Wifi,        desc: 'Online / remote work only' },
]

export default function LocationFilterBar({ filter, onChange }) {
  const [locationData, setLocationData] = useState(filter?.locationData || null)
  const [scope, setScope] = useState(filter?.scope || 'global')
  const [expanded, setExpanded] = useState(false)

  const handleScopeChange = (newScope) => {
    setScope(newScope)
    const updated = buildFilter(newScope, locationData)
    onChange?.(updated)
  }

  const handleLocationChange = (locData) => {
    setLocationData(locData)
    const updated = buildFilter(scope, locData)
    onChange?.(updated)
  }

  const buildFilter = (sc, locData) => {
    if (sc === 'global')        return { scope: 'global', global: true }
    if (sc === 'remote')        return { scope: 'remote', remoteOnly: true }
    if (sc === 'neighbourhood') return { scope: 'neighbourhood', userLat: locData?.lat, userLon: locData?.lng, radiusKm: 10, locationData: locData }
    if (sc === 'city')          return { scope: 'city', city: locData?.city, state: locData?.state, country: locData?.country, locationData: locData }
    if (sc === 'state')         return { scope: 'state', state: locData?.state, country: locData?.country, locationData: locData }
    if (sc === 'country')       return { scope: 'country', country: locData?.country, locationData: locData }
    return { scope: 'global', global: true }
  }

  const needsLocation = ['neighbourhood', 'city', 'state', 'country'].includes(scope)
  const currentScopeOption = SCOPE_OPTIONS.find(o => o.key === scope)

  return (
    <div style={{ marginBottom: 16 }}>
      {/* Collapsed pill bar */}
      <button
        type="button"
        onClick={() => setExpanded(p => !p)}
        style={{
          display: 'flex', alignItems: 'center', gap: 8,
          padding: '8px 14px',
          background: scope === 'global' ? 'var(--bg-hover)' : 'var(--brand-soft)',
          border: `1px solid ${scope === 'global' ? 'var(--border)' : 'var(--brand-primary)'}`,
          borderRadius: 'var(--radius-full)',
          color: scope === 'global' ? 'var(--text-2)' : 'var(--brand-primary)',
          fontSize: '0.84rem', fontWeight: 600, cursor: 'pointer',
          transition: 'all 0.2s'
        }}
      >
        {currentScopeOption && <currentScopeOption.icon size={14} />}
        {scope === 'global' ? 'Global' :
         scope === 'remote' ? 'Remote Only' :
         locationData ? `${locationData[scope === 'neighbourhood' ? 'city' : scope] || locationData.city || 'Location set'}` :
         currentScopeOption?.label}
        <MapPin size={12} style={{ marginLeft: 2, opacity: 0.7 }} />
      </button>

      {/* Expanded panel */}
      {expanded && (
        <div style={{
          marginTop: 10, padding: 16,
          background: 'var(--bg-card)', border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-md)',
          animation: 'pageEnter 0.2s ease'
        }}>
          <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-3)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Discovery Scope
          </div>

          {/* Scope grid */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 8, marginBottom: 14 }}>
            {SCOPE_OPTIONS.map(opt => {
              const active = scope === opt.key
              return (
                <button
                  key={opt.key}
                  type="button"
                  onClick={() => handleScopeChange(opt.key)}
                  style={{
                    padding: '10px 8px',
                    background: active ? 'var(--brand-soft)' : 'var(--bg-hover)',
                    border: `1px solid ${active ? 'var(--brand-primary)' : 'var(--border)'}`,
                    borderRadius: 'var(--radius-md)',
                    color: active ? 'var(--brand-primary)' : 'var(--text-2)',
                    cursor: 'pointer',
                    display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 5,
                    fontSize: '0.78rem', fontWeight: 600,
                    transition: 'all 0.15s'
                  }}
                >
                  <opt.icon size={15} />
                  {opt.label}
                </button>
              )
            })}
          </div>

          {/* Radius slider for neighbourhood */}
          {scope === 'neighbourhood' && (
            <div style={{ marginBottom: 14 }}>
              <label style={{ fontSize: '0.8rem', color: 'var(--text-2)', display: 'flex', justifyContent: 'space-between' }}>
                <span>Radius</span>
                <span style={{ color: 'var(--brand-primary)', fontWeight: 700 }}>10 km</span>
              </label>
              <input
                type="range" min={1} max={100} defaultValue={10}
                onChange={e => {
                  const updated = buildFilter(scope, locationData)
                  onChange?.({ ...updated, radiusKm: Number(e.target.value) })
                }}
                style={{ width: '100%', marginTop: 6, accentColor: 'var(--brand-primary)' }}
              />
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.7rem', color: 'var(--text-3)' }}>
                <span>1 km</span><span>50 km</span><span>100 km</span>
              </div>
            </div>
          )}

          {/* Location search — only shown for scopes that need a place */}
          {needsLocation && (
            <div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-2)', marginBottom: 8 }}>
                {scope === 'neighbourhood' ? '📍 Your position:' :
                 scope === 'city' ? '🏙️ Select city:' :
                 scope === 'state' ? '📌 Select state:' :
                 '🌍 Select country:'}
              </div>
              <LocationPicker
                value={locationData ? { display: locationData.display } : null}
                onChange={handleLocationChange}
                placeholder={
                  scope === 'neighbourhood' ? 'Enter your street or area…' :
                  scope === 'city' ? 'Enter city or LGA…' :
                  scope === 'state' ? 'Enter state or region…' :
                  'Enter country…'
                }
              />
            </div>
          )}

          {/* Confirm button */}
          <button
            type="button"
            onClick={() => setExpanded(false)}
            style={{
              width: '100%', marginTop: 14, padding: '10px',
              background: 'var(--brand-primary)', color: 'white',
              borderRadius: 'var(--radius-md)', fontWeight: 700, fontSize: '0.9rem',
              border: 'none', cursor: 'pointer', transition: 'opacity 0.2s'
            }}
          >
            Apply Filter
          </button>
        </div>
      )}
    </div>
  )
}
