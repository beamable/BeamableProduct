using Beamable.Common;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class InsideLobbyPlayerSystem : InsideLobbyView.IDependencies
	{
		public delegate Promise<List<LobbySlotPresenter.Data>> GetData();
		public GetData GetDataAction;

		public LobbiesListEntryPresenter.Data LobbyData { get; set; }
		public List<LobbySlotPresenter.Data> SlotsData => BuildViewData();
		
		public bool IsVisible { get; set; }
		public bool IsPlayerAdmin { get; set; }
		public bool IsPlayerReady { get; set; }
		public bool IsServerReady { get; set; }
		public bool IsMatchStarting { get; set; }

		public List<string> PlayerNames;
		public List<bool> PlayerReadiness;
		public List<bool> SlotOccupation;

		public void Setup(LobbiesListEntryPresenter.Data data, bool isAdmin, bool testMode)
		{
			LobbyData = data;
			IsPlayerAdmin = isAdmin;
			IsPlayerReady = false;
			IsServerReady = false;
			IsMatchStarting = false;

			PlayerNames = new List<string>(data.MaxPlayers);
			PlayerReadiness = new List<bool>(data.MaxPlayers);
			SlotOccupation = new List<bool>(data.MaxPlayers);

			if (!testMode)
			{
				GetDataAction = FetchData;
			}
			else
			{
				GetDataAction = GetTestData;
			}
		}

		public async Promise ConfigureData()
		{
			List<LobbySlotPresenter.Data> list = await GetDataAction.Invoke();
			RegisterLobbyPlayers(list);
		}

		public async Promise<List<LobbySlotPresenter.Data>> GetTestData()
		{
			await Promise.Success.WaitForSeconds(2);
			return LobbiesTestDataHelper.GetTestPlayersData(LobbyData.CurrentPlayers, LobbyData.MaxPlayers);
		}

		public async Promise<List<LobbySlotPresenter.Data>> FetchData()
		{
			await Promise.Success.WaitForSeconds(10);
			return LobbiesTestDataHelper.GetTestPlayersData(LobbyData.CurrentPlayers, LobbyData.MaxPlayers);
		}

		public virtual void RegisterLobbyPlayers(List<LobbySlotPresenter.Data> data)
		{
			BuildClientData(data, ref PlayerNames, ref PlayerReadiness, ref SlotOccupation);
		}

		/// <summary>
		/// The actual data transformation function that converts lobbies entries into data that is relevant for our <see cref="InsideLobbyView.IDependencies"/>. 
		/// </summary>
		public virtual void BuildClientData(List<LobbySlotPresenter.Data> entries,
		                                    ref List<string> names,
		                                    ref List<bool> readiness,
		                                    ref List<bool> slotOccupation)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}

			GuaranteeInitList(ref names);
			for (int i = 0; i < LobbyData.MaxPlayers; i++)
			{
				names.Add(entries[i].Name);
			}

			GuaranteeInitList(ref readiness);
			for (int i = 0; i < LobbyData.MaxPlayers; i++)
			{
				readiness.Add(entries[i].IsReady);
			}
			
			GuaranteeInitList(ref slotOccupation);
			for (int i = 0; i < LobbyData.MaxPlayers; i++)
			{
				slotOccupation.Add(entries[i].IsOccupied);
			}
		}

		private List<LobbySlotPresenter.Data> BuildViewData()
		{
			List<LobbySlotPresenter.Data> data = new List<LobbySlotPresenter.Data>(LobbyData.MaxPlayers);

			for (int i = 0; i < LobbyData.MaxPlayers; i++)
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
