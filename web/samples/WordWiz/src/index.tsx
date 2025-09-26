import ReactDOM from 'react-dom/client';
import { StrictMode } from 'react';
import { Root } from '@app/components/Root.tsx';
import { init } from '@app/init.ts';
import { BeamProvider } from '@app/context/BeamContext.tsx';
import { createGameStore } from '@app/game/state/store.ts';
import { GameStoreProvider } from '@app/context/GameStoreContext.tsx';
import { MAX_ATTEMPTS, WORD_LENGTH } from '@app/game/constants.ts';
import { Letter } from '@app/game/types.ts';
import answers from '@app/assets/answers.ts';
import { getAndComputePlayerStats } from '@app/beam.ts';
import './index.css';
import { ErrorBoundary } from './components/ErrorBoundary';

const root = ReactDOM.createRoot(document.getElementById('root')!);

try {
  // Configure all application dependencies.
  const beam = await init();

  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });

  await getAndComputePlayerStats(beam, store);

  root.render(
    <StrictMode>
      <BeamProvider beam={beam}>
        <GameStoreProvider store={store}>
          <Root />
        </GameStoreProvider>
      </BeamProvider>
    </StrictMode>,
  );
} catch (error) {
  console.error('Failed to initialize WordWiz:', error);
  root.render(<ErrorBoundary />);
}
