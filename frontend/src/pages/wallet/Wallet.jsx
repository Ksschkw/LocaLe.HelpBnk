import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../../contexts/AuthContext'
import { useDialog } from '../../contexts/DialogContext'
import { useToast, fmt, tierColor, tierEmoji } from '../../hooks/useUtils'
import { walletApi, usersApi } from '../../api'
import Card from '../../components/ui/Card'
import Button from '../../components/ui/Button'
import Input from '../../components/ui/Input'
import TopBar from '../../components/layout/TopBar'
import styles from './Wallet.module.css'

export default function WalletPage() {
  const { user } = useAuth()
  const toast = useToast()
  const { confirm } = useDialog()
  const [wallet, setWallet] = useState(null)
  const [txns, setTxns] = useState([])
  const [loading, setLoading] = useState(true)
  const [showTopup, setShowTopup] = useState(false)
  const [amount, setAmount] = useState('')
  const [topupLoading, setTopupLoading] = useState(false)
  const [withdrawLoading, setWithdrawLoading] = useState(false)
  const [badgeLoading, setBadgeLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [wRes, tRes] = await Promise.all([walletApi.get(), walletApi.transactions()])
      setWallet(wRes.data)
      setTxns(tRes.data || [])
    } catch { toast('Failed to load wallet', 'error') }
    finally { setLoading(false) }
  }, [toast])

  useEffect(() => { load() }, [load])

  const handleTopup = async (e) => {
    e.preventDefault()
    const n = Number(amount)
    if (!n || n < 1000) { toast('Minimum top-up is ₦1,000', 'error'); return }
    setTopupLoading(true)
    try {
      await walletApi.topup(n)
      toast(`₦${n.toLocaleString()} added to wallet ✓`, 'success')
      setShowTopup(false)
      setAmount('')
      load()
    } catch (e) {
      toast(e?.response?.data?.message || 'Top-up failed', 'error')
    } finally { setTopupLoading(false) }
  }

  const handleWithdraw = async () => {
    const n = Number(amount)
    if (!n || n < 1000) { toast('Minimum withdraw is ₦1,000', 'error'); return }
    setWithdrawLoading(true)
    try {
      await walletApi.withdraw(n)
      toast(`₦${n.toLocaleString()} withdrawn to bank ✓`, 'success')
      setShowTopup(false)
      setAmount('')
      load()
    } catch (e) {
      toast(e?.response?.data?.message || 'Withdrawal failed. Insufficient funds?', 'error')
    } finally { setWithdrawLoading(false) }
  }

  const handlePurchaseBadge = async () => {
    const isOk = await confirm({ title: 'Upgrade Account', message: "Purchase Platinum Badge for ₦10,000? This instantly boosts your Trust Tier globally." })
    if (!isOk) return
    
    setBadgeLoading(true)
    try {
      await usersApi.purchaseBadge()
      toast('Welcome to the Platinum Tier! 🌟', 'success')
      load()
    } catch (e) {
      toast(e?.response?.data?.message || 'Insufficient wallet balance for badge upgrade', 'error')
    } finally { setBadgeLoading(false) }
  }

  return (
    <div className={styles.page}>
      <TopBar back={false} title="Wallet" />

      <div className={styles.content}>
        {/* Balance Card */}
        <div className={styles.balanceCard}>
          <div className={styles.balanceBg} />
          <span className={styles.balanceLabel}>Available Balance</span>
          {loading ? (
            <div className="skeleton" style={{ width: 160, height: 48, borderRadius: 8 }} />
          ) : (
            <h1 className={styles.balanceAmount}>{fmt(wallet?.balance)}</h1>
          )}
          <div className={styles.balanceSub}>LocaLe Escrow Wallet</div>
        </div>

        {/* Actions */}
        <div className={styles.actions} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <Button block variant="success" onClick={() => setShowTopup(t => !t)}>💰 Top Up</Button>
            <Button block variant="secondary" onClick={() => setShowTopup(t => !t)}>⬆️ Withdraw</Button>
          </div>
          
          <Button block variant="outline" style={{ borderColor: '#FFD700', color: '#FFD700', background: 'rgba(255, 215, 0, 0.05)' }} onClick={handlePurchaseBadge} loading={badgeLoading}>
            💎 Buy Platinum Badge (₦10,000)
          </Button>
        </div>

        {/* Top-up/Withdraw Form */}
        {showTopup && (
          <Card className={styles.topupCard} glow>
            <h3 style={{ marginBottom: 12 }}>External Transfer</h3>
            <form onSubmit={(e) => e.preventDefault()}>
              <Input label="Amount (₦ NGN)" type="number" min={1000} placeholder="Min ₦1,000"
                value={amount} onChange={e => setAmount(e.target.value)} required />
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 12 }}>
                <Button type="button" block size="lg" loading={topupLoading} onClick={handleTopup} style={{ background: 'var(--success-color)' }}>Deposit In</Button>
                <Button type="button" block size="lg" loading={withdrawLoading} onClick={handleWithdraw} style={{ background: 'var(--danger-color)' }}>Withdraw Out</Button>
              </div>
            </form>
          </Card>
        )}

        {/* Transaction History */}
        <h3 className={styles.sectionTitle}>Transaction Ledger</h3>

        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="skeleton" style={{ height: 60, marginBottom: 8 }} />
          ))
        ) : txns.length === 0 ? (
          <div className={styles.empty}>No transactions yet.</div>
        ) : txns.map((t, i) => {
          const isDeposit = t.action?.includes('TopUp') || t.action?.includes('Released') || t.action?.includes('Refund') || t.action?.includes('CANCELLED');
          const isWithdrawal = t.action?.includes('Withdraw') || t.action?.includes('Locked') || t.action?.includes('SECURED');
          
          let amt = null;
          const amtMatch = t.details?.match(/₦([0-9.,]+)/);
          if (amtMatch) amt = `₦${amtMatch[1]}`;

          return (
            <div key={i} className={styles.txn}>
              <div>
                <div className={styles.txnLabel}>{t.action || 'Transaction'}</div>
                <div className={styles.txnDate} style={{ fontSize: '0.8rem', opacity: 0.6, marginTop: 4 }}>{t.details}</div>
                <div className={styles.txnDate}>{new Date(t.timestamp).toLocaleString()}</div>
              </div>
              <div className={[styles.txnAmount, isDeposit ? styles.credit : styles.debit].join(' ')}>
                {isDeposit ? '+' : (isWithdrawal ? '-' : '')} {amt || 'Action'}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
