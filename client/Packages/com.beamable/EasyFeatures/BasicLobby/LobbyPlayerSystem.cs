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
		public string Id => BeamContext.Lobby.Id;
		public string Name => BeamContext.Lobby.Name;
		public string Description => BeamContext.Lobby.Description;
		public int MaxPlayers => BeamContext.Lobby.MaxPlayers;
		public int? CurrentlySelectedPlayerIndex { get; set; }
		public bool IsVisible { get; set; }
		public bool IsPlayerAdmin => BeamContext.Lobby.Host == BeamContext.PlayerId.ToString();
		public bool IsPlayerReady => BeamContext.Lobby.GetCurrentPlayer(BeamContext.PlayerId.ToString()).IsReady();
		public bool IsServerReady => false;
		public bool IsMatchStarting => false;
		public int CurrentPlayers => SlotsData.Count(slot => slot.PlayerId != string.Empty);

		public List<string> PlayerIds = new List<string>();
		public List<bool> PlayerReadiness = new List<bool>();

		public LobbyPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
			CurrentlySelectedPlayerIndex = null;
		}

		public async Promise LeaveLobby()
		{
			CurrentlySelectedPlayerIndex = null;
			await BeamContext.Lobby.Leave();
		}

		public async Promise KickPlayer()
		{
			if (CurrentlySelectedPlayerIndex == null)
			{
				return;
			}
			
			LobbyPlayer lobbyPlayer = BeamContext.Lobby.Players[CurrentlySelectedPlayerIndex.Value];
			await BeamContext.Lobby.KickPlayer(lobbyPlayer.playerId);
			CurrentlySelectedPlayerIndex = null;
		}

		public async void SetPlayerReady(bool value)
		{
			await BeamContext.Lobby.AddTags(new List<Tag>
			{
				new Tag(LobbyExtensions.TAG_PLAYER_READY, value.ToString().ToLower())
			}, true);
		}

		public bool SetCurrentSelectedPlayer(int slotIndex)
		{
			LobbyPlayer player = BeamContext.Lobby.Players[slotIndex];

			if (player.playerId == BeamContext.PlayerId.ToString())
			{
				return false;
			}

			CurrentlySelectedPlayerIndex = slotIndex;
			return true;
		}

		public void UpdateLobby(string name, string description)
		{
			Lobby lobby = BeamContext.Lobby.State.Copy();
			lobby.name = name;
			lobby.description = description;
			// TODO: invoke method for update after it will be created
			// BeamContext.Lobby.State.Set(lobby); // This one works locally
		}

		public async Promise StartMatch()
		{
			// TODO: Implement match start here 
			await Promise.Success.WaitForSeconds(3);
		}

		public virtual void RegisterLobbyPlayers(List<LobbyPlayer> data)
		{
			BuildClientData(data, ref PlayerIds, ref PlayerReadiness);
		}

		/// <summary>
		/// The actual data transformation function that converts lobbies entries into data that is relevant for our <see cref="LobbyView.IDependencies"/>. 
		/// </summary>
		public virtual void BuildClientData(List<LobbyPlayer> entries, ref List<string> names, ref List<bool> readiness)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}

			GuaranteeInitList(ref names);
			names.AddRange(entries.Select(player => player.playerId));

			GuaranteeInitList(ref readiness);
			readiness.AddRange(entries.Select(player => player.IsReady()));
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
