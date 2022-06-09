using Beamable.Common.Content;
using Beamable.EasyFeatures.Basicmatchmaking;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class StartMatchmakingPlayerSystem : StartMatchmakingView.IDependencies
	{
		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; set; }

		public void Setup(List<SimGameType> gameTypes)
		{
			GameTypes = gameTypes;
		}
	}
}
