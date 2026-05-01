import { useState, useEffect } from 'react'
import { MapPin, Wifi, Globe } from 'lucide-react'
import { Country, State, City } from 'country-state-city'

// Dropdown-only LocationPicker using country-state-city package.
// Props:
//  value - current location object
//  onChange - callback(locationObj)
//  placeholder - unused for dropdowns
//  isRemote / onRemoteChange - optional remote toggle
export default function LocationPicker({ value, onChange, isRemote, onRemoteChange }) {
  const [countries, setCountries] = useState([])
  const [states, setStates] = useState([])
  const [cities, setCities] = useState([])

  const [countryCode, setCountryCode] = useState(value?.countryCode || '')
  const [stateCode, setStateCode] = useState(value?.stateCode || '')
  const [cityName, setCityName] = useState(value?.city || '')
  const [loadingDetect, setLoadingDetect] = useState(false)

  useEffect(() => {
    const all = Country.getAllCountries() || []
    setCountries(all)
  }, [])

  useEffect(() => {
    if (!countryCode) { setStates([]); setStateCode(''); return }
    const s = State.getStatesOfCountry(countryCode) || []
    setStates(s)
    // reset downstream
    setStateCode('')
    setCities([])
    setCityName('')
  }, [countryCode])

  useEffect(() => {
    if (!countryCode || !stateCode) { setCities([]); setCityName(''); return }
    const c = City.getCitiesOfState(countryCode, stateCode) || []
    setCities(c)
    setCityName('')
  }, [countryCode, stateCode])

  // Sync external value -> select codes if provided
  useEffect(() => {
    if (!value) return
    if (value.countryCode) setCountryCode(value.countryCode)
    if (value.stateCode) setStateCode(value.stateCode)
    if (value.city) setCityName(value.city)
  }, [value?.countryCode, value?.stateCode, value?.city])

  const emitChange = (cCode, sCode, cName) => {
    const countryObj = countries.find(c => c.isoCode === cCode) || null
    const stateObj = states.find(s => s.isoCode === sCode) || null
    const cityObj = cities.find(ci => ci.name === cName) || null

    const loc = {
      display: [cName, stateObj?.name, countryObj?.name].filter(Boolean).join(', '),
      country: countryObj?.name || null,
      countryCode: countryObj?.isoCode || null,
      state: stateObj?.name || null,
      stateCode: stateObj?.isoCode || null,
      city: cName || null,
      lat: cityObj?.latitude ? parseFloat(cityObj.latitude) : null,
      lng: cityObj?.longitude ? parseFloat(cityObj.longitude) : null
    }
    onChange?.(loc)
  }

  const handleDetect = async () => {
    setLoadingDetect(true)
    try {
      const ipRes = await fetch('https://ipapi.co/json/')
      if (ipRes.ok) {
        const ipData = await ipRes.json()
        const countryName = ipData.country_name || ipData.country
        const region = ipData.region || ipData.region_code || ''
        const city = ipData.city || ''

        if (countryName) {
          const matched = countries.find(c => c.name.toLowerCase() === countryName.toLowerCase() || (c.isoCode && c.isoCode.toLowerCase() === (ipData.country || '').toLowerCase()))
          if (matched) {
            setCountryCode(matched.isoCode)
            // try to match state by name
            const stateList = State.getStatesOfCountry(matched.isoCode) || []
            const matchedState = stateList.find(s => s.name.toLowerCase() === (region || '').toLowerCase())
            if (matchedState) {
              setStateCode(matchedState.isoCode)
              const cityList = City.getCitiesOfState(matched.isoCode, matchedState.isoCode) || []
              const matchedCity = cityList.find(ci => ci.name.toLowerCase() === (city || '').toLowerCase())
              if (matchedCity) {
                setCities(cityList)
                setCityName(matchedCity.name)
                emitChange(matched.isoCode, matchedState.isoCode, matchedCity.name)
                setLoadingDetect(false)
                return
              }
              // no exact city match; set state and leave city for user
              setCities(cityList)
              emitChange(matched.isoCode, matchedState?.isoCode || '', city || '')
              setLoadingDetect(false)
              return
            }
            // no state matched; set country only
            setStates(State.getStatesOfCountry(matched.isoCode) || [])
            emitChange(matched.isoCode, '', city || '')
            setLoadingDetect(false)
            return
          }
        }
      }
    } catch (e) {
      // ignore
    } finally {
      setLoadingDetect(false)
    }
  }

  return (
    <div style={{ width: '100%' }}>
      {onRemoteChange && (
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10, cursor: 'pointer', width: 'fit-content' }}>
          <div
            onClick={() => onRemoteChange(!isRemote)}
            style={{
              width: 40, height: 22, borderRadius: 11,
              background: isRemote ? 'var(--brand-primary)' : 'var(--bg-hover)',
              border: '1px solid var(--border)',
              position: 'relative', transition: 'background 0.2s'
            }}
          >
            <div style={{
              width: 16, height: 16, borderRadius: 8, background: 'white',
              position: 'absolute', top: 2,
              left: isRemote ? 20 : 2,
              transition: 'left 0.2s'
            }} />
          </div>
          <span style={{ fontSize: '0.85rem', color: isRemote ? 'var(--brand-primary)' : 'var(--text-2)', display: 'flex', alignItems: 'center', gap: 4 }}>
            <Globe size={14} /> Remote / Online job
          </span>
        </label>
      )}

      {!isRemote && (
        <div style={{ display: 'grid', gap: 10 }}>
          <div style={{ display: 'flex', gap: 8 }}>
            <div style={{ flex: 1 }}>
              <label style={{ display: 'block', fontSize: 12, color: 'var(--text-2)', marginBottom: 6 }}>Country</label>
              <select value={countryCode} onChange={e => setCountryCode(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid var(--border)' }}>
                <option value="">Select country...</option>
                {countries.map(c => (
                  <option key={c.isoCode} value={c.isoCode}>{c.name}</option>
                ))}
              </select>
            </div>

            <div style={{ flex: 1 }}>
              <label style={{ display: 'block', fontSize: 12, color: 'var(--text-2)', marginBottom: 6 }}>State / Region</label>
              <select value={stateCode} onChange={e => setStateCode(e.target.value)} disabled={!states.length} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid var(--border)' }}>
                <option value="">Select state...</option>
                {states.map(s => (
                  <option key={s.isoCode} value={s.isoCode}>{s.name}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: 12, color: 'var(--text-2)', marginBottom: 6 }}>City / Town</label>
            <select value={cityName} onChange={e => { setCityName(e.target.value); emitChange(countryCode, stateCode, e.target.value) }} disabled={!cities.length} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid var(--border)' }}>
              <option value="">Select city...</option>
              {cities.map(ci => (
                <option key={`${ci.name}-${ci.latitude}-${ci.longitude}`} value={ci.name}>{ci.name}</option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
            <button type="button" onClick={handleDetect} style={{ background: 'none', border: '1px solid var(--border)', padding: '8px 10px', borderRadius: 8, cursor: 'pointer' }}>
              {loadingDetect ? <div className="spinner" style={{ width: 14, height: 14, borderWidth: 1.5 }} /> : <><MapPin size={14} style={{ marginRight: 8 }} /> Detect</>}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
