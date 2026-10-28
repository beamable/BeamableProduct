import { createRoot } from 'react-dom/client'
import App from './App'
import './app.css'

// React is the runtime's (@beamable/portal-toolkit/runtime), which the page loads first; see shared/index.html.
createRoot(document.getElementById('root')!).render(<App />)
