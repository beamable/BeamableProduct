import { MAX_ATTEMPTS, TIPS, WORD_LENGTH } from '@app/game/constants.ts';
import { useCallback, useEffect, useRef } from 'react';
import { GameState, LetterMark } from '@app/game/types.ts';
import { useGameStore } from '@app/context/GameStoreContext.tsx';

export const useGrid = () => {
  const store = useGameStore();
  const tipTextTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const tipTextRef = useRef<HTMLParagraphElement | null>(null);
  const gridRowsRef = useRef<(HTMLDivElement | null)[]>(
    Array.from({ length: MAX_ATTEMPTS }, (): HTMLDivElement | null => null),
  );
  const tilesRef = useRef<(HTMLDivElement | null)[][]>(
    Array.from({ length: MAX_ATTEMPTS }, () =>
      Array.from({ length: WORD_LENGTH }, (): HTMLDivElement | null => null),
    ),
  );

  const setTilesRef = useCallback(
    (row: number, col: number) => (el: HTMLDivElement) => {
      if (!el) return;
      tilesRef.current[row][col] = el;
    },
    [],
  );

  const setGridRowsRef = useCallback(
    (row: number) => (el: HTMLDivElement) => {
      if (!el) return;
      gridRowsRef.current[row] = el;
    },
    [],
  );

  const handleGridChange = useCallback((state: GameState) => {
    if (state.status === 'revealing') return;

    const grid = state.grid;
    for (let row = 0; row < MAX_ATTEMPTS; row++) {
      for (let col = 0; col < WORD_LENGTH; col++) {
        const tile = grid[row][col];
        const el = tilesRef.current[row][col];
        if (!el) continue;

        if (!tile) {
          el.textContent = '';
          el.dataset['animation'] = 'idle';
          continue;
        }

        el.textContent = tile.letter;
        el.dataset['animation'] = 'pulse';
      }
    }
  }, []);

  const handleFlipTile = useCallback(
    (row: number, col: number, mark: LetterMark) => {
      const el = tilesRef.current[row][col];
      if (!el) return;

      el.dataset['animation'] = 'flip';
      el.addEventListener(
        'animationend',
        () => {
          el.classList.remove('tile--correct', 'tile--present', 'tile--absent');
          el.classList.add(`tile--${mark}`);
        },
        { once: true },
      );
    },
    [],
  );

  const handleShakeGridRow = useCallback((row: number, reason: string) => {
    const el = gridRowsRef.current[row];
    if (!el) return;

    if (tipTextRef.current) {
      const tipText = tipTextRef.current;
      tipText.classList.add('notification');
      tipText.textContent = reason;

      if (tipTextTimeoutRef.current) {
        clearTimeout(tipTextTimeoutRef.current);
        tipTextTimeoutRef.current = null;
      }

      tipTextTimeoutRef.current = setTimeout(() => {
        tipText.classList.remove('notification');
        tipText.textContent = TIPS;
      }, 2000);
    }

    el.dataset['animation'] = 'shake';
    el.addEventListener(
      'animationend',
      () => {
        el.dataset['animation'] = 'idle';
      },
      { once: true },
    );
  }, []);

  const reset = () => {
    for (let row = 0; row < MAX_ATTEMPTS; row++) {
      for (let col = 0; col < WORD_LENGTH; col++) {
        const el = tilesRef.current[row][col];
        if (!el) continue;

        el.textContent = '';
        el.dataset['animation'] = 'idle';
        el.classList.remove('tile--correct', 'tile--present', 'tile--absent');
      }
    }
    if (tipTextRef.current) {
      tipTextRef.current.textContent = TIPS;
    }
    if (tipTextTimeoutRef.current) clearTimeout(tipTextTimeoutRef.current);
  };

  useEffect(() => {
    store.subscribe(handleGridChange);
    store.setCallback({
      onShakeGridRow: handleShakeGridRow,
      onFlipTile: handleFlipTile,
    });
    window.addEventListener('reset', reset);
    return () => {
      window.removeEventListener('reset', reset);
    };
  }, []);

  return { tipTextRef, setTilesRef, setGridRowsRef };
};
