import logo from '@app/assets/logo.png';
import { useResult } from '@app/components/hooks/useResult.ts';
import HomeIcon from '@app/assets/HomeIcon.tsx';
import NextIcon from '@app/assets/NextIcon.tsx';

export const Result = () => {
  const {
    state,
    didWin,
    isOpen,
    attemptsUsed,
    answer,
    handleHome,
    handleContinue,
    resultContainerRef,
  } = useResult();

  return (
    <dialog className="result-dialog" open={isOpen}>
      <div
        className="result-container"
        data-animation="idle"
        ref={resultContainerRef}
      >
        <div className="result-header">
          <img src={logo} alt="logo" className="brand-logo" />
          <p className="result-title">
            {didWin ? 'You Win!' : 'Out of guesses'}
          </p>
          {didWin ? (
            <p className="result-subtitle">
              Solved in {attemptsUsed}/{state.maxAttempts}
            </p>
          ) : (
            <p className="result-subtitle">Answer</p>
          )}
          <div className="result-answer">{answer}</div>
        </div>
        <div className="result-actions">
          <button className="btn btn--sm" onClick={handleHome}>
            <HomeIcon />
            Home
          </button>
          {state.mode === 'endless' && (
            <button className="btn btn--sm" onClick={handleContinue}>
              <NextIcon />
              Continue
            </button>
          )}
        </div>
      </div>
    </dialog>
  );
};
