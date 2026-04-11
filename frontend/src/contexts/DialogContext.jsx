import { createContext, useContext, useState, useCallback, useRef } from 'react'
import Button from '../components/ui/Button'
import styles from './Dialog.module.css'

const DialogContext = createContext({})

export function useDialog() {
  return useContext(DialogContext)
}

export function DialogProvider({ children }) {
  const [dialogs, setDialogs] = useState([])

  const confirm = useCallback((options) => {
    return new Promise((resolve) => {
      setDialogs((d) => [
        ...d,
        {
          id: Math.random().toString(),
          type: 'confirm',
          ...options,
          resolve
        }
      ])
    })
  }, [])

  const prompt = useCallback((options) => {
    return new Promise((resolve) => {
      setDialogs((d) => [
        ...d,
        {
          id: Math.random().toString(),
          type: 'prompt',
          ...options,
          resolve
        }
      ])
    })
  }, [])

  const closeDialog = (id, result) => {
    setDialogs((prev) => {
      const idx = prev.findIndex((p) => p.id === id)
      if (idx !== -1) {
        prev[idx].resolve(result)
        const updated = [...prev]
        updated.splice(idx, 1)
        return updated
      }
      return prev
    })
  }

  return (
    <DialogContext.Provider value={{ confirm, prompt }}>
      {children}
      {dialogs.length > 0 && (
        <div className={styles.overlay}>
          {dialogs.map((d) => (
            <DialogUI key={d.id} dialog={d} onClose={(res) => closeDialog(d.id, res)} />
          ))}
        </div>
      )}
    </DialogContext.Provider>
  )
}

function DialogUI({ dialog, onClose }) {
  const [inputValue, setInputValue] = useState(dialog.defaultValue || '')
  
  return (
    <div className={styles.modal}>
      <h3 className={styles.title}>{dialog.title || 'Attention Required'}</h3>
      <p className={styles.message}>{dialog.message}</p>
      
      {dialog.type === 'prompt' && (
        <input 
          className={styles.input} 
          autoFocus
          placeholder={dialog.placeholder || 'Type here...'}
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={(e) => {
             if (e.key === 'Enter') onClose(inputValue)
             if (e.key === 'Escape') onClose(null)
          }}
        />
      )}
      
      <div className={styles.actions}>
        <Button variant="secondary" onClick={() => onClose(dialog.type === 'prompt' ? null : false)}>
          {dialog.cancelText || 'Cancel'}
        </Button>
        <Button variant={dialog.danger ? 'danger' : 'primary'} onClick={() => onClose(dialog.type === 'prompt' ? inputValue : true)}>
          {dialog.confirmText || 'Confirm'}
        </Button>
      </div>
    </div>
  )
}
