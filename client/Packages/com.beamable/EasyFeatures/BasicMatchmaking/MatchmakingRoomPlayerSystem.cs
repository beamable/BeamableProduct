using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Experimental.Api.Matchmaking;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class MatchmakingRoomPlayerSystem : MatchmakingRoomView.IDependencies
	{
		protected BeamContext BeamContext;
		protected SimGameType SelectedGameType;
		
		public bool IsVisible { get; set; }
		public string Name { get; }
		public int MaxPlayers { get; }
		public int? CurrentlySelectedPlayerIndex { get; set; }
		public int CurrentPlayers { get; }
		public bool IsPlayerAdmin { get; }
		public bool IsPlayerReady { get; }
		public bool IsMatchStarting { get; }
		
		public List<MatchmakingSlotPresenter.ViewData> SlotsData => BuildViewData();
		
		public List<string> PlayerIds = new List<string>();
		public List<string> PlayerTeams = new List<string>();

		public MatchmakingRoomPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
			CurrentlySelectedPlayerIndex = null;
		}
		
		public bool IsServerReady()
		{
			return PlayerIds.Count == SelectedGameType.maxPlayers;
		}

		public void SetCurrentSelectedPlayer(int slotIndex)
		{
			string playerId = PlayerIds[slotIndex];

			// We don't want to interact with card for self
			if (playerId == BeamContext.PlayerId.ToString())
			{
				return;
			}

			if (CurrentlySelectedPlayerIndex == slotIndex)
			{
				CurrentlySelectedPlayerIndex = null;
			}
			else
			{
				CurrentlySelectedPlayerIndex = slotIndex;
			}
		}

		public Promise LeaveMatch()
		{
			throw new System.NotImplementedException();
		}

		public async Promise StartMatch()
		{
			// TODO: Implement match start here 
			await Promise.Success.WaitForSeconds(3);
		}
		
		public async Promise KickPlayer()
		{
			await Promise.Success.WaitForSeconds(3);
			
			if (CurrentlySelectedPlayerIndex == null)
			{
				return;
			}

			// LobbyPlayer lobbyPlayer = BeamContext.Lobby.Players[CurrentlySelectedPlayerIndex.Value];
			// await BeamContext.Lobby.KickPlayer(lobbyPlayer.playerId);
			CurrentlySelectedPlayerIndex = null;
		}
		
		public virtual void RegisterMatch(SimGameType simGameType, Match match)
		{
			SelectedGameType = simGameType;
			BuildClientData(match, ref PlayerIds, ref PlayerTeams);
		}

		public void BuildClientData(Match match, ref List<string> names, ref List<string> teams)
		{
			void GuaranteeInitList<T>(ref List<T> toInit)
			{
				if (toInit != null) toInit.Clear();
				else toInit = new List<T>();
			}

			GuaranteeInitList(ref names);
			GuaranteeInitList(ref teams);
			foreach (Team team in match.teams)
			{
				foreach (string player in team.players)
				{
					names.Add(player);
					teams.Add(team.name);
				}
			}
		}

		private List<MatchmakingSlotPresenter.ViewData> BuildViewData()
		{
			List<MatchmakingSlotPresenter.ViewData> slotsData = new List<MatchmakingSlotPresenter.ViewData>(MaxPlayers);

			for (int i = 0; i < MaxPlayers; i++)
			{
				MatchmakingSlotPresenter.ViewData entry = new MatchmakingSlotPresenter.ViewData();

				if (i < PlayerIds.Count)
				{
					entry.PlayerId = PlayerIds[i];
					entry.Team = PlayerTeams[i];
					
					if (CurrentlySelectedPlayerIndex != null)
					{
						entry.IsUnfolded = CurrentlySelectedPlayerIndex == i;
					}
					else
					{
						entry.IsUnfolded = false;
					}
				}
				else
				{
					entry.PlayerId = string.Empty;
					entry.Team = string.Empty;
				}

				slotsData.Add(entry);
			}

			return slotsData;
		}
	}
}
