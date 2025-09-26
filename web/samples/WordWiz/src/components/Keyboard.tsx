import { useKeyboard } from '@app/components/hooks/useKeyboard.ts';
import { KEYBOARD_ROW } from '@app/game/constants.ts';

export const Keyboard = () => {
  const {
    setKeyboardKeysRef,
    handleOnScreenKeyboardKeyClick,
    handleSubmit,
    handleDeleteLetter,
  } = useKeyboard();

  return (
    <div className="keyboard-container">
      <div className="keyboard-group">
        <div className="keyboard-row">
          {KEYBOARD_ROW[0].map((key) => {
            return (
              <button
                key={key}
                className="keyboard-key"
                data-key={key}
                onClick={handleOnScreenKeyboardKeyClick}
                ref={setKeyboardKeysRef}
              >
                {key}
              </button>
            );
          })}
        </div>

        <div className="keyboard-row">
          <div className="keyboard-edge"></div>
          {KEYBOARD_ROW[1].map((key) => {
            return (
              <button
                key={key}
                className="keyboard-key"
                data-key={key}
                onClick={handleOnScreenKeyboardKeyClick}
                ref={setKeyboardKeysRef}
              >
                {key}
              </button>
            );
          })}
          <div className="keyboard-edge"></div>
        </div>

        <div className="keyboard-row">
          <button
            className="keyboard-key keyboard-key--large"
            onClick={handleSubmit}
          >
            Enter
          </button>
          {KEYBOARD_ROW[2].map((key) => {
            return (
              <button
                key={key}
                className="keyboard-key"
                data-key={key}
                onClick={handleOnScreenKeyboardKeyClick}
                ref={setKeyboardKeysRef}
              >
                {key}
              </button>
            );
          })}
          <button
            className="keyboard-key keyboard-key--large"
            onClick={handleDeleteLetter}
          >
            <svg
              aria-hidden="true"
              xmlns="http://www.w3.org/2000/svg"
              height="16"
              width="16"
              viewBox="0 0 24 24"
            >
              <path
                fill="currentColor"
                d="M22 3H7c-.69 0-1.23.35-1.59.88L0 12l5.41 8.11c.36.53.9.89 1.59.89h15c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H7.07L2.4 12l4.66-7H22v14zm-11.59-2L14 13.41 17.59 17 19 15.59 15.41 12 19 8.41 17.59 7 14 10.59 10.41 7 9 8.41 12.59 12 9 15.59z"
              ></path>
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};
