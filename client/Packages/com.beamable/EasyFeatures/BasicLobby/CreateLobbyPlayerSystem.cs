using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Matchmaking;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyPlayerSystem : CreateLobbyView.IDependencies
	{
		private readonly MatchmakingService _matchmakingService;
		private readonly IUserContext _ctx;
		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; private set; }
		public int SelectedGameType { get; set; }
		public Dictionary<string, bool> AccessOptions { get; } = new Dictionary<string, bool>();
		public int SelectedAccessOption { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public CreateLobbyPlayerSystem(MatchmakingService matchmakingService, IUserContext ctx)
		{
			_matchmakingService = matchmakingService;
			_ctx = ctx;
			
			ResetData();
			
			AccessOptions.Add("Private", false);
			AccessOptions.Add("Public", true);
		}

		public void Setup(List<SimGameType> gameTypes)
		{
			GameTypes = gameTypes;
		}

		public bool ValidateConfirmButton()
		{
			// Add more conditions if necessary
			return Name.Length > 5;
		}

		public void ConfirmButtonClicked()
		{
			// Create lobby with name, description, game type and access
		}

		public void ResetData()
		{
			SelectedGameType = 0;
			SelectedAccessOption = 0;
			Name = string.Empty;
			Description = string.Empty;
		}
	}
}
