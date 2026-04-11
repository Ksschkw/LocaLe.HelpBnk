import styles from './Button.module.css'

export default function Button({
  children, variant = 'primary', size = 'md', block = false,
  loading = false, icon, onClick, type = 'button', disabled, style, className = ''
}) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled || loading}
      style={style}
      className={[
        styles.btn,
        styles[variant],
        styles[size],
        block ? styles.block : '',
        loading ? styles.loading : '',
        className
      ].join(' ')}
    >
      {loading ? <span className="spinner" /> : null}
      {!loading && icon ? <span className={styles.icon}>{icon}</span> : null}
      {!loading ? children : null}
    </button>
  )
}
