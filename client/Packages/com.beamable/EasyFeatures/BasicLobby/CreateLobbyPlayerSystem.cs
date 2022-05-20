using Beamable.Common.Content;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyPlayerSystem : CreateLobbyView.IDependencies
	{
		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; private set; }

		public void Setup(List<SimGameType> gameTypes)
		{
			GameTypes = gameTypes;
			
			
		}
	}
}
