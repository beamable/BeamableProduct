using System;

namespace Beamable.Player
{
	public enum PlayerStatType
	{
		/// <summary>
		/// client.public.player
		/// </summary>
		PUBLIC,
		
		/// <summary>
		/// client.private.player
		/// </summary>
		PRIVATE,
		
		/// <summary>
		/// game.public.player
		/// </summary>
		SECURE,
		
		/// <summary>
		/// game.private.player
		/// </summary>
		SECRET
	}

	public static class PlayerStatTypeExtensions
	{
		public static string GetBeamableObjectId(this PlayerStatType self, long playerId)
		{
			ToBeamableObjectId(self, out var id);
			return id + "." + playerId;
		}
		static void ToBeamableObjectId(this PlayerStatType self, out string objectId)
		{
			switch (self)
			{
				case PlayerStatType.PUBLIC:
					objectId = "client.public.player";
					break;
				case PlayerStatType.PRIVATE:
					objectId = "client.private.player";
					break;
				case PlayerStatType.SECURE:
					objectId = "game.public.player";
					break;
				case PlayerStatType.SECRET:
					objectId = "game.private.player";
					break;
				default:
					throw new InvalidOperationException("Unknown enum value for player stat type");
			}
		}
	}
}
