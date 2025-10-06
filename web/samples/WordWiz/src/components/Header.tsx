import { Avatar } from '@app/components/Avatar.tsx';
import { useHeader } from '@app/components/hooks/useHeader.ts';
import { DAILY_STREAK, ENDLESS_STREAK } from '@app/game/constants.ts';

export const Header = () => {
  const { state, stats } = useHeader();

  return (
    <div className="header-container">
      <header>
        <div className="profile-group">
          <Avatar />
          <p className="username">Guest</p>
        </div>
        <div className="stack-vert stack-vert--right">
          <p className="caption caption--right">{state.mode}</p>
          <p className="data data--right">
            Streak:{' '}
            {stats[state.mode === 'daily' ? DAILY_STREAK : ENDLESS_STREAK]}
          </p>
        </div>
      </header>
    </div>
  );
};
