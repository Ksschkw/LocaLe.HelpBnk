import { useState, useRef, useEffect } from 'react'
import { escrowApi } from '../../api'
import { useToast } from '../../hooks/useUtils'
import Button from '../ui/Button'
import Input from '../ui/Input'
import styles from './ScanSheet.module.css'

export default function ScanSheet({ booking, onClose }) {
  const toast = useToast()
  const [code, setCode] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const inputRef = useRef(null)

  useEffect(() => {
    setTimeout(() => inputRef.current?.focus(), 300)
  }, [])

  const submit = async () => {
    if (code.trim().length !== 6) {
      setError('Please enter the 6-digit code')
      return
    }
    setError('')
    setLoading(true)
    try {
      const { data: escrow } = await escrowApi.getByBooking(booking.id)
      await escrowApi.release(escrow.id, code.trim())
      setSuccess(true)
      toast('💸 Payment released! Funds sent to provider.', 'success')
      setTimeout(() => onClose(), 2000)
    } catch (e) {
      setError(e?.response?.data?.error || e?.response?.data?.message || 'Invalid code. Try again.')
    } finally { setLoading(false) }
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.sheet} onClick={e => e.stopPropagation()}>
        <div className={styles.handle} />

        {success ? (
          <div className={styles.successState}>
            <div className={styles.successIcon}>💸</div>
            <h2>Payment Released!</h2>
            <p>Funds are being transferred to your provider.</p>
          </div>
        ) : (
          <>
            <h2 className={styles.title}>Scan to Release Funds</h2>
            <p className={styles.sub}>Enter the 6-digit code from the buyer's app to collect your payment.</p>

            <div className={styles.codeInputRow}>
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className={[styles.codeBox, code[i] ? styles.codeBoxFilled : ''].join(' ')}>
                  {code[i] || ''}
                </div>
              ))}
            </div>

            {/* Hidden full input */}
            <input
              ref={inputRef}
              type="text"
              inputMode="numeric"
              maxLength={6}
              value={code}
              onChange={e => { setCode(e.target.value.replace(/\D/g, '')); setError('') }}
              className={styles.hiddenInput}
              onKeyDown={e => { if (e.key === 'Enter') submit() }}
            />

            {error && <p className={styles.errorMsg}>⚠️ {error}</p>}

            <p className={styles.hint}>Ask the buyer for their 6-digit backup code if QR scanning fails. Works even with weak network.</p>

            <div className={styles.actions}>
              <Button block size="lg" onClick={submit} loading={loading} disabled={code.length !== 6}>
                ⚡ Release Payment
              </Button>
              <Button block variant="ghost" onClick={onClose}>Cancel</Button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
