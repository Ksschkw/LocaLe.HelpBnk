import { useEffect, useRef, useState } from 'react'
import { escrowApi } from '../../api'
import { useToast, fmt } from '../../hooks/useUtils'
import Button from '../ui/Button'
import styles from './EscrowQRSheet.module.css'

export default function EscrowQRSheet({ escrow, booking, onClose }) {
  const toast = useToast()
  const canvasRef = useRef(null)
  const [refreshing, setRefreshing] = useState(false)
  const [timer, setTimer] = useState(null)
  const [currentEscrow, setCurrentEscrow] = useState(escrow)

  useEffect(() => {
    drawQR(currentEscrow.qrToken)
    if (currentEscrow.qrTokenExpiry) {
      const remaining = (new Date(currentEscrow.qrTokenExpiry) - Date.now()) / 1000
      setTimer(Math.max(0, Math.floor(remaining)))
    }
  }, [currentEscrow])

  useEffect(() => {
    if (timer === null) return
    if (timer <= 0) return
    const t = setTimeout(() => setTimer(t => t - 1), 1000)
    return () => clearTimeout(t)
  }, [timer])

  const drawQR = (code) => {
    const canvas = canvasRef.current
    if (!canvas || !code) return
    // Simple pixel art QR pattern seeded by code
    const ctx = canvas.getContext('2d')
    const size = 200
    canvas.width = size; canvas.height = size
    ctx.fillStyle = '#fff'
    ctx.fillRect(0, 0, size, size)
    ctx.fillStyle = '#000'
    const cell = 5
    const cells = size / cell

    // Finder patterns
    const finder = (ox, oy) => {
      ctx.fillRect(ox * cell, oy * cell, 7 * cell, 7 * cell)
      ctx.fillStyle = '#fff'
      ctx.fillRect((ox+1)*cell, (oy+1)*cell, 5*cell, 5*cell)
      ctx.fillStyle = '#000'
      ctx.fillRect((ox+2)*cell, (oy+2)*cell, 3*cell, 3*cell)
    }
    finder(0, 0); finder(cells-7, 0); finder(0, cells-7)

    let seed = 0
    for (let i = 0; i < code.length; i++) seed += code.charCodeAt(i) * (i + 7)
    const rng = () => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed % 3 === 0 }

    for (let y = 0; y < cells; y++) {
      for (let x = 0; x < cells; x++) {
        if ((x < 8 && y < 8) || (x >= cells-8 && y < 8) || (x < 8 && y >= cells-8)) continue
        if (rng()) ctx.fillRect(x * cell, y * cell, cell, cell)
      }
    }
  }

  const refresh = async () => {
    setRefreshing(true)
    try {
      const { data } = await escrowApi.refreshQr(currentEscrow.id)
      setCurrentEscrow(data)
      toast('QR Token refreshed', 'success')
    } catch (e) {
      toast(e?.response?.data?.message || 'Refresh failed', 'error')
    } finally { setRefreshing(false) }
  }

  const cancel = async () => {
    if (!confirm('Cancel escrow and refund your wallet?')) return
    try {
      await escrowApi.cancel(currentEscrow.id)
      toast('Escrow cancelled. Funds refunded.', 'success')
      onClose()
    } catch (e) {
      toast(e?.response?.data?.message || 'Cancel failed', 'error')
    }
  }

  const timerColor = timer !== null && timer <= 60 ? 'var(--error)' : 'var(--text-2)'

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.sheet} onClick={e => e.stopPropagation()}>
        <div className={styles.handle} />

        <div className={styles.header}>
          <h2 className={styles.title}>Escrow Locked</h2>
          <p className={styles.sub}>Show this to your provider only when satisfied</p>
        </div>

        {/* Amount */}
        <div className={styles.amountBox}>
          <span className={styles.amountLabel}>Locked Amount</span>
          <span className={styles.amount}>{fmt(currentEscrow.amount)}</span>
        </div>

        {/* QR Canvas */}
        <div className={styles.qrWrap}>
          <div className={styles.qrBox}>
            <canvas ref={canvasRef} />
          </div>
          <div className={styles.codeDisplay}>
            {currentEscrow.qrToken?.split('').map((d, i) => (
              <span key={i} className={styles.digit}>{d}</span>
            ))}
          </div>
          {timer !== null && (
            <p className={styles.timer} style={{ color: timerColor }}>
              ⏱ Token expires in {Math.floor(timer / 60)}:{String(timer % 60).padStart(2, '0')}
            </p>
          )}
        </div>

        <div className={styles.warning}>
          ⚠️ Only scan when the job is 100% complete. This releases funds immediately.
        </div>

        <div className={styles.actions}>
          <Button block variant="secondary" loading={refreshing} onClick={refresh}>↺ Refresh Token</Button>
          <Button block variant="danger" onClick={cancel}>Cancel &amp; Refund</Button>
          <Button block variant="ghost" onClick={onClose}>Close</Button>
        </div>
      </div>
    </div>
  )
}
