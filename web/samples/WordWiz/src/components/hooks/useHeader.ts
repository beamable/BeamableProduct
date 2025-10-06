import { useGameStore } from '@app/context/GameStoreContext.tsx';
import { useCallback, useEffect, useState } from 'react';

export const useHeader = () => {
  const store = useGameStore();
  const state = store.getState();
  const [stats, setStats] = useState(store.stats);

  const handleStatUpdate = useCallback(() => {
    setStats(store.stats);
  }, []);

  useEffect(() => {
    window.addEventListener('stats_updated', handleStatUpdate);
    return () => {
      window.removeEventListener('stats_updated', handleStatUpdate);
    };
  }, []);

  return { state, stats };
};
