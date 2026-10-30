// The components the pages use, in plain React and beam.css: the same names and look as the portal's beam-* components,
// with only what these pages need, so the CLI's web app depends on nothing but React.

import {
  Children,
  cloneElement,
  isValidElement,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type MouseEvent,
  type ReactElement,
  type ReactNode,
} from 'react'
import { ICONS } from './icons'

type Variant = 'brand' | 'neutral' | 'success' | 'warning' | 'danger'
type Size = 'small' | 'medium'

/** The `slot` a child asks for (the portal components' slot names), or '' for none. */
function slotOf(child: ReactNode): string {
  return isValidElement<{ slot?: string }>(child) ? child.props.slot ?? '' : ''
}

/** Splits children by slot: `bySlot(children, 'footer')` returns [the footer children, the rest]. */
function bySlot(children: ReactNode, slot: string): [ReactNode[], ReactNode[]] {
  const all = Children.toArray(children)
  return [all.filter((c) => slotOf(c) === slot), all.filter((c) => slotOf(c) !== slot)]
}

const cx = (...names: (string | false | undefined | null)[]) => names.filter(Boolean).join(' ')

// ── Icons, badges, small things ──

export function BeamIcon({ name, style }: { name: string; style?: CSSProperties; slot?: string }) {
  const icon = ICONS[name]
  if (!icon) return null
  return (
    <svg className={cx('beam-icon', name === 'spinner' && 'beam-spin')} viewBox={`0 0 ${icon[0]} 512`} style={style} aria-hidden="true">
      <path fill="currentColor" d={icon[1]} />
    </svg>
  )
}

export function BeamSpinner({ style }: { style?: CSSProperties }) {
  return <span className="beam-spinner" style={style} role="progressbar" aria-label="Loading" />
}

export function BeamBadge({ variant = 'brand', appearance = 'filled', pill, title, style, children }: {
  variant?: Variant; appearance?: 'filled' | 'outlined'; pill?: boolean; title?: string; style?: CSSProperties; children?: ReactNode
}) {
  return <span className={cx('beam-badge', `beam-${variant}`, `beam-${appearance}`, pill && 'beam-pill')} title={title} style={style}>{children}</span>
}

export function BeamTag({ onRemove, children, ...rest }: { size?: Size; 'with-remove'?: boolean; onRemove?: () => void; children?: ReactNode }) {
  return (
    <span className="beam-tag">
      {children}
      {rest['with-remove'] && (
        <button type="button" className="beam-tag-remove" aria-label="Remove" onClick={onRemove}><BeamIcon name="xmark" /></button>
      )}
    </span>
  )
}

export function BeamDivider() {
  return <hr className="beam-divider" />
}

export function BeamCallout({ variant = 'brand', children }: { variant?: Variant; children?: ReactNode }) {
  const [icon, rest] = bySlot(children, 'icon')
  return (
    <div className={cx('beam-callout', `beam-${variant}`)} role="note">
      {icon.length > 0 && <span className="beam-callout-icon">{icon}</span>}
      <div className="beam-callout-body">{rest}</div>
    </div>
  )
}

export function BeamProgressBar({ value, indeterminate, label, style }: { value: number; indeterminate?: boolean; label?: string; style?: CSSProperties }) {
  return (
    <div className={cx('beam-progress', indeterminate && 'beam-indeterminate')} style={style} role="progressbar" aria-label={label}
      aria-valuenow={indeterminate ? undefined : value} aria-valuemin={0} aria-valuemax={100}>
      <div className="beam-progress-fill" style={indeterminate ? undefined : { width: `${Math.max(0, Math.min(100, value))}%` }} />
    </div>
  )
}

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 31536000], ['month', 2592000], ['week', 604800], ['day', 86400], ['hour', 3600], ['minute', 60], ['second', 1],
]

/** "3 minutes ago", kept current. Hovering shows the UTC time. */
export function BeamRelativeTime({ date, numeric = 'auto', style }: { date: string; numeric?: 'auto' | 'always'; style?: CSSProperties; 'with-utc-popover'?: boolean }) {
  const [now, setNow] = useState(Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(timer)
  }, [])
  const at = new Date(date)
  const seconds = Math.round((at.getTime() - now) / 1000)
  const [unit, size] = RELATIVE_UNITS.find(([, s]) => Math.abs(seconds) >= s) ?? ['second', 1]
  const label = new Intl.RelativeTimeFormat(undefined, { numeric }).format(Math.round(seconds / size), unit)
  return <time dateTime={date} title={at.toUTCString()} style={style}>{label}</time>
}

