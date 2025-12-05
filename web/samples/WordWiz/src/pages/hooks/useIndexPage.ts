import { getEndlessSeed } from '@app/game/engine/seed.ts';
import { useGameStore } from '@app/context/GameStoreContext.tsx';
import { useNavigate } from 'react-router-dom';

export const useIndexPage = () => {
  const store = useGameStore();
  const navigate = useNavigate();

  const handleEndlessButtonClick = () => {
    const seed = getEndlessSeed();
    store.changeMode('endless', seed);
    navigate('/game');
  };

  return { handleEndlessButtonClick };
};
