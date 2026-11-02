import { createRoot } from 'react-dom/client'
import { BeamPageHeader, BeamTab, BeamTabGroup, BeamTabPanel } from '../shared/beam'
import { ThemeToggle, Toaster, initialTheme, useSessionState } from '../shared/ui'
import StacksApp from '../stacks/App'
import BundlesApp from '../bundles/App'
import '../shared/beam.css'

const TABS = ['stacks', 'bundles']

/** The page `beam local management` opens: the workspace's local stacks, and its bundles. */
function Management() {
  const [stored, setStored] = useSessionState('page:tab', 'stacks')
  const active = TABS.includes(stored) ? stored : 'stacks'
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      <BeamPageHeader label="Local management" description="The local stacks and the bundles of this workspace, through the Beamable CLI">
        <ThemeToggle />
      </BeamPageHeader>
      <BeamTabGroup active={active} onChange={setStored}>
        <BeamTab panel="stacks">Stacks</BeamTab>
        <BeamTab panel="bundles">Bundles</BeamTab>
        <BeamTabPanel name="stacks"><StacksApp /></BeamTabPanel>
        <BeamTabPanel name="bundles"><BundlesApp /></BeamTabPanel>
      </BeamTabGroup>
      <Toaster />
    </div>
  )
}

document.documentElement.dataset.theme = initialTheme()
createRoot(document.getElementById('root')!).render(<Management />)
