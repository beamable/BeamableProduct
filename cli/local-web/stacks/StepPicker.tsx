import { useMemo, useState, type CSSProperties } from 'react'
import { BeamButton, BeamCheckbox, BeamIcon, BeamInput, BeamTag } from '@beamable/portal-toolkit/react'
import { text, textMuted } from '../shared/ui'

export interface PickerGroup {
  title: string
  items: string[]
}

const BOX: CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '0.5rem', padding: '0.5rem 0.75rem',
  border: '1px solid var(--color-beam-border)', borderRadius: '0.375rem',
}
const LIST: CSSProperties = { maxHeight: '16rem', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '0.125rem' }
const GROUP_TITLE: CSSProperties = {
  ...textMuted, fontSize: '10px', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em',
  padding: '0.5rem 0 0.25rem', borderTop: '1px solid var(--color-beam-border)', marginTop: '0.25rem',
}

/**
 * Picks several tags or steps: what is picked shows as removable tags, and "Choose" opens a searchable list grouped
 * under titles (tags first, then the steps by kind).
 */
export function StepPicker({ label, groups, value, onChange, emptyText = 'Nothing', searchPlaceholder = 'Search tags and steps' }: {
  label: string
  groups: PickerGroup[]
  value: string[]
  onChange: (value: string[]) => void
  emptyText?: string
  searchPlaceholder?: string
}) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const toggle = (item: string, on: boolean) => onChange(on ? [...value, item] : value.filter((v) => v !== item))

  const shown = useMemo(() => {
    const term = search.trim().toLowerCase()
    return groups
      .map((g) => ({ ...g, items: term ? g.items.filter((i) => i.toLowerCase().includes(term)) : g.items }))
      .filter((g) => g.items.length > 0)
  }, [groups, search])
  const shownItems = shown.flatMap((g) => g.items)
  const allShownPicked = shownItems.length > 0 && shownItems.every((i) => value.includes(i))
  const toggleShown = () => onChange(allShownPicked
    ? value.filter((v) => !shownItems.includes(v))
    : [...value, ...shownItems.filter((i) => !value.includes(i))])

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
      <span style={{ ...text, fontSize: '14px', fontWeight: 600 }}>{label}</span>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.375rem' }}>
        {value.length === 0 && <span style={{ ...textMuted, fontSize: '13px' }}>{emptyText}</span>}
        {value.map((v) => (
          <BeamTag key={v} size="small" with-remove onWaRemove={() => toggle(v, false)}>{v}</BeamTag>
        ))}
        <BeamButton size="small" appearance="plain" onClick={() => setOpen(!open)}>
          <BeamIcon slot="start" name={open ? 'chevron-up' : 'chevron-down'} /> {open ? 'Done' : 'Choose'}
        </BeamButton>
      </div>
      {open && (
        <div style={BOX}>
          <BeamInput size="small" placeholder={searchPlaceholder} value={search} with-clear
            onValueInput={setSearch} onValueChange={setSearch} />
          {shownItems.length > 0 && (
            <BeamButton size="small" appearance="plain" style={{ alignSelf: 'flex-start' }} onClick={toggleShown}>
              {allShownPicked
                ? (search.trim() ? `Unselect the ${shownItems.length} shown` : 'Unselect all')
                : (search.trim() ? `Select the ${shownItems.length} shown` : 'Select all')}
            </BeamButton>
          )}
          <div style={LIST}>
            {shown.length === 0 && <span style={{ ...textMuted, fontSize: '13px' }}>Nothing matches.</span>}
            {shown.map((g, i) => (
              <div key={g.title}>
                <div style={i === 0 ? { ...GROUP_TITLE, borderTop: 'none', marginTop: 0, paddingTop: 0 } : GROUP_TITLE}>{g.title}</div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                  {g.items.map((item) => (
                    <BeamCheckbox key={item} size="small" checked={value.includes(item)} onCheckedChange={(on: boolean) => toggle(item, on)}>
                      {item}
                    </BeamCheckbox>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
