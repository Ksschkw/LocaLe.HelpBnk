import { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Search, SlidersHorizontal, Plus, MapPin } from 'lucide-react'
import { searchApi, categoriesApi, servicesApi } from '../../api'
import { useAuth } from '../../contexts/AuthContext'
import { useToast, fmt, tierEmoji, tierColor } from '../../hooks/useUtils'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import TopBar from '../../components/layout/TopBar'
import styles from './Discover.module.css'

export default function DiscoverPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()

  const [services, setServices] = useState([])
  const [categories, setCategories] = useState([])
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState('')
  const [autocompleteOpts, setAutocompleteOpts] = useState([])
  const [showAutocomplete, setShowAutocomplete] = useState(false)
  
  const [selectedCat, setSelectedCat] = useState(null)
  const [showFilters, setShowFilters] = useState(false)
  const [remoteOnly, setRemoteOnly] = useState(false)

  const searchRef = useRef(null)

  // 1. Load Initial Categories & Services
  const loadInitial = useCallback(async () => {
    setLoading(true)
    try {
      const [sRes, cRes] = await Promise.all([
        servicesApi.getAll({ isRemote: remoteOnly }), 
        categoriesApi.getAll()
      ])
      setServices(sRes.data || [])
      setCategories(cRes.data || [])
    } catch {
      toast('Failed to load marketplace data', 'error')
    } finally {
      setLoading(false)
    }
  }, [remoteOnly, toast])

  useEffect(() => { loadInitial() }, [loadInitial])

  // 2. Autocomplete Hook
  useEffect(() => {
    if (!query || query.length < 2) {
      setAutocompleteOpts([])
      return
    }
    const timer = setTimeout(async () => {
      try {
        const res = await searchApi.autocompleteServices(query)
        setAutocompleteOpts(res.data || [])
      } catch (err) {
        // Ignore autocomplete errors silently
      }
    }, 250)
    return () => clearTimeout(timer)
  }, [query])

  // 3. Perform Server-Side Search
  const performSearch = async (forcedQuery = query, catId = selectedCat) => {
    setLoading(true)
    setShowAutocomplete(false)
    try {
      if (!forcedQuery && !catId && !remoteOnly) {
        // Empty search revert to general feed
        const res = await servicesApi.getAll({ isRemote: remoteOnly })
        setServices(res.data)
        return
      }

      // Backend fuzzy search endpoint
      // Note: We'd realistically pass category or remoteOnly if the backend Search API supports it,
      // But looking at the schema, `/Search/services` takes query, lat, lon, radiusKm, isRemote.
      const res = await searchApi.services({
        query: forcedQuery || ' ', // API throws 400 if empty, so pass space if empty but filtering
        isRemote: remoteOnly
      })
      
      let filtered = res.data || []
      // Client fallback for Category if searchApi doesn't inherently filter it
      if (catId) {
        filtered = filtered.filter(s => s.categoryId === catId)
      }
      setServices(filtered)
    } catch (e) {
      if (e.response?.status === 400) {
         setServices([]) // Empty query strictness
      } else {
         toast('Search failed', 'error')
      }
    } finally {
      setLoading(false)
    }
  }

  // Handle Search Input Selection
  const handleSelectAutocomplete = (suggestion) => {
    setQuery(suggestion)
    performSearch(suggestion, selectedCat)
  }

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      performSearch(query, selectedCat)
    }
  }

  const handleCategorySelect = (catId) => {
    setSelectedCat(catId)
    performSearch(query, catId)
  }

  // Click outside autocomplete
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (searchRef.current && !searchRef.current.contains(event.target)) {
        setShowAutocomplete(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className={styles.page}>
      <TopBar back={false} title="LocaLe" action={
        <Button size="sm" icon={<Plus size={14} />} onClick={() => navigate('/create-service')}>Host Service</Button>
      } />

      <div className={styles.content}>
        {/* Updated Hero Banner (Minigame removed to Chat room) */}
        <div className={styles.hero}>
          <div className={styles.heroBg} />
          <div style={{ position: 'relative', zIndex: 1, textAlign: 'center', width: '100%', maxWidth: 600, margin: '0 auto' }}>
            <h1 className={styles.heroTitle}>Explore Your <span className="gradient-text">Neighborhood</span></h1>
            <p className={styles.heroSub}>Trusted local services, secured by smart escrow and vetted by reality.</p>
          </div>
        </div>

        {/* Server-Side Search Bar with Autocomplete */}
        <div className={styles.searchWrap} ref={searchRef}>
          <div className={styles.searchBar}>
            <Search size={18} className={styles.searchIcon} />
            <div style={{ position: 'relative', flex: 1 }}>
              <input
                className={styles.searchInput}
                placeholder="Search precise services (e.g. 'Plumbing')..."
                value={query}
                onChange={e => {
                  setQuery(e.target.value)
                  setShowAutocomplete(true)
                }}
                onKeyDown={handleKeyDown}
                onFocus={() => setShowAutocomplete(true)}
              />
              {/* Autocomplete Dropdown */}
              {showAutocomplete && autocompleteOpts.length > 0 && (
                 <div className={styles.autocompleteDropdown} style={{
                    position: 'absolute', top: '100%', left: 0, right: 0, 
                    backgroundColor: '#111827', border: '1px solid var(--border-color)',
                    borderRadius: 8, marginTop: 8, zIndex: 9999, boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
                    overflow: 'hidden'
                 }}>
                    {autocompleteOpts.map((opt, idx) => (
                      <div key={idx} 
                           style={{ padding: '12px 16px', cursor: 'pointer', borderBottom: '1px solid var(--border-color)', fontSize: '0.9rem' }}
                           onClick={() => handleSelectAutocomplete(opt)}
                           onMouseEnter={(e) => e.target.style.background = 'var(--bg-color)'}
                           onMouseLeave={(e) => e.target.style.background = 'transparent'}
                      >
                        <Search size={14} style={{ display: 'inline', marginRight: 8, opacity: 0.5 }}/>
                        {opt}
                      </div>
                    ))}
                 </div>
              )}
            </div>
            
            <button className={styles.filterBtn} onClick={() => setShowFilters(f => !f)}>
              <SlidersHorizontal size={16} />
            </button>
            <Button size="sm" onClick={() => performSearch(query, selectedCat)}>Search</Button>
          </div>

          {showFilters && (
            <div className={styles.filters}>
              <label className={styles.filterRow}>
                <span>Remote jobs only</span>
                <input type="checkbox" checked={remoteOnly} onChange={e => {
                   setRemoteOnly(e.target.checked)
                   // When toggle changes, refetch immediately
                   setTimeout(() => performSearch(query, selectedCat), 0)
                }} />
              </label>
            </div>
          )}
        </div>

        {/* Category Pills calling Server Search */}
        <div className={styles.catScroll}>
          <button
            className={[styles.catPill, !selectedCat ? styles.catActive : ''].join(' ')}
            onClick={() => handleCategorySelect(null)}
          >All</button>
          {categories.flatMap(c => [c, ...(c.subCategories || [])]).map(c => (
            <button
              key={c.id}
              className={[styles.catPill, selectedCat === c.id ? styles.catActive : ''].join(' ')}
              onClick={() => handleCategorySelect(c.id)}
            >{c.name}</button>
          ))}
        </div>

        {/* Services Feed mapping to API Real Results */}
        <h3 className={styles.sectionTitle}>Marketplace Feed</h3>

        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="skeleton" style={{ height: 110, marginBottom: 12 }} />
          ))
        ) : services.length === 0 ? (
          <div className={styles.empty}>
            <div style={{ fontSize: '3rem' }}>🔍</div>
            <p>No services found for this query.</p>
          </div>
        ) : (
          services.map(s => (
            <ServiceCard key={s.id} service={s} onClick={() => navigate(`/service/${s.id}`)} />
          ))
        )}
      </div>
    </div>
  )
}

