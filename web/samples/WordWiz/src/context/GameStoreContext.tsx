import { createContext, useContext, type ReactNode } from 'react';
import { GameStore } from '@app/game/state/store.ts';

export const GameStoreContext = createContext<GameStore | null>(null);

export function GameStoreProvider({
  store,
  children,
}: {
  store: GameStore;
  children: ReactNode;
}) {
  return (
    <GameStoreContext.Provider value={store}>
      {children}
    </GameStoreContext.Provider>
  );
}

export function useGameStore(): GameStore {
  const ctx = useContext(GameStoreContext);
  if (!ctx) {
    throw new Error('useGameStore must be used within a GameStoreProvider');
  }
  return ctx;
}
