import { Avatar } from '@app/components/Avatar.tsx';

interface ProfileProps {
  name?: string;
  picture?: string;
}

export const Profile = ({ name = 'Guest', picture = '' }: ProfileProps) => {
  return (
    <div className="profile-container" aria-label="User profile">
      <header>
        <div className="profile-group">
          <Avatar name={name} picture={picture} />
          <p className="username">{name}</p>
        </div>
      </header>
    </div>
  );
};
