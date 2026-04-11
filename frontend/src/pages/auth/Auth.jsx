import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useToast } from '../../hooks/useUtils'
import Button from '../../components/ui/Button'
import Input from '../../components/ui/Input'
import styles from './Auth.module.css'

const SLIDES = [
  { emoji: '🛡️', title: 'Zero Scams', desc: 'Money is locked until you confirm the job is done. No exceptions.' },
  { emoji: '📍', title: 'Your Neighborhood', desc: 'Find trusted service providers within walking distance of home.' },
  { emoji: '⚡', title: 'Instant Payout', desc: 'Provider gets paid in under 5 minutes after QR verification.' },
]

const LogoIcon = () => (
  <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg" style={{ marginBottom: -8 }}>
    <path d="M24 8L40 36H8L24 8Z" stroke="var(--brand-primary)" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
)

export default function AuthPage() {
  const { login, register, user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const [mode, setMode] = useState('landing') // landing | login | register
  const [loading, setLoading] = useState(false)
  const [slide, setSlide] = useState(0)
  const [form, setForm] = useState({ name: '', email: '', password: '', confirm: '' })
  const [errors, setErrors] = useState({})

  useEffect(() => {
    if (user) navigate('/discover', { replace: true })
  }, [user, navigate])

  useEffect(() => {
    const t = setInterval(() => setSlide(s => (s + 1) % SLIDES.length), 3500)
    return () => clearInterval(t)
  }, [])

  const set = (k) => (e) => setForm(f => ({ ...f, [k]: e.target.value }))

  const handleLogin = async (e) => {
    e.preventDefault()
    if (!form.email || !form.password) return
    setLoading(true)
    try {
      await login(form.email, form.password)
      navigate('/discover', { replace: true })
    } catch (err) {
      toast(err.message || 'Login failed', 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleRegister = async (e) => {
    e.preventDefault()
    const errs = {}
    if (!form.name) errs.name = 'Name is required'
    if (!form.email) errs.email = 'Email is required'
    if (form.password.length < 6) errs.password = 'Minimum 6 characters'
    if (form.password !== form.confirm) errs.confirm = 'Passwords do not match'
    if (Object.keys(errs).length) { setErrors(errs); return }
    setLoading(true)
    try {
      await register(form.name, form.email, form.password)
      navigate('/discover', { replace: true })
    } catch (err) {
      toast(err.message || 'Registration failed', 'error')
    } finally {
      setLoading(false)
    }
  }

  const inFormMode = mode === 'login' || mode === 'register'

  return (
    <div className={styles.authContainer}>
      {/* Left Panel: Storytelling (Visible on Desktop always, Visible on Mobile if mode === 'landing') */}
      <div className={[styles.leftPanel, inFormMode ? styles.hidden : styles.visible].join(' ')}>
        <div className={styles.glow1} />
        <div className={styles.glow2} />
        
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', zIndex: 1, gap: 4 }}>
          <LogoIcon />
          <div className={styles.logo}>LocaLe<span className={styles.dot}>.</span></div>
        </div>

        <div className={styles.slideArea}>
          {SLIDES.map((s, i) => (
            <div key={i} className={[styles.slide, i === slide ? styles.slideActive : ''].join(' ')}>
              <div className={styles.slideEmoji}>{s.emoji}</div>
              <h2 className={styles.slideTitle}>{s.title}</h2>
              <p className={styles.slideDesc}>{s.desc}</p>
            </div>
          ))}
          <div className={styles.dots}>
            {SLIDES.map((_, i) => (
              <button key={i} onClick={() => setSlide(i)}
                className={[styles.dotBtn, i === slide ? styles.dotActive : ''].join(' ')} />
            ))}
          </div>
        </div>

        <div className={styles.actions}>
          <Button variant="primary" block size="lg" onClick={() => setMode('login')}>Log In</Button>
          <Button variant="ghost" block size="lg" onClick={() => setMode('register')}>Create Account</Button>
        </div>
      </div>

      {/* Right Panel: Forms (Visible on Desktop always, Visible on Mobile if inFormMode) */}
      <div className={[styles.rightPanel, mode === 'landing' ? styles.hidden : styles.visible].join(' ')}>
        {/* Mobile Back Button */}
        <button className={styles.back} onClick={() => setMode('landing')}>←</button>

        <div className={styles.authCard}>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, marginBottom: 20 }}>
            <LogoIcon />
            <div className={styles.authLogo} style={{ marginBottom: 0 }}>LocaLe<span className={styles.dot}>.</span></div>
          </div>
          <h2 className={styles.authTitle}>{mode === 'login' || mode === 'landing' ? 'Welcome Back' : 'Join LocaLe'}</h2>
          <p className={styles.authSub}>{(mode === 'login' || mode === 'landing') ? 'Sign in to your account' : 'Your trusted neighborhood network'}</p>

          {(mode === 'login' || mode === 'landing') ? (
            <form onSubmit={handleLogin} className={styles.form}>
              <Input label="Email" type="email" value={form.email} onChange={set('email')} placeholder="you@locale.ng" required error={errors.email} />
              <Input label="Password" type="password" value={form.password} onChange={set('password')} placeholder="••••••••" required error={errors.password} />
              <Button type="submit" block size="lg" loading={loading} variant="primary">Authenticate</Button>
              <p className={styles.switch}>No account? <button type="button" onClick={() => setMode('register')}>Sign up</button></p>
            </form>
          ) : (
            <form onSubmit={handleRegister} className={styles.form}>
              <Input label="Full Name" value={form.name} onChange={set('name')} placeholder="Jane Doe" required error={errors.name} />
              <Input label="Email" type="email" value={form.email} onChange={set('email')} placeholder="you@locale.ng" required error={errors.email} />
              <Input label="Password" type="password" value={form.password} onChange={set('password')} placeholder="Min. 6 characters" required error={errors.password} />
              <Input label="Confirm Password" type="password" value={form.confirm} onChange={set('confirm')} placeholder="Repeat password" required error={errors.confirm} />
              <Button type="submit" block size="lg" loading={loading} variant="primary">Agree &amp; Register</Button>
              <p className={styles.switch}>Have an account? <button type="button" onClick={() => setMode('login')}>Log in</button></p>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
