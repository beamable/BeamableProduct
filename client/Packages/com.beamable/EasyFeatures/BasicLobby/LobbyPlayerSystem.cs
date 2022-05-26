using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbyPlayerSystem : LobbyView.IDependencies
	{
		protected BeamContext BeamContext;
		
		public Lobby LobbyData { get; set; }
		public List<LobbySlotPresenter.Data> SlotsData => BuildViewData();
		public bool IsVisible { get; set; }
		public bool IsPlayerAdmin { get; set; }
		public bool IsPlayerReady { get; set; }
		public bool IsServerReady { get; set; }
		public bool IsMatchStarting { get; set; }
		
		public List<string> PlayerNames;
		public List<bool> PlayerReadiness;
		public List<bool> SlotOccupation;

		public void Setup(BeamContext beamContext, Lobby data, bool isAdmin)
		{
			BeamContext = beamContext;
			LobbyData = data;
			IsPlayerAdmin = isAdmin;
			IsPlayerReady = false;
			IsServerReady = false;
			IsMatchStarting = false;

			PlayerNames = new List<string>(data.maxPlayers);
			PlayerReadiness = new List<bool>(data.maxPlayers);
			SlotOccupation = new List<bool>(data.maxPlayers);
			
			// TODO: configure players from lobby data
			//RegisterLobbyPlayers();
		}
		
		public async Promise LeaveLobby()
		{
			await BeamContext.Lobby.Leave();
		}

		public virtual void RegisterLobbyPlayers(List<LobbyPlayer> data)
		{
			BuildClientData(data, ref PlayerNames, ref PlayerReadiness, ref SlotOccupation);
		}

		/// <summary>
		/// The actual data transformation function that converts lobbies entries into data that is relevant for our <see cref="LobbyView.IDependencies"/>. 
		/// </summary>
		public virtual void BuildClientData(List<LobbyPlayer> entries,
		                                    ref List<string> names,
		                                    ref List<bool> readiness,
		                                    ref List<bool> slotOccupation)
		{
			// void GuaranteeInitList<T>(ref List<T> toInit)
			// {
			// 	if (toInit != null) toInit.Clear();
			// 	else toInit = new List<T>();
			// }
			//
			// GuaranteeInitList(ref names);
			// for (int i = 0; i < LobbyData.maxPlayers; i++)
			// {
			// 	names.Add(entries[i].Name);
			// }
			//
			// GuaranteeInitList(ref readiness);
			// for (int i = 0; i < LobbyData.maxPlayers; i++)
			// {
			// 	readiness.Add(entries[i].IsReady);
			// }
			//
			// GuaranteeInitList(ref slotOccupation);
			// for (int i = 0; i < LobbyData.maxPlayers; i++)
			// {
			// 	slotOccupation.Add(entries[i].IsOccupied);
			// }
		}

		private List<LobbySlotPresenter.Data> BuildViewData()
		{
			List<LobbySlotPresenter.Data> data = new List<LobbySlotPresenter.Data>(LobbyData.maxPlayers);

			for (int i = 0; i < LobbyData.maxPlayers; i++)
			{
				LobbySlotPresenter.Data entry = new LobbySlotPresenter.Data
				{
					Name = PlayerNames[i], IsReady = PlayerReadiness[i], IsOccupied = SlotOccupation[i]
				};
				
				data.Add(entry);
			}

			return data;
		}
	}
}
