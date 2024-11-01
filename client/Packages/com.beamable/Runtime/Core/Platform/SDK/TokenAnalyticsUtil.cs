using Beamable.Api.Analytics;
using System;
using System.Collections.Generic;

namespace Beamable.Api
{
	public interface ITokenEventSettings
	{
		bool EnableTokenAnalytics { get; }
	}


	public class TokenEvent : CoreEvent
	{
		public static TokenEvent InvalidAccessToken(long playerId, string accessToken, string refreshToken, string error)
		{
			return new TokenEvent(
				eventName: "access_token_invalid",
				eventParams: new Dictionary<string, object>
				{
					["player-id"] = playerId,
					["error"] = error,
					["access-token-last-4"] = Last4(accessToken),
					["refresh-token-last-4"] = Last4(refreshToken)
				});
		}


		public static TokenEvent GetNewToken(long playerId, string newAccessToken, string newRefreshToken, string oldAccessToken, string oldRefreshToken)
		{
			return new TokenEvent(
				eventName: "got_new_token",
				eventParams: new Dictionary<string, object>
				{
					["player-id"] = playerId,
					["old-access-token-last-4"] = Last4(oldAccessToken),
					["old-refresh-token-last-4"] = Last4(oldRefreshToken),
					["new-access-token-last-4"] = Last4(newAccessToken),
					["new-refresh-token-last-4"] = Last4(newRefreshToken)
				});
		}

		public static TokenEvent ChangingToken(long playerId, string newAccessToken, string newRefreshToken, string oldAccessToken, string oldRefreshToken)
		{
			return new TokenEvent(
				eventName: "will_change_token",
				eventParams: new Dictionary<string, object>
				{
					["player-id"] = playerId,
					["old-access-token-last-4"] = Last4(oldAccessToken),
					["old-refresh-token-last-4"] = Last4(oldRefreshToken),
					["new-access-token-last-4"] = Last4(newAccessToken),
					["new-refresh-token-last-4"] = Last4(newRefreshToken)
				});
		}

		public static string Last4(string input)
		{
			if (input == null) return null;
			return input.Substring(Math.Max(input.Length - 4, 0));
		}

		private TokenEvent(string eventName, IDictionary<string, object> eventParams) : base("user-tokens", eventName, eventParams) { }
	}
}