// ── Buttons ──

export function BeamButton({ size = 'medium', appearance = 'filled', variant = 'neutral', disabled, onClick, title, href, target, style, children }: {
  size?: Size
  appearance?: 'filled' | 'outlined' | 'plain'
  variant?: Variant
  disabled?: boolean
  onClick?: (e: MouseEvent) => void
  title?: string
  href?: string
  target?: string
  style?: CSSProperties
  slot?: string
  children?: ReactNode
}) {
  const className = cx('beam-button', `beam-${variant}`, `beam-${appearance}`, size === 'small' && 'beam-small')
  if (href && !disabled) {
    return <a className={className} href={href} target={target} rel="noreferrer" title={title} style={style} onClick={onClick}>{children}</a>
  }
  return <button type="button" className={className} disabled={disabled} title={title} style={style} onClick={onClick}>{children}</button>
}

export function BeamCopyButton({ value, ...labels }: { value: string; 'copy-label'?: string; 'success-label'?: string }) {
  const [copied, setCopied] = useState(false)
  async function copy() {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    } catch { /* clipboard blocked; nothing to report */ }
  }
  const label = copied ? labels['success-label'] ?? 'Copied' : labels['copy-label'] ?? 'Copy'
  return (
    <button type="button" className="beam-button beam-neutral beam-plain beam-small" title={label} aria-label={label} onClick={copy}>
      <BeamIcon name={copied ? 'check' : 'copy'} />
    </button>
  )
}

// ── Form fields ──

function Field({ label, style, children }: { label?: string; style?: CSSProperties; children: ReactNode }) {
  return (
    <label className="beam-field" style={style}>
      {label && <span className="beam-field-label">{label}</span>}
      {children}
    </label>
  )
}

interface TextProps {
  size?: Size
  label?: string
  value: string
  disabled?: boolean
  placeholder?: string
  /** Every keystroke. */
  onValueInput?: (value: string) => void
  /** When the field is left or Enter is pressed; also every keystroke when there is no onValueInput. */
  onValueChange?: (value: string) => void
  style?: CSSProperties
  appearance?: string
  'with-clear'?: boolean
}

export function BeamInput({ label, value, disabled, placeholder, onValueInput, onValueChange, style, ...rest }: TextProps) {
  return (
    <Field label={label} style={style}>
      <input className="beam-input" type={rest['with-clear'] ? 'search' : 'text'} value={value} disabled={disabled} placeholder={placeholder}
        onChange={(e) => (onValueInput ?? onValueChange)?.(e.target.value)}
        onBlur={(e) => onValueInput && onValueChange?.(e.target.value)}
        onKeyDown={(e) => { if (e.key === 'Enter') onValueChange?.(e.currentTarget.value) }} />
    </Field>
  )
}

export function BeamTextarea({ label, value, disabled, placeholder, onValueInput, onValueChange, rows, style }: TextProps & { rows?: number }) {
  return (
    <Field label={label} style={style}>
      <textarea className="beam-input" value={value} disabled={disabled} placeholder={placeholder} rows={rows}
        onChange={(e) => (onValueInput ?? onValueChange)?.(e.target.value)}
        onBlur={(e) => onValueInput && onValueChange?.(e.target.value)} />
    </Field>
  )
}

export function BeamCheckbox({ checked, onCheckedChange, children }: { checked: boolean; onCheckedChange?: (checked: boolean) => void; size?: Size; children?: ReactNode }) {
  return (
    <label className="beam-checkbox">
      <input type="checkbox" checked={checked} onChange={(e) => onCheckedChange?.(e.target.checked)} />
      <span>{children}</span>
    </label>
  )
}

/** An option of a select: read by `BoundSelect`, which renders it. */
export function BeamOption(_: { value: string; disabled?: boolean; children?: ReactNode }): ReactElement | null {
  return null
}

interface SelectProps {
  label?: string
  /** Seeded once on mount; to reset it, remount with a `key`. */
  defaultValue: string | string[]
  onChange: (value: any) => void
  multiple?: boolean
  /** Offers an empty choice, shown as the placeholder. */
  clearable?: boolean
  placeholder?: string
  disabled?: boolean
  size?: Size
  style?: CSSProperties
  /** `BeamOption`s; a `<small>` titles the options after it and a `BeamDivider` is skipped. */
  children?: ReactNode
}

type SelectEntry = { title: string } | { value: string; disabled?: boolean; label: ReactNode }

