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
		private bool _testMode;
		private List<LobbiesListEntryPresenter.Data> _lobbiesData;

		public List<SimGameType> GameTypes { get; } = new List<SimGameType>();
		public bool IsVisible { get; set; }
		public int CurrentlySelectedGameType { get; set; }
		public string CurrentFilter { get; private set; }
		public List<LobbiesListEntryPresenter.Data> LobbiesData => FilterData();

		public JoinLobbyPlayerSystem(MatchmakingService matchmakingService, IUserContext ctx)
		{
			_matchmakingService = matchmakingService;
			_ctx = ctx;
		}

		public async Promise FetchData()
		{
			// TODO: remember to implement some cancellation mechanism in case if user clicked another game type toggle
			// before he got response from previous call
			
			if (_testMode)
			{
				await Promise.Success.WaitForSeconds(2);
				_lobbiesData = LobbiesTestDataHelper.GetTestLobbiesData(GameTypes[CurrentlySelectedGameType].maxPlayers);
			}
			else
			{
				await Promise.Success.WaitForSeconds(2);
				_lobbiesData = new List<LobbiesListEntryPresenter.Data>();
			}
		}

		public async Promise Setup(bool testMode, List<SimGameTypeRef> gameTypes)
		{
			_testMode = testMode;

			// TODO: move this to a feature control because this will be also used in Create Lobby view
			foreach (SimGameTypeRef simGameTypeRef in gameTypes)
			{
				SimGameType simGameType = await simGameTypeRef.Resolve();
				GameTypes.Add(simGameType);
			}

			CurrentlySelectedGameType = 0;
			CurrentFilter = string.Empty;

			// We need some initial data before first Enrich will be called
			await FetchData();
		}

		public void ApplyFilter(string filter)
		{
			CurrentFilter = filter;
		}

		private List<LobbiesListEntryPresenter.Data> FilterData()
		{
			return CurrentFilter == string.Empty ? _lobbiesData : _lobbiesData.Where(data => data.Name.Contains(CurrentFilter)).ToList();
		}
	}
}
