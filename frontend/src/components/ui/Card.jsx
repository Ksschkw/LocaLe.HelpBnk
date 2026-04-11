import styles from './Card.module.css'

export default function Card({ children, glass = false, glow = false, onClick, style, className = '' }) {
  return (
    <div
      onClick={onClick}
      style={style}
      className={[
        styles.card,
        glass ? styles.glass : '',
        glow ? styles.glow : '',
        onClick ? styles.clickable : '',
        className
      ].join(' ')}
    >
      {children}
    </div>
  )
}
