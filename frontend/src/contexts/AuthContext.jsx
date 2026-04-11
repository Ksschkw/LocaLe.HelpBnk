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
      setUser(data)
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
    setUser(data)
  }

  const register = async (name, email, password) => {
    const { data } = await authApi.register({ name, email, password })
    setUser(data)
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