function selectEntries(children: ReactNode): SelectEntry[] {
  return Children.toArray(children).flatMap((child): SelectEntry[] => {
    if (!isValidElement<{ value?: string; disabled?: boolean; children?: ReactNode }>(child)) return []
    if (child.type === BeamOption) return [{ value: child.props.value ?? '', disabled: child.props.disabled, label: child.props.children }]
    if (child.type === 'small') return [{ title: String(child.props.children ?? '') }]
    return []
  })
}

/** A single choice (a native select), or several (`multiple`: a list of checkboxes). Reports through `onChange`. */
export function BoundSelect({ label, defaultValue, onChange, multiple, clearable, placeholder, disabled, style, children }: SelectProps) {
  const [value, setValue] = useState<string | string[]>(defaultValue)
  const entries = selectEntries(children)
  const set = (next: string | string[]) => { setValue(next); onChange(next) }

  if (multiple) {
    const picked = Array.isArray(value) ? value : value ? [value] : []
    return (
      <div className="beam-field" style={style} role="group" aria-label={label}>
        {label && <span className="beam-field-label">{label}</span>}
        <div className="beam-choice-list">
          {entries.map((e, i) => 'title' in e
            ? <small key={`t-${i}`} className="beam-choice-title">{e.title}</small>
            : (
              <label key={e.value} className={cx('beam-checkbox', e.disabled && !picked.includes(e.value) && 'beam-disabled')}>
                <input type="checkbox" checked={picked.includes(e.value)} disabled={disabled || (e.disabled && !picked.includes(e.value))}
                  onChange={(ev) => set(ev.target.checked ? [...picked, e.value] : picked.filter((p) => p !== e.value))} />
                <span>{e.label}</span>
              </label>
            ))}
        </div>
      </div>
    )
  }

  // A native option holds text only.
  const text = (node: ReactNode) => Children.toArray(node).filter((n) => typeof n === 'string' || typeof n === 'number').join('')
  return (
    <Field label={label} style={style}>
      <select className="beam-input" value={String(value ?? '')} disabled={disabled} onChange={(e) => set(e.target.value)}>
        {(clearable || (placeholder && !entries.some((e) => 'value' in e && e.value === ''))) && <option value="">{placeholder ?? ''}</option>}
        {entries.map((e, i) => 'title' in e
          ? <option key={`t-${i}`} disabled>{`— ${e.title} —`}</option>
          : <option key={e.value} value={e.value} disabled={e.disabled}>{text(e.label)}</option>)}
      </select>
    </Field>
  )
}

// ── Disclosure, dialogs, menus ──

export function BeamDetails({ open, onOpenChange, children }: { open?: boolean; onOpenChange?: (open: boolean) => void; children?: ReactNode }) {
  const [summary, rest] = bySlot(children, 'summary')
  return (
    <details className="beam-details" open={open} onToggle={(e) => onOpenChange?.(e.currentTarget.open)}>
      <summary>{summary}</summary>
      <div className="beam-details-body">{rest}</div>
    </details>
  )
}

/**
 * A modal dialog: a native `<dialog>` shown while `open`. Escape and the close button call `onClose`; so does a click
 * outside it with `light-dismiss`. Children with `slot="footer"` go in the footer.
 */
export function BeamDialog({ open, label, onClose, style, children, ...flags }: {
  open: boolean
  label: string
  onClose: () => void
  style?: CSSProperties
  'with-footer'?: boolean
  'light-dismiss'?: boolean
  children?: ReactNode
}) {
  const ref = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])
  const [footer, body] = bySlot(children, 'footer')
  const titleId = useId()
  return (
    <dialog ref={ref} className="beam-dialog" style={style} aria-labelledby={titleId}
      onCancel={(e) => { e.preventDefault(); onClose() }}
      onClick={(e) => { if (flags['light-dismiss'] && e.target === e.currentTarget) onClose() }}>
      {open && (
        <>
          <header className="beam-dialog-header">
            <h2 id={titleId}>{label}</h2>
            <button type="button" className="beam-button beam-neutral beam-plain beam-small" aria-label="Close" onClick={onClose}>
              <BeamIcon name="xmark" />
            </button>
          </header>
          <div className="beam-dialog-body">{body}</div>
          {flags['with-footer'] !== false && footer.length > 0 && <footer className="beam-dialog-footer">{footer}</footer>}
        </>
      )}
    </dialog>
  )
}

