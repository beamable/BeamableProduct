using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbyPlayerSystem : LobbyView.IDependencies
	{
		protected BeamContext BeamContext;

		public List<LobbySlotPresenter.ViewData> SlotsData => BuildViewData();
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int MaxPlayers { get; set; }
		public bool IsVisible { get; set; }
		public bool IsPlayerAdmin { get; set; }
		public bool IsPlayerReady { get; set; }
		public bool IsServerReady { get; set; }
		public bool IsMatchStarting { get; set; }
		public int CurrentPlayers => SlotsData.Count(slot => slot.PlayerId != string.Empty);

		public List<string> PlayerIds = new List<string>();
		public List<bool> PlayerReadiness = new List<bool>();

		public LobbyPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
		}

		public void Setup(string lobbyId, string lobbyName, string lobbyDescription, int maxPlayers, bool isAdmin, List<LobbyPlayer> players) // Players list is temporary
		{
			Id = lobbyId;
			Name = lobbyName;
			Description = lobbyDescription;
			MaxPlayers = maxPlayers;
			IsPlayerAdmin = isAdmin;
			IsPlayerReady = false;
			IsServerReady = false;
			IsMatchStarting = false;

			RegisterLobbyPlayers(players);
		}

		public async Promise LeaveLobby()
		{
			await BeamContext.Lobby.Leave();
		}

		public virtual void RegisterLobbyPlayers(List<LobbyPlayer> data)
		{
			BuildClientData(data, ref PlayerIds, ref PlayerReadiness);
		}

		/// <summary>
		/// The actual data transformation function that converts lobbies entries into data that is relevant for our <see cref="LobbyView.IDependencies"/>. 
		/// </summary>
		public virtual void BuildClientData(List<LobbyPlayer> entries,
		                                    ref List<string> names,
		                                    ref List<bool> readiness)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}
			
			GuaranteeInitList(ref names);
			for (int i = 0; i < entries.Count; i++)
			{
				names.Add(entries[i].playerId);
			}
			
			GuaranteeInitList(ref readiness);
			for (int i = 0; i < entries.Count; i++)
			{
				//readiness.Add(entries[i].tags);
				// TEMPORARY
				readiness.Add(true);
			}
		}

		public List<LobbySlotPresenter.ViewData> BuildViewData()
		{
			List<LobbySlotPresenter.ViewData> data = new List<LobbySlotPresenter.ViewData>(MaxPlayers);

			for (int i = 0; i < MaxPlayers; i++)
			{
				LobbySlotPresenter.ViewData entry = new LobbySlotPresenter.ViewData();

				if (i < PlayerIds.Count)
				{
					entry.PlayerId = PlayerIds[i];
					entry.IsReady = PlayerReadiness[i];
				}
				else
				{
					entry.PlayerId = string.Empty;
					entry.IsReady = false;
				}

				data.Add(entry);
			}

			return data;
		}
	}
}
