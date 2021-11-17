using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Experimental.Api.Sim
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Multiplayer feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/multiplayer-feature">Multiplayer</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class GameRelayService
	{
		private PlatformService _platform;
		private PlatformRequester _requester;

		private const string GAME_RESULTS_EVENT_NAME = "gamerelay.game_results";

		public GameRelayService(PlatformService platform, PlatformRequester requester)
		{
			_platform = platform;
			_requester = requester;
		}

		public Promise<GameRelaySyncMsg> Sync(string roomId, GameRelaySyncMsg request)
		{
			return _requester.Request<GameRelaySyncMsg>(
				Method.POST,
				$"/object/gamerelay/{roomId}/sync",
				request
			);
		}

		/// <summary>
		/// Report the results of the game to the platform.
		/// </summary>
		/// <param name="roomId">The ID of the game session.</param>
		/// <param name="results">The array of `PlayerResult` to send to the platform for verification.</param>
		/// <returns>A promise of the confirmed game results</returns>
		public Promise<GameResults> ReportResults(string roomId, params PlayerResult[] results)
		{
			return _requester.Request<GameResults>(
				Method.POST,
				$"/object/gamerelay/{roomId}/results",
				new ResultsRequest(results)
			);
		}
	}

	/// <summary>
	/// This type defines the %ResultsRequest for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class ResultsRequest
	{
		public List<PlayerResult> results;

		public ResultsRequest(params PlayerResult[] results)
		{
			this.results = results.ToList();
		}
	}

	/// <summary>
	/// This type defines the %GameResults for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class GameResults
	{
		public bool cheatingDetected;
		public List<DeltaScoresByLeaderBoardId> deltaScores;
		public List<CurrencyChange> currenciesGranted;
		public List<Item> itemsGranted;
	}

	/// <summary>
	/// This type defines the %DeltaScoresByLeaderBoardId for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class DeltaScoresByLeaderBoardId
	{
		public string leaderBoardId;
		public double scoreDelta;
	}

	/// <summary>
	/// This type defines the %PlayerResult for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class PlayerResult
	{
		public long playerId;
		public double score;
		public int rank;
	}

	/// <summary>
	/// This type defines the %GameRelaySyncMsg for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class GameRelaySyncMsg
	{
		public long t;
		public List<GameRelayEvent> events = new List<GameRelayEvent>();
	}

	/// <summary>
	/// This type defines the %GameRelayEvent for the %GameRelayService.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.GameRelayService script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[Serializable]
	public class GameRelayEvent
	{
		public long t;
		public string type;
		public long origin;
		public string body;

		public void FromSimEvent(SimEvent evt)
		{
			t = evt.Frame;
			type = evt.Type;
			origin = 0;
			body = evt.Body;
		}

		public SimEvent ToSimEvent()
		{
			string origin = this.origin.ToString();
			string type = this.type;
			if (origin == "-1")
			{
				origin = "$system";
				if (type == "_c")
				{
					type = "$connect";
				}
				else if (type == "_d")
				{
					type = "$disconnect";
				}
				else if (type == "_a")
				{
					type = "$init";
				}
			}

			SimEvent result = new SimEvent(origin, type, body);
			result.Frame = t;

			return result;
		}
	}
}