export function BeamConfirmDialog({ open, label, message, onConfirm, onCancel, ...rest }: {
  open: boolean
  label: string
  message: string
  'confirm-label'?: string
  'confirm-variant'?: Variant
  onConfirm: () => void
  onCancel: () => void
}) {
  return (
    <BeamDialog open={open} label={label} onClose={onCancel} with-footer style={{ '--width': '28rem' } as CSSProperties}>
      <p style={{ margin: 0 }}>{message}</p>
      <div slot="footer" className="beam-row-end">
        <BeamButton size="small" appearance="outlined" onClick={onCancel}>Cancel</BeamButton>
        <BeamButton size="small" variant={rest['confirm-variant'] ?? 'brand'} onClick={onConfirm}>{rest['confirm-label'] ?? 'Confirm'}</BeamButton>
      </div>
    </BeamDialog>
  )
}

/**
 * A menu under its trigger (the child with `slot="trigger"`). It is a popover, so it stays above tables and dialogs
 * and closes on Escape or a click elsewhere.
 */
export function BeamDropdown({ placement = 'bottom-start', children }: { placement?: 'bottom-start' | 'bottom-end'; children?: ReactNode }) {
  const menu = useRef<HTMLDivElement>(null)
  const [trigger, items] = bySlot(children, 'trigger')

  function toggle(e: MouseEvent) {
    const el = menu.current
    if (!el) return
    if (el.matches(':popover-open')) {
      el.hidePopover()
      return
    }
    el.showPopover()
    const at = (e.currentTarget as HTMLElement).getBoundingClientRect()
    const { width, height } = el.getBoundingClientRect()
    const left = placement === 'bottom-end' ? at.right - width : at.left
    const top = at.bottom + 4 + height > window.innerHeight ? at.top - 4 - height : at.bottom + 4
    el.style.left = `${Math.max(4, Math.min(left, window.innerWidth - width - 4))}px`
    el.style.top = `${Math.max(4, top)}px`
  }

  return (
    <>
      {trigger.map((t) => isValidElement<{ onClick?: (e: MouseEvent) => void }>(t) ? cloneElement(t, { onClick: toggle }) : t)}
      <div ref={menu} popover="auto" className="beam-menu" role="menu">{items}</div>
    </>
  )
}

export function BeamDropdownItem({ onClick, disabled, children }: { onClick?: () => void; disabled?: boolean; children?: ReactNode }) {
  return (
    <button type="button" role="menuitem" className="beam-menu-item" disabled={disabled}
      onClick={(e) => { (e.currentTarget.closest('[popover]') as HTMLElement | null)?.hidePopover(); onClick?.() }}>
      {children}
    </button>
  )
}

// ── Tabs, page header ──

export function BeamTab(_: { panel: string; slot?: string; children?: ReactNode }): ReactElement | null {
  return null
}

export function BeamTabPanel(_: { name: string; children?: ReactNode }): ReactElement | null {
  return null
}

/**
 * Tabs from its `BeamTab` and `BeamTabPanel` children. A panel renders the first time its tab is shown and then stays
 * mounted (hidden), so what it loaded and typed survives switching tabs.
 */
export function BeamTabGroup({ active, onChange, children }: { active: string; onChange: (name: string) => void; children?: ReactNode }) {
  const all = Children.toArray(children).filter(isValidElement)
  const tabs = all.filter((c) => c.type === BeamTab) as ReactElement<{ panel: string; children?: ReactNode }>[]
  const panels = all.filter((c) => c.type === BeamTabPanel) as ReactElement<{ name: string; children?: ReactNode }>[]
  const [seen, setSeen] = useState<string[]>([active])
  useEffect(() => { setSeen((s) => (s.includes(active) ? s : [...s, active])) }, [active])
  const id = useId()
  return (
    <div className="beam-tab-group">
      <div className="beam-tab-nav" role="tablist">
        {tabs.map((t) => (
          <button key={t.props.panel} type="button" role="tab" id={`${id}-${t.props.panel}`} aria-selected={t.props.panel === active}
            className={cx('beam-tab', t.props.panel === active && 'beam-active')} onClick={() => onChange(t.props.panel)}>
            {t.props.children}
          </button>
        ))}
      </div>
      {panels.map((p) => (seen.includes(p.props.name) || p.props.name === active) && (
        <div key={p.props.name} role="tabpanel" aria-labelledby={`${id}-${p.props.name}`} hidden={p.props.name !== active} className="beam-tab-panel">
          {p.props.children}
        </div>
      ))}
    </div>
  )
}

