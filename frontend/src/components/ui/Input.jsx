import styles from './Input.module.css'

export default function Input({
  label, type = 'text', id, value, onChange, onKeyUp,
  placeholder, required, min, max, minLength, maxLength,
  inputMode, hint, error, ...props
}) {
  return (
    <div className={styles.group}>
      {label && <label className={styles.label} htmlFor={id}>{label}</label>}
      <input
        id={id}
        type={type}
        value={value}
        onChange={onChange}
        onKeyUp={onKeyUp}
        placeholder={placeholder}
        required={required}
        min={min}
        max={max}
        minLength={minLength}
        maxLength={maxLength}
        inputMode={inputMode}
        className={[styles.input, error ? styles.inputError : ''].join(' ')}
        {...props}
      />
      {hint && !error && <p className={styles.hint}>{hint}</p>}
      {error && <p className={styles.error}>{error}</p>}
    </div>
  )
}
