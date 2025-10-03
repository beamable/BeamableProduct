import { useGameStore } from '@app/context/GameStoreContext.tsx';
import { useNavigate } from 'react-router-dom';
import { useEffect, useRef, useState } from 'react';
import { GameState } from '@app/game/types.ts';
import { getEndlessSeed } from '@app/game/engine/seed.ts';
import { useBeam } from '@app/context/BeamContext.tsx';
import { DAILY_STREAK, ENDLESS_STREAK } from '@app/game/constants.ts';
import { getAndComputePlayerStats } from '@app/beam.ts';

export const useResult = () => {
  const beam = useBeam();
  const store = useGameStore();
  const navigate = useNavigate();
  const resultContainerRef = useRef<HTMLDivElement>(null);
  const [state, setState] = useState<GameState>(store.getState());

  const didWin = state.status === 'round_end_win';
  const didLose = state.status === 'round_end_loss';
  const isOpen = didWin || didLose;
  const attemptsUsed = didWin ? state.row + 1 : state.maxAttempts;
  const answer = state.currentAnswer.join('');

  const handleGameEnd = async (gameState: GameState) => {
    if (resultContainerRef.current) {
      resultContainerRef.current.dataset['animation'] = 'reveal';
      resultContainerRef.current.addEventListener(
        'animationend',
        () => {
          resultContainerRef.current!.dataset['animation'] = 'idle';
        },
        { once: true },
      );
    }

    const newGameState = { ...gameState };
    setState(newGameState);

    const mode = gameState.mode;
    const key = mode === 'daily' ? DAILY_STREAK : ENDLESS_STREAK;

    const currentStreak = Number(store.stats[key] ?? '0');
    if (currentStreak === 0 && gameState.status === 'round_end_loss') return;

    const newStreak =
      gameState.status === 'round_end_win' ? String(currentStreak + 1) : '0';

    await beam.stats.set({
      accessType: 'private',
      stats: {
        [key]: newStreak,
      },
    });

    await getAndComputePlayerStats(beam, store);
  };

  const handleHome = () => {
    store.reset();
    dispatchEvent(new CustomEvent('reset'));
    setState(store.getState());
    navigate('/');
  };

  const handleContinue = () => {
    const seed = getEndlessSeed();
    store.nextRound(seed);
    dispatchEvent(new CustomEvent('reset'));
    setState(store.getState());
  };

  useEffect(() => {
    store.setCallback({
      onGameEnd: (gameState) => {
        void handleGameEnd(gameState);
      },
    });
  }, []);

  return {
    state,
    didWin,
    isOpen,
    attemptsUsed,
    answer,
    handleHome,
    handleContinue,
    resultContainerRef,
  };
};