export function BeamPageHeader({ label, description, children }: { label: string; description?: string; children?: ReactNode }) {
  return (
    <header className="beam-page-header">
      <div>
        <h1>{label}</h1>
        {description && <p>{description}</p>}
      </div>
      {children && <div className="beam-page-header-actions">{children}</div>}
    </header>
  )
}

// ── Table ──

interface ColumnProps<T> {
  field: keyof T & string
  header: string
  width?: string
  align?: 'left' | 'right'
  sortable?: boolean
  /** Renders the cell; without it (or `format`) the field's value shows as text. */
  children?: (row: T) => ReactNode
  format?: (value: any, row: T) => ReactNode
}

/** A column of a `BeamTable`: read by the table, which renders it. */
export function BeamColumn<T>(_: ColumnProps<T>): ReactElement | null {
  return null
}

/** A table of `data` with the `BeamColumn` children as columns, sortable where asked, paged with "Showing X-Y of Z". */
export function BeamTable<T>({ data, pageSize = 10, pageSizeOptions, rowKey, emptyMessage = 'No data', children }: {
  data: T[]
  pageSize?: number
  pageSizeOptions?: number[]
  rowKey: (row: T) => string
  emptyMessage?: string
  children?: ReactNode
}) {
  const columns = Children.toArray(children)
    .filter((c): c is ReactElement<ColumnProps<T>> => isValidElement(c) && c.type === BeamColumn)
    .map((c) => c.props)
  const [sort, setSort] = useState<{ index: number; desc: boolean } | null>(null)
  const [size, setSize] = useState(pageSize)
  const [page, setPage] = useState(0)

  const sorted = useMemo(() => {
    if (!sort) return data
    const field = columns[sort.index]?.field
    if (!field) return data
    const value = (row: T) => row[field] as unknown
    return [...data].sort((a, b) => {
      const [x, y] = [value(a), value(b)]
      const order = typeof x === 'number' && typeof y === 'number' ? x - y : String(x ?? '').localeCompare(String(y ?? ''))
      return sort.desc ? -order : order
    })
    // columns are rebuilt every render; the sorted field is what matters.
  }, [data, sort, columns[sort?.index ?? -1]?.field])

  const pages = Math.max(1, Math.ceil(sorted.length / size))
  const current = Math.min(page, pages - 1)
  const rows = sorted.slice(current * size, current * size + size)
  const cell = (column: ColumnProps<T>, row: T) =>
    column.children ? column.children(row) : column.format ? column.format(row[column.field], row) : String(row[column.field] ?? '')

  return (
    <div className="beam-table">
      <div className="beam-table-scroll">
        <table>
          <thead>
            <tr>
              {columns.map((c, i) => (
                <th key={i} style={{ width: c.width, textAlign: c.align ?? 'left' }}
                  aria-sort={sort?.index === i ? (sort.desc ? 'descending' : 'ascending') : undefined}>
                  {c.sortable ? (
                    <button type="button" className="beam-sort" onClick={() => setSort(sort?.index === i ? { index: i, desc: !sort.desc } : { index: i, desc: false })}>
                      {c.header}
                      <BeamIcon name={sort?.index === i && sort.desc ? 'chevron-up' : 'chevron-down'} style={{ opacity: sort?.index === i ? 1 : 0.35 }} />
                    </button>
                  ) : c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && <tr><td colSpan={columns.length} className="beam-table-empty">{emptyMessage}</td></tr>}
            {rows.map((row) => (
              <tr key={rowKey(row)}>
                {columns.map((c, i) => <td key={i} style={{ textAlign: c.align ?? 'left' }}>{cell(c, row)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {sorted.length > Math.min(size, ...(pageSizeOptions ?? [size])) && (
        <div className="beam-table-pager">
          <span>Showing {current * size + 1}-{Math.min(sorted.length, current * size + size)} of {sorted.length}</span>
          {pageSizeOptions && (
            <select className="beam-input beam-pager-size" aria-label="Rows per page" value={size} onChange={(e) => { setSize(Number(e.target.value)); setPage(0) }}>
              {pageSizeOptions.map((n) => <option key={n} value={n}>{n} per page</option>)}
            </select>
          )}
          <BeamButton size="small" appearance="outlined" disabled={current === 0} onClick={() => setPage(current - 1)}>Prev</BeamButton>
          <BeamButton size="small" appearance="outlined" disabled={current >= pages - 1} onClick={() => setPage(current + 1)}>Next</BeamButton>
        </div>
      )}
    </div>
  )
}
