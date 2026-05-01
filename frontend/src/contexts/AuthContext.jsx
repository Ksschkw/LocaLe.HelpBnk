import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { authApi, notificationsApi } from '../api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)
  const [notifications, setNotifications] = useState([])

  const fetchMe = useCallback(async () => {
    try {
      const { data } = await authApi.me()
      setUser(normalizeUser(data))
    } catch {
      setUser(null)
    } finally {
      setLoading(false)
    }
  }, [])

  const fetchNotifications = useCallback(async () => {
    if (!user) return
    try {
      const { data } = await notificationsApi.getAll()
      setNotifications(data)
    } catch { /* silent */ }
  }, [user])

  useEffect(() => { fetchMe() }, [fetchMe])
  useEffect(() => {
    if (user) { fetchNotifications() }
  }, [user, fetchNotifications])

  const login = async (email, password) => {
    const { data } = await authApi.login({ email, password })
    setUser(normalizeUser(data))
  }

  const register = async (name, email, password) => {
    const { data } = await authApi.register({ name, email, password })
    setUser(normalizeUser(data))
  }

  // Normalize backend user payloads (snake/camel/pascal inconsistencies)
  function normalizeUser(data) {
    if (!data) return null
    return {
      userId: data.userId ?? data.UserId ?? data.UserID ?? null,
      name: data.name ?? data.Name ?? '',
      email: data.email ?? data.Email ?? '',
      phone: data.phone ?? data.Phone ?? null,
      avatarUrl: data.avatarUrl ?? data.AvatarUrl ?? null,
      tier: data.tier ?? data.Tier ?? null,
      role: data.role ?? data.Role ?? 'User',
      trustScore: data.trustScore ?? data.TrustScore ?? 0,
      bio: data.bio ?? data.Bio ?? null,
      totalVouchPoints: data.totalVouchPoints ?? data.TotalVouchPoints ?? 0,
      jobsCompleted: data.jobsCompleted ?? data.JobsCompleted ?? 0,
      latitude: data.latitude ?? data.Latitude ?? null,
      longitude: data.longitude ?? data.Longitude ?? null,
      areaName: data.areaName ?? data.AreaName ?? null
    }
  }

  const logout = async () => {
    await authApi.logout()
    setUser(null)
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, notifications, unreadCount, fetchNotifications }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => useContext(AuthContext)
