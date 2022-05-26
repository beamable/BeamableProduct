using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Lobbies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyPlayerSystem : CreateLobbyView.IDependencies
	{
		protected BeamContext BeamContext;

		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; set; }
		public int SelectedGameTypeIndex { get; set; }
		public Dictionary<string, LobbyRestriction> AccessOptions { get; } = new Dictionary<string, LobbyRestriction>();
		public int SelectedAccessOption { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public SimGameType SelectedGameType => GameTypes[SelectedGameTypeIndex];

		public virtual void Setup(BeamContext beamContext, List<SimGameType> gameTypes)
		{
			BeamContext = beamContext;
			GameTypes = gameTypes;

			ResetData();

			AccessOptions.Clear();
			AccessOptions.Add("Public", LobbyRestriction.Open);
			AccessOptions.Add("Private", LobbyRestriction.Closed);
		}

		public virtual bool ValidateConfirmButton()
		{
			return Name.Length > 5;
		}

		public virtual async Promise<Lobby> CreateLobby()
		{
			return await BeamContext.Lobby.Create(Name, AccessOptions.ElementAt(SelectedAccessOption).Value,
			                                      SelectedGameType.Id, Description,
			                                      maxPlayers: SelectedGameType.maxPlayers);
		}

		public virtual void ResetData()
		{
			SelectedGameTypeIndex = 0;
			SelectedAccessOption = 0;
			Name = string.Empty;
			Description = string.Empty;
		}
	}
}
