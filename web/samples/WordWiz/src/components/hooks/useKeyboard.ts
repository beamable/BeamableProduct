import {
  MouseEvent as ReactMouseEvent,
  useCallback,
  useEffect,
  useRef,
} from 'react';
import { useGameStore } from '@app/context/GameStoreContext.tsx';
import { GameState, Letter } from '@app/game/types.ts';

export const useKeyboard = () => {
  const store = useGameStore();
  const keyboardKeysRef = useRef<HTMLButtonElement[]>([]);

  const setKeyboardKeysRef = useCallback((el: HTMLButtonElement) => {
    keyboardKeysRef.current.push(el);
  }, []);

  const handleSubmit = () => {
    store.submit();
  };

  const handleDeleteLetter = () => {
    store.deleteLetter();
  };

  const handleOnScreenKeyboardKeyClick = (
    event: ReactMouseEvent<HTMLButtonElement, MouseEvent>,
  ) => {
    const key = (event.target as HTMLButtonElement).dataset['key'];
    if (!key) return;

    store.inputLetter(key.toUpperCase() as Letter);
  };

  const handlePhysicalKeyboardKeyClick = (event: KeyboardEvent) => {
    event.preventDefault();
    const key = event.key;

    if (key === 'Enter') {
      handleSubmit();
    } else if (key === 'Backspace') {
      handleDeleteLetter();
    } else if (/^[a-zA-Z]$/.test(key)) {
      store.inputLetter(key.toUpperCase() as Letter);
    }
  };

  const handleKeyboardChange = useCallback((state: GameState) => {
    if (state.status === 'revealing') return;

    const keyboardState = state.keyboard;
    for (let i = 0; i < keyboardKeysRef.current.length; i++) {
      const key = keyboardKeysRef.current[i];
      if (!key) continue;

      const keyLetter = key.dataset['key'];
      if (!keyLetter) continue;

      const keyLetterMark = keyboardState[keyLetter.toUpperCase() as Letter];
      if (!keyLetterMark) continue;

      key.classList.remove(
        'keyboard-key--correct',
        'keyboard-key--present',
        'keyboard-key--absent',
      );
      key.classList.add(`keyboard-key--${keyLetterMark}`);
    }
  }, []);

  const reset = () => {
    for (let i = 0; i < keyboardKeysRef.current.length; i++) {
      const key = keyboardKeysRef.current[i];
      if (!key) return;

      key.classList.remove(
        'keyboard-key--correct',
        'keyboard-key--present',
        'keyboard-key--absent',
      );
    }
  };

  useEffect(() => {
    store.subscribe(handleKeyboardChange);
    window.addEventListener('keydown', handlePhysicalKeyboardKeyClick);
    window.addEventListener('reset', reset);
    return () => {
      window.removeEventListener('keydown', handlePhysicalKeyboardKeyClick);
      window.removeEventListener('reset', reset);
    };
  }, []);

  return {
    setKeyboardKeysRef,
    handleOnScreenKeyboardKeyClick,
    handleSubmit,
    handleDeleteLetter,
    handleKeyboardChange,
  };
};
