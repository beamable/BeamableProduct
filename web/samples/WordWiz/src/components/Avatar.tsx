type AvatarProps = {
  name?: string;
  picture?: string;
  size?: number; // pixels
  className?: string;
};

export const Avatar = ({
  name = 'Guest',
  picture = '',
  size = 32,
  className = '',
}: AvatarProps) => {
  const initial = (name?.trim?.() || 'G').charAt(0).toUpperCase();

  const style: React.CSSProperties = {
    width: size,
    height: size,
    fontSize: Math.round(size * 0.45),
  };

  return (
    <div
      className={`avatar ${className}`.trim()}
      style={style}
      title={name}
      aria-label={`Avatar for ${name}`}
    >
      {picture ? (
        <img
          style={{ width: '100%', height: '100%', borderRadius: '50%' }}
          src={picture}
          alt={name}
        />
      ) : (
        <>{initial}</>
      )}
    </div>
  );
};
