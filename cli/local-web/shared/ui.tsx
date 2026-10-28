// Small pieces both apps share. The first ones are copies of agentic-portal's extension libs (portal-ui, portal-dev),
// which these apps can't import from another repository.

import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { BeamButton, BeamSelect, BeamToast, BeamToastStack } from '@beamable/portal-toolkit/react'

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

/**
 * A `<BeamSelect>` that reports its value through `onChange`. The value is seeded once on mount; to reset it,
 * remount it with a `key`.
 */
export function BoundSelect({
  defaultValue,
  onChange,
  multiple,
  children,
  ...rest
}: {
  defaultValue: string | string[]
  onChange: (value: any) => void
  multiple?: boolean
  children: ReactNode
  [key: string]: unknown
}) {
  const elRef = useRef<any>(null)
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange
  const seeded = useRef(false)

  const bind = useCallback(
    (el: any) => {
      elRef.current = el
      if (!el || el.__beamBoundSelect) return
      el.__beamBoundSelect = true
      el.addEventListener('change', () => {
        const value = el.value
        if (multiple) onChangeRef.current(Array.isArray(value) ? value : value ? [value] : [])
        else onChangeRef.current(String(value ?? ''))
      })
      el.addEventListener('wa-clear', () => onChangeRef.current(multiple ? [] : ''))
    },
    [multiple],
  )

  useEffect(() => {
    const el = elRef.current
    if (!el || seeded.current) return
    seeded.current = true
    Promise.resolve(el.updateComplete).then(() => {
      el.value = defaultValue
    })
    // Seeded once on mount by design (see above).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <BeamSelect ref={bind} size="small" multiple={multiple} {...rest}>
      {children}
    </BeamSelect>
  )
}

type ToastVariant = 'success' | 'danger' | 'warning' | 'info'

let nextToastId = 1

/** A page-local toast stack: call the returned `toast` functions and render `<Toaster />` once. */
export function useToasts(duration = 4000) {
  const [entries, setEntries] = useState<{ id: number; variant: ToastVariant; message: string }[]>([])
  const dismiss = useCallback((id: number) => setEntries((prev) => prev.filter((t) => t.id !== id)), [])
  const push = useCallback((variant: ToastVariant, message: string) => {
    setEntries((prev) => [...prev, { id: nextToastId++, variant, message }])
  }, [])

  const toast = useMemo(
    () => ({
      success: (message: string) => push('success', message),
      error: (message: string) => push('danger', message),
      warning: (message: string) => push('warning', message),
      info: (message: string) => push('info', message),
    }),
    [push],
  )

  const Toaster = useCallback(
    () => (
      <BeamToastStack placement="bottom-end">
        {entries.map((t) => (
          <BeamToast key={t.id} variant={t.variant} message={t.message} duration={duration} show-progress onWaHide={() => dismiss(t.id)} />
        ))}
      </BeamToastStack>
    ),
    [entries, duration, dismiss],
  )

  return { toast, Toaster }
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

type Theme = 'dark' | 'light'

interface RuntimeTheme {
  get(): Theme
  toggle(): void
  subscribe(listener: (value: Theme) => void): () => void
}

/** The runtime's light/dark theme (see @beamable/portal-toolkit/runtime), shared with the portal's setting. */
function runtimeTheme(): RuntimeTheme | undefined {
  return (window as unknown as { __beamPortal?: { theme?: RuntimeTheme } }).__beamPortal?.theme
}

export function ThemeToggle() {
  const theme = runtimeTheme()
  const [value, setValue] = useState<Theme>(theme?.get() ?? 'dark')
  useEffect(() => theme?.subscribe(setValue), [theme])
  if (!theme) return null
  return (
    <BeamButton size="small" appearance="plain" onClick={() => theme.toggle()} title={`Switch to ${value === 'dark' ? 'light' : 'dark'} mode`}>
      {value === 'dark' ? 'Light mode' : 'Dark mode'}
    </BeamButton>
  )
}
