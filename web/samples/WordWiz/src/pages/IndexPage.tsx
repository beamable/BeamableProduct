import type { FC } from 'react';
import { Page } from '@app/components/Page.tsx';
import logo from '@app/assets/logo.png';
import EndlessIcon from '@app/assets/EndlessIcon.tsx';
import { Profile } from '@app/components/Profile.tsx';
import { useIndexPage } from '@app/pages/hooks/useIndexPage.ts';

export const IndexPage: FC = () => {
  const { handleEndlessButtonClick } = useIndexPage();

  return (
    <Page>
      <div className="app-page app-page--game">
        <Profile />
        <div className="flex-spacer" />
        <div className="landing-hero">
          <img src={logo} alt="logo" className="logo-img" />
          <div className="button-row">
            <button className="btn" onClick={handleEndlessButtonClick}>
              <EndlessIcon />
              Endless
            </button>
          </div>
        </div>
        <div className="landing-footer">
          <p>
            powered by <span className="text-strong">Beamable</span>
          </p>
        </div>
      </div>
    </Page>
  );
};
