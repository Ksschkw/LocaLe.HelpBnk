import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useDialog } from '../../contexts/DialogContext'
import { jobsApi, bookingsApi, escrowApi, chatsApi, disputesApi } from '../../api'
import { useToast, fmt } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Button from '../../components/ui/Button'
import Card from '../../components/ui/Card'
import { Send, Lock, ShieldAlert, CheckCircle, Gamepad2 } from 'lucide-react'
import TicTacToe from '../../components/game/TicTacToe'
import styles from './Chat.module.css'

export default function ChatPage() {
  const { id: jobId } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const { confirm } = useDialog()

  const [job, setJob] = useState(null)
  const [booking, setBooking] = useState(null)
  const [escrow, setEscrow] = useState(null)
  const [messages, setMessages] = useState([])
  
  const [inputMsg, setInputMsg] = useState('')
  const [releaseTokenInput, setReleaseTokenInput] = useState('')
  
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [minigameOpen, setMinigameOpen] = useState(false)

  const messagesEndRef = useRef(null)

  const loadData = useCallback(async () => {
    try {
      const jobRes = await jobsApi.getById(jobId)
      setJob(jobRes.data)
      
      // Attempt to load booking if available
      try {
        const bRes = await bookingsApi.mine()
        const match = bRes.data.find(b => b.jobId === jobId)
        if (match) {
          setBooking(match)
          try {
            const eRes = await escrowApi.getByBooking(match.id)
            setEscrow(eRes.data)
          } catch (err) { /* Escrow might not be secured yet */ }
        }
      } catch (e) { /* ignore booking fail */ }

      // Polling chats
      const cRes = await chatsApi.getMessages(jobId)
      setMessages(cRes.data || [])

    } catch (e) {
       toast('Failed to load job details', 'error')
       navigate('/activity')
    } finally {
       setLoading(false)
    }
  }, [jobId, navigate, toast])

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 5000) // 5s chat polling
    return () => clearInterval(interval)
  }, [loadData])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  // 12h after Completed: block entry entirely
  if (!loading && job && job.status === 'Completed') {
    const closedAt = new Date(job.updatedAt || job.createdAt)
    const hoursSince = (Date.now() - closedAt.getTime()) / (1000 * 60 * 60)
    if (hoursSince > 12) {
      return (
        <div className={styles.page}>
          <TopBar back title="Contract Room" />
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '60vh', gap: 16, padding: 24, textAlign: 'center' }}>
            <div style={{ fontSize: '3rem' }}>🔒</div>
            <h2 style={{ color: 'var(--text-secondary)' }}>Room Permanently Closed</h2>
            <p style={{ color: '#666', maxWidth: 300 }}>This contract room was closed more than 12 hours ago and can no longer be accessed.</p>
          </div>
        </div>
      )
    }
  }

  if (loading && !job) return (
    <div className={styles.page}>
      <TopBar back title="Loading Contract Room..." />
      <div className="skeleton" style={{ height: '100%', margin: 16 }} />
    </div>
  )

  const isBuyer = user?.userId === job?.creatorId

  // --- ACTIONS ---
  
  const handleSendMessage = async () => {
    if (!inputMsg.trim()) return
    const txt = inputMsg
    setInputMsg('')
    try {
      // End-to-End Encryption mock flag hook
      await chatsApi.sendMessage(jobId, { content: txt, isEncrypted: false })
      loadData()
    } catch {
      toast('Failed to send message', 'error')
    }
  }

  // Provider side
  const handleAcceptJob = async () => {
    setActionLoading('accept')
    try {
      await jobsApi.accept(jobId)
      toast('Job accepted! Waiting for buyer to secure Escrow.', 'success')
      loadData()
    } catch (e) { toast(e.response?.data?.message || 'Error', 'error') }
    finally { setActionLoading(false) }
  }

  // Buyer side
  const handleSecureEscrow = async () => {
    if (!booking) return
    setActionLoading('secure')
    try {
      await bookingsApi.confirm(booking.id)
      toast('Funds successfully locked in Escrow!', 'success')
      loadData()
    } catch (e) { toast(e.response?.data?.message || 'Insufficient funds in Wallet', 'error') }
    finally { setActionLoading(false) }
  }

  // Buyer generates unlock code
  const handleGenerateCode = async () => {
    setActionLoading('generate')
    try {
      await jobsApi.confirmCompletion(jobId)
      toast('Job marked complete! Give the code to the provider.', 'success')
      loadData()
    } catch (e) { toast(e.message || 'Error generating release code', 'error') }
    finally { setActionLoading(false) }
  }

  // Buyer officially closes the job after vault unlocked
  const handleMarkComplete = async () => {
    setActionLoading('complete')
    try {
      await jobsApi.confirmCompletion(jobId)
      toast('Job closed! Provider stats updated. 🎉', 'success')
      loadData()
    } catch (e) { toast(e.message || 'Failed to close job', 'error') }
    finally { setActionLoading(false) }
  }

  // Provider releases
  const handleReleaseEscrow = async () => {
    if (!releaseTokenInput || !escrow) return
    setActionLoading('release')
    try {
      await escrowApi.release(escrow.id, releaseTokenInput.trim())
      toast('Escrow released to your wallet!', 'success')
      loadData()
    } catch (e) { toast(e.response?.data?.message || e.response?.data?.Error || 'Invalid QR/Release code', 'error') }
    finally { setActionLoading(false) }
  }

  const handleRefreshQr = async () => {
    setActionLoading('refresh')
    try {
      await escrowApi.refreshQr(escrow.id)
      toast('New Unlock Key generated!', 'success')
      loadData()
    } catch (e) { toast(e.response?.data?.message || e.response?.data?.Error || 'Error regenerating code', 'error') }
    finally { setActionLoading(false) }
  }

  const handleDispute = async () => {
    if (!escrow) return
    const isOk = await confirm({ title: 'Raise Dispute', message: 'Are you sure you want to raise a dispute? Funds will be frozen.', danger: true })
    if (!isOk) return
    
    setActionLoading('dispute')
    try {
      await disputesApi.raise(jobId, "Service not delivered as expected.")
      toast('Dispute raised. Admins have been notified.', 'warning')
      loadData()
    } catch (e) { toast('Error raising dispute', 'error') }
    finally { setActionLoading(false) }
  }

  return (
    <div className={styles.page}>
      <TopBar back title="Contract Room" action={
        <button onClick={() => setMinigameOpen(!minigameOpen)} style={{ background: 'none', border: 'none', color: 'var(--brand-primary)'}}>
          <Gamepad2 size={20} />
        </button>
      } />
      
      <div style={{ background: '#ff3333', color: '#111', padding: '12px', textAlign: 'center', fontSize: '0.85rem', fontWeight: 900, textTransform: 'uppercase', display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 8, boxShadow: '0 2px 10px rgba(255,0,0,0.3)', zIndex: 10 }}>
        <ShieldAlert size={16} /> WARNING: NEVER COMMUNICATE OR SEND FUNDS OUTSIDE OF LOCALE. THIS ROOM IS END-TO-END ENCRYPTED.
      </div>

      <div className={styles.content}>
        {/* Left Side: Job & Escrow Details */}
        <div className={styles.panel}>
          <Card>
            <h3 style={{ marginBottom: 4 }}>{job.title}</h3>
            <p style={{ fontSize: '0.85rem', opacity: 0.7 }}>{job.description}</p>
            <div style={{ marginTop: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <strong style={{ fontSize: '1.2rem' }}>{fmt(job.amount)}</strong>
              <div style={{ padding: '4px 8px', borderRadius: 12, background: 'var(--bg-color)', fontSize: '0.8rem', fontWeight: 'bold' }}>
                {job.status}
              </div>
            </div>
            {job.statusDetail && <p style={{ fontSize: '0.8rem', marginTop: 8, color: 'var(--text-secondary)' }}>{job.statusDetail}</p>}
          </Card>

          {/* Minigame Dropdown Overlay */}
          {minigameOpen && (
            <Card style={{ border: '2px solid var(--brand-primary)', overflow: 'hidden' }}>
               <h4 style={{textAlign: 'center', marginBottom: 12}}>Local 1v1 Pass & Play</h4>
               <TicTacToe mode="local" />
            </Card>
          )}

          {/* Action Management Pane - Iron Safe Gamification */}
          <Card className={styles.escrowBox} style={{
             background: 'linear-gradient(145deg, #2b2b2b, #111)',
             border: '4px solid #444',
             borderRadius: '8px',
             position: 'relative',
             overflow: 'hidden',
             boxShadow: 'inset 0 0 15px rgba(0,0,0,0.8), 0 5px 15px rgba(0,0,0,0.5)',
             padding: '24px'
          }}>
            {/* Safe Rivets */}
            <div style={{ position: 'absolute', top: 6, left: 6, width: 8, height: 8, background: '#666', borderRadius: '50%', boxShadow: 'inset 1px 1px 2px #999, 1px 1px 2px #000'}} />
            <div style={{ position: 'absolute', top: 6, right: 6, width: 8, height: 8, background: '#666', borderRadius: '50%', boxShadow: 'inset 1px 1px 2px #999, 1px 1px 2px #000'}} />
            <div style={{ position: 'absolute', bottom: 6, left: 6, width: 8, height: 8, background: '#666', borderRadius: '50%', boxShadow: 'inset 1px 1px 2px #999, 1px 1px 2px #000'}} />
            <div style={{ position: 'absolute', bottom: 6, right: 6, width: 8, height: 8, background: '#666', borderRadius: '50%', boxShadow: 'inset 1px 1px 2px #999, 1px 1px 2px #000'}} />

            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16, color: '#aaa', borderBottom: '2px solid #333', paddingBottom: 8 }}>
              <Lock size={16} /> <strong style={{ textTransform: 'uppercase', letterSpacing: '2px' }}>Iron Escrow Vault</strong>
            </div>

            {/* If NO Booking yet, and you are Provider */}
            {!booking && !isBuyer && job.status === 'Open' && (
              <div style={{ textAlign: 'center' }}>
                <p style={{ color: '#888', fontStyle: 'italic', marginBottom: 16 }}>Vault Door is Open. Awaiting your acceptance.</p>
                <Button block loading={actionLoading==='accept'} onClick={handleAcceptJob} style={{ background: 'linear-gradient(to right, #4CAF50, #2E7D32)' }}>
                  Accept Request
                </Button>
              </div>
            )}
            
            {/* If Booking exists, but no Escrow secured */}
            {booking && booking.status === 'Pending' && !escrow && isBuyer && (
               <div style={{ textAlign: 'center' }}>
                 <p style={{ color: '#888', marginBottom: 16 }}>Place your funds into the Iron Vault to begin.</p>
                 <Button block loading={actionLoading==='secure'} onClick={handleSecureEscrow} style={{ background: 'linear-gradient(to right, #0088ff, #0055ff)' }}>
                   Pay & Lock {fmt(job.amount)}
                 </Button>
               </div>
            )}
            {booking && booking.status === 'Pending' && !escrow && !isBuyer && (
               <p style={{ color: '#888', textAlign: 'center', animation: 'pulse 2s infinite' }}>Awaiting the Buyer to lock funds in the Vault...</p>
            )}

            {/* Escrow is Secured - In Progress */}
            {escrow && escrow.status === 'Secured' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16, position: 'relative' }}>
                
                {/* Visual Chains representing lock */}
                <div style={{ display: 'flex', justifyContent: 'center', margin: '8px 0', opacity: 0.7 }}>
                  <div style={{ height: 2, background: 'repeating-linear-gradient(90deg, #555, #555 10px, transparent 10px, transparent 20px)', width: '100%', alignSelf: 'center' }} />
                  <div style={{ background: '#ff3333', color: '#111', padding: '4px 12px', fontSize: '0.8rem', fontWeight: 900, textTransform: 'uppercase', letterSpacing: '1px', borderRadius: '4px', boxShadow: '0 0 10px rgba(255,50,50,0.5)', zIndex: 2 }}>
                    Secured
                  </div>
                  <div style={{ height: 2, background: 'repeating-linear-gradient(90deg, #555, #555 10px, transparent 10px, transparent 20px)', width: '100%', alignSelf: 'center' }} />
                </div>

                {!isBuyer && (
                   <div style={{ display: 'flex', gap: 8, background: '#1a1a1a', padding: '12px', border: '1px solid #333', borderRadius: '4px' }}>
                     <input 
                       className={styles.input} 
                       placeholder="Enter 6-digit Vault Key"
                       value={releaseTokenInput}
                       style={{ minWidth: 0, fontFamily: 'monospace', fontSize: '1.2rem', textAlign: 'center', background: '#000', border: '1px solid #444' }}
                       onChange={e => setReleaseTokenInput(e.target.value)}
                     />
                     <Button loading={actionLoading==='release'} onClick={handleReleaseEscrow} variant="primary">Unlock</Button>
                   </div>
                )}
                
                {isBuyer && !escrow.qrToken && (
                   <Button block loading={actionLoading==='generate'} onClick={handleGenerateCode} style={{ background: '#333', color: '#fff', border: '1px solid #555' }}>
                     Generate Unlock Key
                   </Button>
                )}

                {isBuyer && escrow.qrToken && (
                   <div style={{ background: '#1a1a1a', padding: 16, borderRadius: 8, border: '1px dashed #555', textAlign: 'center' }}>
                     <p style={{fontSize: '0.85rem', color: '#888', marginBottom: 8}}>Present this key to the Provider to unlock their funds:</p>
                     <div style={{ fontFamily: 'monospace', fontSize: '2.5rem', fontWeight: 900, color: '#00EAFF', letterSpacing: '4px', textShadow: '0 0 10px rgba(0,234,255,0.5)' }}>
                       {escrow.qrToken}
                     </div>
                     <Button variant="outline" size="sm" loading={actionLoading==='refresh'} onClick={handleRefreshQr} style={{ marginTop: 12 }}>
                        Regenerate Key
                     </Button>
                   </div>
                )}

                <Button block variant="outline" icon={<ShieldAlert size={14}/>} style={{ color: 'var(--danger-color)', borderColor: 'var(--danger-color)' }} onClick={handleDispute} loading={actionLoading==='dispute'}>
                  Trigger Vault Freeze (Dispute)
                </Button>
              </div>
            )}
            {/* Dispute State */}
            {escrow && escrow.status === 'InDispute' && (
              <div style={{ textAlign: 'center', padding: '16px', background: 'rgba(255, 0, 0, 0.1)', border: '1px solid var(--danger-color)', borderRadius: 8 }}>
                 <ShieldAlert size={32} color="var(--danger-color)" style={{ marginBottom: 12 }} />
                 <h3 style={{ color: 'var(--danger-color)', marginBottom: 8, textTransform: 'uppercase', letterSpacing: '1px' }}>Funds Frozen</h3>
                 <p style={{ fontSize: '0.85rem', color: '#ccc' }}>A dispute was raised. Funds are frozen pending Admin review and resolution.</p>
              </div>
            )}

            {/* Finished States */}
            {escrow && escrow.status === 'Released' && (
              <div style={{ textAlign: 'center', padding: '24px 0', animation: 'modal-pop 0.6s cubic-bezier(0.18, 0.89, 0.32, 1.28) forwards' }}>
                <div style={{ display: 'inline-flex', background: 'rgba(76, 175, 80, 0.1)', color: '#4CAF50', padding: '16px', borderRadius: '50%', marginBottom: 16, boxShadow: '0 0 20px rgba(76,175,80,0.3)' }}>
                   <CheckCircle size={48} />
                </div>
                <div style={{ color: '#4CAF50', fontWeight: 900, fontSize: '1.2rem', letterSpacing: '1px', textTransform: 'uppercase' }}>
                  Vault Unlocked
                </div>
                <p style={{ color: '#888', fontSize: '0.9rem', marginTop: 8 }}>Funds transferred to Wallet.</p>

                {isBuyer && job.status !== 'Completed' && (
                   <div style={{ marginTop: 24 }}>
                     <Button block variant="primary" loading={actionLoading === 'complete'} onClick={handleMarkComplete}>
                       ✅ Mark Job Completed
                     </Button>
                     <p style={{ fontSize: '0.8rem', color: '#888', marginTop: 8 }}>This officially closes the job and updates the provider's profile.</p>
                   </div>
                )}
                {job.status === 'Completed' && (
                   <p style={{ fontSize: '0.85rem', color: '#4CAF50', marginTop: 16, fontWeight: 700 }}>✅ Job successfully closed.</p>
                )}
              </div>
            )}
            {escrow && escrow.status === 'InDispute' && (
               <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', background: 'rgba(255,0,0,0.1)', border: '1px solid rgba(255,0,0,0.3)', padding: 16, borderRadius: 8 }}>
                 <ShieldAlert size={32} color="red" style={{ marginBottom: 8 }} />
                 <div style={{ color: 'red', fontWeight: 900, textTransform: 'uppercase', letterSpacing: '1px' }}>
                   Vault Frozen
                 </div>
                 <div style={{ fontSize: '0.85rem', color: '#ffaaaa', textAlign: 'center', marginTop: 4 }}>Admin team is reviewing the dispute.</div>
               </div>
            )}
          </Card>
        </div>

        {/* Right Side: Chat Window */}
        <div className={styles.chatArea}>
          <div className={styles.messages}>
             {messages.length === 0 ? (
                <div style={{ margin: 'auto', opacity: 0.5 }}>End-to-End Encrypted Contract Room</div>
             ) : (
                messages.map(msg => {
                   const mine = msg.senderId === user?.userId
                   return (
                     <div key={msg.id} className={[styles.msgRow, mine ? styles.mine : ''].join(' ')} onDoubleClick={async () => {
                       try {
                         await chatsApi.togglePin(jobId, msg.id);
                         loadData();
                         toast(msg.isPinned ? 'Message unpinned' : 'Message pinned', 'success');
                       } catch { toast('Error pinning', 'error'); }
                     }}>
                       <div className={styles.msgBubble} style={{ border: msg.isPinned ? '2px solid #FFD700' : 'none' }}>
                         {msg.isPinned && <div style={{ fontSize: '0.65rem', color: '#FFD700', marginBottom: 4, fontWeight: 'bold' }}>📌 PINNED</div>}
                         {msg.content}
                         <div className={styles.msgMeta}>
                           {new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                           {msg.isEncrypted && ' 🔒'}
                         </div>
                       </div>
                     </div>
                   )
                })
             )}
             <div ref={messagesEndRef} />
          </div>

          {/* Input Area */}
          {job?.status === 'Completed' && (Date.now() - new Date(job.updatedAt || job.createdAt).getTime()) > 12 * 60 * 60 * 1000 ? (
            <div style={{ textAlign: 'center', padding: 16, color: 'var(--text-secondary)', background: 'var(--bg-card)', borderTop: '1px solid var(--border-color)', fontStyle: 'italic' }}>
              🔒 Contract Room has been permanently closed (12h limit reached).
            </div>
          ) : (
            <div className={styles.inputArea}>
              <input 
                className={styles.input} 
                placeholder="Type message locally encrypted..."
                value={inputMsg}
                onChange={e => setInputMsg(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleSendMessage()}
              />
              <Button icon={<Send size={16} />} onClick={handleSendMessage} />
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
