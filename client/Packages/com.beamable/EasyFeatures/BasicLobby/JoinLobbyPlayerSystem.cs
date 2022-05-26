using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Lobbies;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class JoinLobbyPlayerSystem : JoinLobbyView.IDependencies
	{
		protected BeamContext BeamContext;

		public List<SimGameType> GameTypes { get; set; } = new List<SimGameType>();
		public bool IsVisible { get; set; }
		public int SelectedGameTypeIndex { get; set; }
		public int? SelectedLobbyIndex { get; set; }
		public string NameFilter { get; set; }
		public int CurrentPlayersFilter { get; set; }
		public int MaxPlayersFilter { get; set; }
		public List<Lobby> LobbiesData => BuildViewData();

		public readonly Dictionary<string, List<string>> PerGameTypeLobbiesNames = new Dictionary<string, List<string>>();
		public readonly Dictionary<string, List<List<LobbyPlayer>>> PerGameTypeLobbiesCurrentPlayers = new Dictionary<string, List<List<LobbyPlayer>>>();
		public readonly Dictionary<string, List<int>> PerGameTypeLobbiesMaxPlayers = new Dictionary<string, List<int>>();

		public virtual SimGameType SelectedGameType => GameTypes[SelectedGameTypeIndex];
		public virtual string SelectedGameTypeId => SelectedGameType.Id;
		public virtual IReadOnlyList<string> Names => PerGameTypeLobbiesNames[SelectedGameTypeId];
		public virtual IReadOnlyList<List<LobbyPlayer>> CurrentPlayers => PerGameTypeLobbiesCurrentPlayers[SelectedGameTypeId];
		public virtual IReadOnlyList<int> MaxPlayers => PerGameTypeLobbiesMaxPlayers[SelectedGameTypeId];

		public void Setup(BeamContext beamContext, List<SimGameType> gameTypes)
		{
			BeamContext = beamContext;
			GameTypes = gameTypes;
			
			SelectedGameTypeIndex = 0;
			SelectedLobbyIndex = null;
			NameFilter = string.Empty;
		}

		public async Promise GetLobbies()
		{
			LobbyQueryResponse response = await BeamContext.Lobby.FindLobbies();
			// TODO: add some filtering here??

			RegisterLobbyData(SelectedGameTypeId, response.results);
		}

		public virtual void RegisterLobbyData(SimGameType gameType,
		                                      List<Lobby> data) =>
			RegisterLobbyData(gameType.Id, data);

		public virtual void RegisterLobbyData(SimGameTypeRef gameTypeRef,
		                                      List<Lobby> data) =>
			RegisterLobbyData(gameTypeRef.Id, data);

		public virtual void RegisterLobbyData(string gameTypeId,
		                                      List<Lobby> data)
		{
			PerGameTypeLobbiesNames.TryGetValue(gameTypeId, out var names);
			PerGameTypeLobbiesCurrentPlayers.TryGetValue(gameTypeId, out var currentPlayers);
			PerGameTypeLobbiesMaxPlayers.TryGetValue(gameTypeId, out var maxPlayers);
			
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
		public virtual void BuildLobbiesClientData(List<Lobby> entries,
		                                           ref List<string> names,
		                                           ref List<List<LobbyPlayer>> currentPlayers,
		                                           ref List<int> maxPlayers)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}

			GuaranteeInitList(ref names);
			names.AddRange(entries.Select(lobby => lobby.name));

			GuaranteeInitList(ref currentPlayers);
			currentPlayers.AddRange(entries.Select(lobby => lobby.players));

			GuaranteeInitList(ref maxPlayers);
			maxPlayers.AddRange(entries.Select(lobby => lobby.maxPlayers));
		}

		public virtual void ClearLobbyData(SimGameType gameType) => ClearLobbyData(gameType.Id);

		public virtual void ClearLobbyData(SimGameTypeRef gameTypeRef) => ClearLobbyData(gameTypeRef.Id);

		public virtual void ClearLobbyData(string gameTypeId)
		{
			PerGameTypeLobbiesNames.Remove(gameTypeId);
			PerGameTypeLobbiesCurrentPlayers.Remove(gameTypeId);
			PerGameTypeLobbiesMaxPlayers.Remove(gameTypeId);
		}

		public void OnLobbySelected(int? lobbyIndex)
		{
			SelectedLobbyIndex = lobbyIndex;
		}

		public virtual bool CanJoinLobby()
		{
			if (SelectedLobbyIndex == null)
			{
				return false;
			}
			
			return LobbiesData[SelectedLobbyIndex.Value].players.Count <
				LobbiesData[SelectedLobbyIndex.Value].maxPlayers;
		}

		public void ApplyFilter(string name) => ApplyFilter(name, CurrentPlayers.Count, MaxPlayers.Count);

		public void ApplyFilter(string name, int currentPlayers, int maxPlayers)
		{
			NameFilter = name;
			CurrentPlayersFilter = currentPlayers;
			MaxPlayersFilter = maxPlayers;
		}

		public virtual List<Lobby> BuildViewData()
		{
			int entriesCount = Names.Count;
			
			List<Lobby> data = new List<Lobby>();
				
			for (int i = 0; i < entriesCount; i++)
			{
				data.Add(new Lobby
				{
					name = Names[i],
					players = CurrentPlayers[i],
					maxPlayers = MaxPlayers[i]
				});	
			}

			return NameFilter == string.Empty
				? data
				: data.Where(entry => entry.name.Contains(NameFilter)).ToList();
		}
		
		public async Promise JoinLobby(string lobbyId)
		{
			await BeamContext.Lobby.Join(lobbyId);
		}
	}
}
