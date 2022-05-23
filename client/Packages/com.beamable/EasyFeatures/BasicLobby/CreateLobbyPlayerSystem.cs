using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Matchmaking;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyPlayerSystem : CreateLobbyView.IDependencies
	{
		protected readonly MatchmakingService MatchmakingService;
		protected readonly IUserContext Ctx;
		
		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; set; }
		public int SelectedGameType { get; set; }
		public Dictionary<string, bool> AccessOptions { get; } = new Dictionary<string, bool>();
		public int SelectedAccessOption { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public CreateLobbyPlayerSystem(MatchmakingService matchmakingService, IUserContext ctx)
		{
			MatchmakingService = matchmakingService;
			Ctx = ctx;
			
			SelectedGameType = 0;
			SelectedAccessOption = 0;
			Name = string.Empty;
			Description = string.Empty;
			
			AccessOptions.Add("Private", false);
			AccessOptions.Add("Public", true);
		}

		public virtual void Setup(List<SimGameType> gameTypes)
		{
			GameTypes = gameTypes;
		}

		public virtual bool ValidateConfirmButton()
		{
			return Name.Length > 5;
		}

		public virtual void ConfirmButtonClicked()
		{
			// TODO: Create lobby with name, description, game type and access
		}

		public virtual void ResetData()
		{
			SelectedGameType = 0;
			SelectedAccessOption = 0;
			Name = string.Empty;
			Description = string.Empty;
		}
	}
}
