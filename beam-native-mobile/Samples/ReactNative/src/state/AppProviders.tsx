import type { ReactNode } from 'react';

import { BeamProvider } from './beamContext';
import { LogProvider } from './logContext';
import { NotificationProvider } from './notificationContext';

/**
 * Composes the app's providers in dependency order:
 *
 *   LogProvider          — owns `append`, needed by both providers below
 *   └─ BeamProvider      — auto-init, connection status, account, rail opt-in
 *      └─ NotificationProvider — funnel coords, deep-link routing, token registration
 *
 * Mounted at the root layout so the pushed Details screen can log too.
 */
export default function AppProviders({ children }: { children: ReactNode }) {
  return (
    <LogProvider>
      <BeamProvider>
        <NotificationProvider>{children}</NotificationProvider>
      </BeamProvider>
    </LogProvider>
  );
}
