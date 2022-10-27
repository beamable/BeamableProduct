using Beamable.Common.Content;
using Beamable.EasyFeatures.BasicLobby;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Experimental.Api.Matchmaking;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class MatchmakingRoomPlayerSystem : MatchmakingRoomView.IDependencies
	{
		protected BeamContext BeamContext;
		protected SimGameType SelectedGameType;
		
		public bool IsVisible { get; set; }
		public int MaxPlayers => SelectedGameType.CalculateMaxPlayers();
		public int CurrentPlayers => SlotsData.Count(slot => slot.PlayerId != string.Empty);
		public bool IsPlayerAdmin => PlayerIds[0] == BeamContext.PlayerId.ToString();
		public bool IsPlayerReady => BeamContext.Lobby.GetCurrentPlayer(BeamContext.PlayerId.ToString()).IsReady();
		public bool IsMatchStarting { get; set; }
		
		public List<MatchmakingSlotPresenter.ViewData> SlotsData => BuildViewData();
		
		public List<string> PlayerIds = new List<string>();
		public List<string> PlayerTeams = new List<string>();
		public List<bool> PlayerReadiness = new List<bool>();

		public MatchmakingRoomPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
		}
		
		public bool IsServerReady()
		{
			return MaxPlayers == PlayerReadiness.Count(b => b);
		}
		
		public async void SetPlayerReady(bool value)
		{
			await BeamContext.Lobby.AddTags(
				new List<Tag> {new Tag(LobbyExtensions.TAG_PLAYER_READY, value.ToString().ToLower())}, true);
		}

		public virtual void RegisterMatch(SimGameType simGameType, Match match, List<LobbyPlayer> players)
		{
			SelectedGameType = simGameType;
			BuildClientData(match, players,ref PlayerIds, ref PlayerReadiness, ref PlayerTeams);
		}

		public void BuildClientData(Match match, List<LobbyPlayer> players, ref List<string> names, ref List<bool> readiness, ref List<string> teams)
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
				teams.Add(team.name);
				names.AddRange(team.players);
			}

			GuaranteeInitList(ref readiness);
			readiness.AddRange(players.Select(player => player.IsReady()));
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
					entry.IsReady = PlayerReadiness[i];
				}
				else
				{
					entry.PlayerId = string.Empty;
					entry.Team = string.Empty;
					entry.IsReady = false;
				}

				slotsData.Add(entry);
			}

			return slotsData;
		}
	}
}
