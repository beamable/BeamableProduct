using Beamable.Common;
using Beamable.Common.Api.Social;
using Beamable.Common.Player;
using System;

namespace Beamable.Player
{
	[Serializable]
	public class PlayerFriends : AbsObservableReadonlyList<Friend>
	{
		private ISocialApi _socialApi;
		
		public PlayerFriends(ISocialApi socialApi)
		{
			_socialApi = socialApi;
		}
		
		protected override async Promise PerformRefresh()
		{
			var friendsList = await _socialApi.Get();
			
			SetData(friendsList.friends);
		}
	}
}
