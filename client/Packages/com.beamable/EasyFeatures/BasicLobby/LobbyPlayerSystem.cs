using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbyPlayerSystem : LobbyView.IDependencies
	{
		protected BeamContext BeamContext;

		public List<LobbySlotPresenter.ViewData> SlotsData { get; set; } = new List<LobbySlotPresenter.ViewData>();
		public Lobby LobbyData { get; set; }
		public bool IsVisible { get; set; }
		public bool IsPlayerAdmin { get; set; }
		public bool IsPlayerReady { get; set; }
		public bool IsServerReady { get; set; }
		public bool IsMatchStarting { get; set; }

		public List<string> PlayerIds = new List<string>();
		public List<bool> PlayerReadiness = new List<bool>();

		public LobbyPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
		}

		public void Setup(Lobby data, bool isAdmin)
		{
			LobbyData = data;
			IsPlayerAdmin = isAdmin;
			IsPlayerReady = false;
			IsServerReady = false;
			IsMatchStarting = false;

			// TODO: configure players from lobby data
			//RegisterLobbyPlayers();
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
			for (int i = 0; i < LobbyData.maxPlayers; i++)
			{
				names.Add(entries[i].playerId);
			}
			
			GuaranteeInitList(ref readiness);
			for (int i = 0; i < LobbyData.maxPlayers; i++)
			{
				//readiness.Add(entries[i].tags);
				// TEMPORARY
				readiness.Add(true);
			}
			
			SlotsData = BuildViewData();
		}

		public List<LobbySlotPresenter.ViewData> BuildViewData()
		{
			List<LobbySlotPresenter.ViewData> data = new List<LobbySlotPresenter.ViewData>(LobbyData.maxPlayers);

			for (int i = 0; i < LobbyData.maxPlayers; i++)
			{
				LobbySlotPresenter.ViewData entry = new LobbySlotPresenter.ViewData();

				if (i < LobbyData.players.Count)
				{
					entry.Name = PlayerIds[i];
					entry.IsReady = PlayerReadiness[i];
				}
				else
				{
					entry.Name = string.Empty;
					entry.IsReady = false;
				}

				data.Add(entry);
			}

			return data;
		}
	}
}