function ServiceCard({ service: s, onClick }) {
  return (
    <Card onClick={onClick} className={styles.serviceCard}>
      <div className={styles.serviceTop}>
        <div className={styles.serviceMeta}>
          <div className={styles.serviceAvatar}>
            {(s.providerName || 'U').charAt(0)}
          </div>
          <div>
            <div className={styles.providerName}>{s.providerName}</div>
            <div className={styles.providerTier} style={{ color: tierColor(s.providerTier) }}>
              {tierEmoji(s.providerTier)} {s.providerTier} · {s.providerTrustScore} TP
            </div>
          </div>
        </div>
        <div className={styles.priceWrap}>
          <div className={styles.price}>{fmt(s.basePrice)}</div>
          {s.hourlyRate > 0 && <div className={styles.hourly}>{fmt(s.hourlyRate)}/hr</div>}
        </div>
      </div>

      <h3 className={styles.serviceTitle}>{s.title}</h3>
      <p className={styles.serviceDesc}>{s.description?.substring(0, 90)}{s.description?.length > 90 ? '…' : ''}</p>

      <div className={styles.serviceTags}>
        {s.isDiscoveryEnabled && <span className={styles.tagGreen}>✓ Verified</span>}
        {s.isRemote && <span className={styles.tagBlue}>🌐 Remote</span>}
        {s.areaName && <span className={styles.tagGray}><MapPin size={10} /> {s.areaName}</span>}
        <span className={styles.tagGray}>{s.trustPoints} pts</span>
      </div>
    </Card>
  )
}
