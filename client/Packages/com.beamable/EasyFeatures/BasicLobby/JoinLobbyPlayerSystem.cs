using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Matchmaking;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class JoinLobbyPlayerSystem : JoinLobbyView.IDependencies
	{
		private readonly MatchmakingService _matchmakingService;
		private readonly IUserContext _ctx;

		public delegate Promise<List<LobbiesListEntryPresenter.Data>> GetData();
		public GetData GetDataAction;

		public List<SimGameType> GameTypes { get; private set; } = new List<SimGameType>();
		public bool IsVisible { get; set; }
		public int CurrentlySelectedGameType { get; set; }
		public string CurrentFilter { get; private set; }
		public List<LobbiesListEntryPresenter.Data> LobbiesData => FilterData();

		protected readonly Dictionary<string, List<string>> PerGameTypeLobbiesNames = new Dictionary<string, List<string>>();
		protected readonly Dictionary<string, List<int>> PerGameTypeLobbiesCurrentPlayers = new Dictionary<string, List<int>>();
		protected readonly Dictionary<string, List<int>> PerGameTypeLobbiesMaxPlayers = new Dictionary<string, List<int>>();
		
		protected virtual string SelectedGameTypeId => GameTypes[CurrentlySelectedGameType].Id;
		protected virtual IReadOnlyList<string> Names => PerGameTypeLobbiesNames[SelectedGameTypeId];
		protected virtual IReadOnlyList<int> CurrentPlayers => PerGameTypeLobbiesCurrentPlayers[SelectedGameTypeId];
		protected virtual IReadOnlyList<int> MaxPlayers => PerGameTypeLobbiesMaxPlayers[SelectedGameTypeId];

		public JoinLobbyPlayerSystem(MatchmakingService matchmakingService, IUserContext ctx)
		{
			_matchmakingService = matchmakingService;
			_ctx = ctx;
			
			CurrentlySelectedGameType = 0;
			CurrentFilter = string.Empty;
		}
		
		public virtual void RegisterLobbyData(SimGameType gameType,
		                                      List<LobbiesListEntryPresenter.Data> data) =>
			RegisterLobbyData(gameType.Id, data);

		public virtual void RegisterLobbyData(SimGameTypeRef gameTypeRef,
		                                      List<LobbiesListEntryPresenter.Data> data) =>
			RegisterLobbyData(gameTypeRef.Id, data);

		public virtual void RegisterLobbyData(string gameTypeId,
		                                      List<LobbiesListEntryPresenter.Data> data)
		{
			_ = PerGameTypeLobbiesNames.TryGetValue(gameTypeId, out var names);
			_ = PerGameTypeLobbiesCurrentPlayers.TryGetValue(gameTypeId, out var currentPlayers);
			_ = PerGameTypeLobbiesMaxPlayers.TryGetValue(gameTypeId, out var maxPlayers);
			
			BuildLobbiesClientData(data, ref names, ref currentPlayers, ref maxPlayers);
			
			if (PerGameTypeLobbiesNames.ContainsKey(gameTypeId))
			{
				PerGameTypeLobbiesNames[gameTypeId] = names;
			}
			else
			{
				PerGameTypeLobbiesNames.Add(gameTypeId, names);
			}
			
			if (PerGameTypeLobbiesCurrentPlayers.ContainsKey(gameTypeId))
			{
				PerGameTypeLobbiesCurrentPlayers[gameTypeId] = currentPlayers;
			}
			else
			{
				PerGameTypeLobbiesCurrentPlayers.Add(gameTypeId, currentPlayers);
			}
			
			if (PerGameTypeLobbiesMaxPlayers.ContainsKey(gameTypeId))
			{
				PerGameTypeLobbiesMaxPlayers[gameTypeId] = maxPlayers;
			}
			else
			{
				PerGameTypeLobbiesMaxPlayers.Add(gameTypeId, maxPlayers);
			}
		}
		
		/// <summary>
		/// The actual data transformation function that converts lobbies entries into data that is relevant for our <see cref="JoinLobbyView.IDependencies"/>. 
		/// </summary>
		public virtual void BuildLobbiesClientData(List<LobbiesListEntryPresenter.Data> entries,
		                                               ref List<string> names,
		                                               ref List<int> currentPlayers,
		                                               ref List<int> maxPlayers)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}

			GuaranteeInitList(ref names);
			names.AddRange(entries.Select(entry => entry.Name));

			GuaranteeInitList(ref currentPlayers);
			currentPlayers.AddRange(entries.Select(entry => entry.CurrentPlayers));

			GuaranteeInitList(ref maxPlayers);
			maxPlayers.AddRange(entries.Select(entry => entry.MaxPlayers));
		}

		public virtual void ClearLobbyData(SimGameType gameType) => ClearLobbyData(gameType.Id);
		
		public virtual void ClearLobbyData(SimGameTypeRef gameTypeRef) => ClearLobbyData(gameTypeRef.Id);

		public virtual void ClearLobbyData(string gameTypeId)
		{
			PerGameTypeLobbiesNames.Remove(gameTypeId);
			PerGameTypeLobbiesCurrentPlayers.Remove(gameTypeId);
			PerGameTypeLobbiesMaxPlayers.Remove(gameTypeId);
		}
		
		public async Promise ConfigureData()
		{
			List<LobbiesListEntryPresenter.Data> data = await GetDataAction.Invoke();
			RegisterLobbyData(SelectedGameTypeId, data);
		}

		public async Promise<List<LobbiesListEntryPresenter.Data>> GetTestData()
		{
			await Promise.Success.WaitForSeconds(2);
			return LobbiesTestDataHelper.GetTestLobbiesData(GameTypes[CurrentlySelectedGameType].maxPlayers);
		}

		public async Promise<List<LobbiesListEntryPresenter.Data>> FetchData()
		{
			await Promise.Success.WaitForSeconds(10);
			return LobbiesTestDataHelper.GetTestLobbiesData(GameTypes[CurrentlySelectedGameType].maxPlayers);
		}

		public void Setup(List<SimGameType> gameTypes)
		{
			GameTypes = gameTypes;
			GetDataAction = FetchData;
		}

		public void ApplyFilter(string filter)
		{
			CurrentFilter = filter;
		}

		private List<LobbiesListEntryPresenter.Data> FilterData()
		{
			int entriesCount = Names.Count;
			
			List<LobbiesListEntryPresenter.Data> data = new List<LobbiesListEntryPresenter.Data>();
				
			for (int i = 0; i < entriesCount; i++)
			{
				data.Add(new LobbiesListEntryPresenter.Data
				{
					Name = Names[i],
					CurrentPlayers = CurrentPlayers[i],
					MaxPlayers = MaxPlayers[i]
				});	
			}

			return CurrentFilter == string.Empty
				? data
				: data.Where(entry => entry.Name.Contains(CurrentFilter)).ToList();
		}
	}
}
