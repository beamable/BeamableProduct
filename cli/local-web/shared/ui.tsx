// Small pieces both tabs share: text styles, errors, toasts, session state and the theme.

import { useCallback, useEffect, useRef, useState, useSyncExternalStore, type CSSProperties } from 'react'
import { BeamButton, BeamIcon } from './beam'

export { BoundSelect } from './beam'

/** Default body text color. */
export const text: CSSProperties = { color: 'var(--color-beam-text)' }

/** Muted/secondary text color. */
export const textMuted: CSSProperties = { color: 'var(--color-beam-text-muted)' }

/** Monospace font stack (ids, timestamps, code-ish values). */
export const mono: CSSProperties = { fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace' }

/** Narrow an unknown thrown value to a message, with a caller-supplied fallback. */
export function errorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}

// ── Toasts: one stack for the page, rendered once by <Toaster /> ──

type ToastVariant = 'success' | 'danger' | 'warning' | 'info'
interface ToastEntry { id: number; variant: ToastVariant; message: string }

const TOAST_MS = 4000
let toasts: ToastEntry[] = []
let nextToastId = 1
const toastListeners = new Set<() => void>()

function setToasts(next: ToastEntry[]) {
  toasts = next
  toastListeners.forEach((l) => l())
}

function push(variant: ToastVariant, message: string) {
  const id = nextToastId++
  setToasts([...toasts, { id, variant, message }])
  setTimeout(() => setToasts(toasts.filter((t) => t.id !== id)), TOAST_MS)
}

export const toast = {
  success: (message: string) => push('success', message),
  error: (message: string) => push('danger', message),
  warning: (message: string) => push('warning', message),
  info: (message: string) => push('info', message),
}

export function Toaster() {
  const entries = useSyncExternalStore((l) => { toastListeners.add(l); return () => { toastListeners.delete(l) } }, () => toasts)
  const ref = useRef<HTMLDivElement>(null)
  // A popover, re-shown for each toast: the top layer puts it above an open dialog (which is in the top layer too).
  useEffect(() => {
    const el = ref.current
    if (!el) return
    if (el.matches(':popover-open')) el.hidePopover()
    if (entries.length > 0) el.showPopover()
  }, [entries.length])
  return (
    <div ref={ref} popover="manual" className="beam-toasts" role="status" aria-live="polite">
      {entries.map((t) => (
        <div key={t.id} className={`beam-toast beam-${t.variant === 'info' ? 'brand' : t.variant}`}>
          <span>{t.message}</span>
          <button type="button" className="beam-button beam-neutral beam-plain beam-small" aria-label="Dismiss"
            onClick={() => setToasts(toasts.filter((x) => x.id !== t.id))}>
            <BeamIcon name="xmark" />
          </button>
        </div>
      ))}
    </div>
  )
}

/** State kept in this tab's sessionStorage, so a reload keeps it. */
export function useSessionState<T>(key: string, initial: T): [T, (value: T) => void] {
  const storageKey = `beam-local-web:${key}`
  const [value, setValue] = useState<T>(() => {
    try {
      const stored = sessionStorage.getItem(storageKey)
      return stored === null ? initial : (JSON.parse(stored) as T)
    } catch {
      return initial
    }
  })
  const set = useCallback(
    (next: T) => {
      setValue(next)
      try { sessionStorage.setItem(storageKey, JSON.stringify(next)) } catch { /* storage unavailable */ }
    },
    [storageKey],
  )
  return [value, set]
}

// ── Theme: data-theme on <html>, remembered in localStorage ──

type Theme = 'dark' | 'light'
const THEME_KEY = 'beam-local-web:theme'

/** The theme to start with: the one picked last, else the system's. */
export function initialTheme(): Theme {
  try {
    const stored = localStorage.getItem(THEME_KEY)
    if (stored === 'dark' || stored === 'light') return stored
  } catch { /* storage unavailable */ }
  return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(() => (document.documentElement.dataset.theme as Theme) || initialTheme())
  useEffect(() => {
    document.documentElement.dataset.theme = theme
    try { localStorage.setItem(THEME_KEY, theme) } catch { /* storage unavailable */ }
  }, [theme])
  return (
    <BeamButton size="small" appearance="plain" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}>
      {theme === 'dark' ? 'Light mode' : 'Dark mode'}
    </BeamButton>
  )
}
