import { createContext, useContext, type ReactNode } from 'react';
import type { Beam } from 'beamable-sdk';

// Holds the globally available Beam instance
export const BeamContext = createContext<Beam | null>(null);

export function BeamProvider({
  beam,
  children,
}: {
  beam: Beam;
  children: ReactNode;
}) {
  return <BeamContext.Provider value={beam}>{children}</BeamContext.Provider>;
}

export function useBeam(): Beam {
  const ctx = useContext(BeamContext);
  if (!ctx) {
    throw new Error('useBeam must be used within a BeamProvider');
  }
  return ctx;
}
