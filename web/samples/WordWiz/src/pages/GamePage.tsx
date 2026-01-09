import type { FC } from 'react';
import { Page } from '@app/components/Page.tsx';
import { Grid } from '@app/components/Grid.tsx';
import { Keyboard } from '@app/components/Keyboard.tsx';
import { Header } from '@app/components/Header.tsx';
import { Result } from '@app/components/Result.tsx';

export const GamePage: FC = () => {
  return (
    <Page>
      <div className="app-page app-page--game">
        <Header />
        <section className="game-section">
          <Grid />
          <Keyboard />
        </section>
        <Result />
      </div>
    </Page>
  );
};
