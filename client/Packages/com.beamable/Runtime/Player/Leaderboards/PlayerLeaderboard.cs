using Beamable.Api;
using Beamable.Api.Autogenerated.Leaderboards;
using Beamable.Api.Autogenerated.Models;
using Beamable.Api.Connectivity;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using Beamable.Common.Player;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Beamable.Player
{

	public interface IPlayerLeaderboardFriend
	{
		string boardId { get; }
		void SetSize(long boardSize);
		void SetCurrentScore(RankEntry entry);
		void SetCurrentFriendScore(RankEntry entry);
		void Save();
		Promise<LeaderboardAssignmentInfo> GetAssignment(bool joinBoard);
		void Hydrate(IDependencyProvider provider);
	}

	/// <summary>
	/// A <see cref="PlayerLeaderboard"/> contains scores and metadata for a Beamable player leaderboard.
	/// Use the <see cref="TopScores"/> property to see the high scores for the leaderboard.
	/// If the board is client-authoritative, use the <see cref="SetScore"/> method to set the player's score.
	/// </summary>
	[Serializable]
	public class PlayerLeaderboard : IPlayerLeaderboardFriend
	{
		private IDependencyProvider _provider;
		private ILeaderboardsApi _api;
		private IUserContext _userContext;
		private PlayerStats _statsApi;
		private Promise<LeaderboardAssignmentInfo> _assignment;

		/// <summary>
		/// The leaderboard id
		/// </summary>
		public string boardId;
		string IPlayerLeaderboardFriend.boardId => boardId;

		/// <summary>
		/// The size of the leaderboard. This represents how many players are participating on the leaderboard.
		/// The size may not match <see cref="TopScores"/> or <see cref="NearbyScores"/>, because
		/// there are more players on the board than available in those fields.
		/// </summary>
		public long boardSize;

		/// <summary>
		/// A <see cref="PlayerLeaderboardEntry"/> for the current player's standings in the leaderboard.
		/// </summary>
		public OptionalPlayerLeaderboardEntry myStandings = new OptionalPlayerLeaderboardEntry();

		/// <summary>
		/// A <see cref="PlayerLeaderboardEntry"/> for the current player's standings among their friends.
		/// Friends can be created using the <see cref="PlayerSocial"/> SDK, available through the <see cref="BeamContext.Social"/> accessor.
		/// </summary>
		public OptionalPlayerLeaderboardEntry myStandingsAmongFriends = new OptionalPlayerLeaderboardEntry();

		[SerializeField]
		private LeaderboardAddRequest _pendingScoreRequest;

		[SerializeField]
		private PlayerTopScoresList _topScores;
		[SerializeField]
		private PlayerFocusScoresList _nearbyScores;
		[SerializeField]
		private PlayerFriendScoresList _friendScores;

		internal IPlayerLeaderboardsFriend collection;
		private IConnectivityService _connectivity;

		// don't cache these views.
		private PlayerCollectionScoresListDictionary _playerCollectionViews =
			new PlayerCollectionScoresListDictionary();

		// don't cache these views either
		private PlayerFocusScoresListDictionary _playerViews =
			new PlayerFocusScoresListDictionary();

		public PlayerLeaderboard(IDependencyProvider provider)
		{
			_provider = provider;
			Hydrate(provider);
		}

		/// <summary>
		/// A <see cref="PlayerScoreList"/> view for the top scores on the leaderboard.
		/// <para>
		/// By default, there will be 10 entries in the list. More can be loaded with the
		/// <see cref="PlayerTopScoresList.LoadCount(int)"/> method.
		/// </para>
		/// <para>
		/// After fetching this data once, it will be saved to disk so that is available again
		/// in offline mode.
		/// </para>
		/// <para>
		/// Update the scores using the <see cref="PlayerTopScoresList.Refresh()"/> method.
		/// </para>
		/// </summary>
		public PlayerTopScoresList TopScores
		{
			get
			{
				if (_topScores == null)
				{
					_topScores = new PlayerTopScoresList(this, _provider);
					var _ = _topScores.Refresh();
				}
				return _topScores;
			}
		}


		/// <summary>
		/// A <see cref="PlayerFocusScoresList"/> view for the scores on the leaderboard that are
		/// near the current player. The view will have scores above and below the current player's entry.
		/// <para>
		/// By default, there will be 10 entries in the list. More can be loaded with the
		/// <see cref="PlayerFocusScoresList.LoadCount(int)"/> method.
		/// </para>
		/// <para>
		/// After fetching this data once, it will be saved to disk so that is available again
		/// in offline mode.
		/// </para>
		/// <para>
		/// Update the scores using the <see cref="PlayerFocusScoresList.Refresh()"/> method.
		/// </para>
		/// </summary>
		public PlayerFocusScoresList NearbyScores
		{
			get
			{
				if (_nearbyScores == null)
				{
					_nearbyScores = new PlayerFocusScoresList(this, _provider);
					var _ = _nearbyScores.Refresh();
				}

				return _nearbyScores;
			}
		}


		/// <summary>
		/// A <see cref="PlayerFriendScoresList"/> view for the scores of the current player's
		/// friends. 
		/// <para>
		/// By default, there will be 10 entries in the list. More can be loaded with the
		/// <see cref="PlayerFriendScoresList.LoadCount(int)"/> method.
		/// </para>
		/// <para>
		/// After fetching this data once, it will be saved to disk so that is available again
		/// in offline mode.
		/// </para>
		/// <para>
		/// Friends can be created using the <see cref="PlayerSocial"/> SDK, available through
		/// the <see cref="BeamContext.Social"/> accessor.
		/// </para>
		/// <para>
		/// Update the scores using the <see cref="PlayerFriendScoresList.Refresh()"/> method.
		/// </para>
		/// </summary>
		public PlayerFriendScoresList FriendScores
		{
			get
			{
				if (_friendScores == null)
				{
					_friendScores = new PlayerFriendScoresList(this, _provider);
					var _ = _friendScores.Refresh();
				}

				return _friendScores;
			}
		}

		/// <summary>
		/// Get a <see cref="PlayerCollectionScoresList"/> view for the scores in the leaderboard
		/// for the requested playerIds.
		/// <para>
		/// This data is not cached, ever.
		/// </para>
		/// </summary>
		/// <param name="playerIds">An array of playerIds that should be in the resulting
		/// leaderboard view. If the player is not in the leaderboard, there will be no
		/// entry in the resulting view.</param>
		/// <returns>A <see cref="PlayerCollectionScoresList"/> for the given players</returns>
		public PlayerCollectionScoresList GetScoresForPlayers(params long[] playerIds)
		{
			var hash = 1L;
			foreach (var id in playerIds)
			{
				hash = CombineHashCodes(id, hash);
			}
			var viewName = $"playersHash_{hash}";
			if (!_playerCollectionViews.TryGetValue(viewName, out var view))
			{
				_playerCollectionViews[viewName] = view = new PlayerCollectionScoresList(this, _provider);
			}
			view.playerIds = playerIds;
			var _ = view.Refresh();
			return view;
		}

		/// <summary>
		/// Get a <see cref="PlayerFocusScoresList"/> view for the leaderboard, centered around
		/// the requester playerId
		/// <para>
		/// This data is not cached, ever.
		/// </para>
		/// </summary>
		/// <param name="playerId">The playerId of the player to find nearby scores</param>
		/// <param name="size">The number of entries to return</param>
		/// <returns>A <see cref="PlayerFocusScoresList"/> for the given players</returns>
		public PlayerFocusScoresList GetScoresNearPlayer(long playerId, int size = 10)
		{
			var viewName = $"playerHash_{playerId}";
			if (!_playerViews.TryGetValue(viewName, out var view))
			{
				_playerViews[viewName] = view = new PlayerFocusScoresList(this, _provider);
			}

			view.playerId = playerId;
			return view.LoadCount(size);
		}

		// TODO: move to shared code somewhere
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static long CombineHashCodes(long h1, long h2)
		{
			return (((h1 << 5) + h1) ^ h2);
		}

		private Promise<LeaderboardAssignmentInfo> RefreshAssignment(bool joinBoard = false)
		{
			_assignment = _api.GetAssignment(boardId, joinBoard)
							  .Recover(ex =>
							  {
								  if (ex is PlatformRequesterException err && err.Error.status == 404)
								  {
									  return new LeaderboardAssignmentInfo
									  {
										  leaderboardId = boardId,
										  playerId = _userContext.UserId
									  };
								  }
								  throw ex;
							  });
			return _assignment;
		}

		Promise<LeaderboardAssignmentInfo> IPlayerLeaderboardFriend.GetAssignment(bool joinBoard)
		{
			return GetLocalAssignment(joinBoard);
		}


		void Hydrate(IDependencyProvider provider)
		{
			// re-assign dependencies
			_provider = provider;
			_api = provider.GetService<ILeaderboardsApi>();
			_userContext = provider.GetService<IUserContext>();
			_statsApi = provider.GetService<BeamContext>().Stats;

			_connectivity = provider.GetService<IConnectivityService>();
			_connectivity.OnConnectivityChanged += OnConnectionChanged;
			_connectivity.OnReconnectOnce(async () =>
			{
				await UpdateLocalScore();
				await SetPendingScore();
			});

			// hydrate nested data structures 
			(_topScores as IPlayerScoreListFriend)?.Hydrate(this, _provider);
			(_friendScores as IPlayerScoreListFriend)?.Hydrate(this, _provider);

			_nearbyScores.playerId = _userContext.UserId;
			(_nearbyScores as IPlayerScoreListFriend)?.Hydrate(this, _provider);
		}

		private void OnConnectionChanged(bool hasConnection)
		{
			if (!hasConnection) return; // nothing to do
			if (!HasPendingScoreRequest) return; // nothing to do

			var _ = SetPendingScore();
		}

		bool HasPendingScoreRequest => _pendingScoreRequest != null && _pendingScoreRequest.id > 0;

		void IPlayerLeaderboardFriend.Hydrate(IDependencyProvider provider) => Hydrate(provider);

		async Promise<LeaderboardAssignmentInfo> GetLocalAssignment(bool joinBoard)
		{
			if (_assignment == null)
			{
				var assignment = await RefreshAssignment(joinBoard);
				return assignment;
			}
			else
			{
				return await _assignment;
			}
		}

		private async Promise UpdateLocalScore()
		{
			var info = await GetLocalAssignment(false);
			var response = await _api.ObjectGetView(info.leaderboardId, max: 0, outlier: _userContext.UserId);
			boardSize = response.lb.boardSize;
			var rank = response.lb.rankgt;
			if (rank.HasValue)
			{
				SetCurrentScore(rank.Value);
			}
		}

		/// <summary>
		/// <bold> This method requires that the board supports client authoritative writes. In content, that is specified with the <see cref="LeaderboardContent.permissions"/> field.</bold>
		/// <para>
		/// Set the score for the current player. 
		/// </para>
		/// <para>
		/// If the SDK is in offline mode, then the request will be cached and replayed the next time the SDK is online.
		/// </para>
		/// </summary>
		/// <param name="score">
		/// The new absolute score for the player.
		/// High scores are ranked higher.
		/// </param>
		/// <param name="stats">
		/// Every leaderboard entry has a set of stats, <see cref="PlayerLeaderboardEntry.stats"/>, that can hold
		/// metadata about the entry.
		/// </param>
		public async Promise SetScore(double score, Dictionary<string, string> stats = null)
		{

			var request = new LeaderboardAddRequest { id = _userContext.UserId, score = score };
			CreateRequestObject(request);
			PrepareStats(stats);
			await SetPendingScore();
		}


		/// <summary>
		/// <bold> This method requires that the board supports client authoritative writes. In content, that is specified with the <see cref="LeaderboardContent.permissions"/> field.</bold>
		/// <para>
		/// Increments the score for the current player. 
		/// </para>
		/// <para>
		/// If the SDK is in offline mode, then the request will be cached and replayed the next time the SDK is online.
		/// If multiple calls are made in offline mode, they will be coalesced into a single API call.
		/// </para>
		/// </summary>
		/// <param name="change">
		/// The increment for the player's score. Negative numbers will cause the player's score to lower.
		/// High scores are ranked higher.
		/// </param>
		/// <param name="stats">
		/// Every leaderboard entry has a set of stats, <see cref="PlayerLeaderboardEntry.stats"/>, that can hold
		/// metadata about the entry.
		/// </param>
		public async Promise IncrementScore(double change, Dictionary<string, string> stats = null)
		{
			var request = new LeaderboardAddRequest
			{
				id = _userContext.UserId,
				score = change
			};
			request.increment.Set(true);
			CreateRequestObject(request);
			PrepareStats(stats);
			await SetPendingScore();
		}

		/// <summary>
		/// <bold> This method requires that the board supports client authoritative writes. In content, that is specified with the <see cref="LeaderboardContent.permissions"/> field.</bold>
		/// <para>
		/// Increments the score for the current player. 
		/// </para>
		/// <para>
		/// If the SDK is in offline mode, then the request will be cached and replayed the next time the SDK is online.
		/// If multiple calls are made in offline mode, they will be coalesced into a single API call.
		/// </para>
		/// </summary>
		/// <param name="change">
		/// The increment for the player's score. Negative numbers will cause the player's score to lower.
		/// High scores are ranked higher.
		/// </param>
		/// <param name="minScore">
		/// The resulting score will be capped by the min score.
		/// </param>
		/// <param name="maxScore">
		/// The resulting score will be capped by the min score.
		/// </param>
		/// <param name="stats">
		/// Every leaderboard entry has a set of stats, <see cref="PlayerLeaderboardEntry.stats"/>, that can hold
		/// metadata about the entry.
		/// </param>
		public async Promise IncrementScore(double change, double minScore, double maxScore, Dictionary<string, string> stats = null)
		{
			var request = new LeaderboardAddRequest
			{
				id = _userContext.UserId,
				score = change
			};
			request.increment.Set(true);
			request.minScore.Set(minScore);
			request.maxScore.Set(maxScore);
			CreateRequestObject(request);
			PrepareStats(stats);
			await SetPendingScore();
		}


		void CreateRequestObject(LeaderboardAddRequest next)
		{
			if (!HasPendingScoreRequest)
			{
				_pendingScoreRequest = next;
				return;
			}

			_pendingScoreRequest.id = next.id;

			var nextIncrement = next.increment.GetOrElse(false);
			var wasIncrement = _pendingScoreRequest.increment.GetOrElse(false);

			if (nextIncrement && wasIncrement)
			{
				_pendingScoreRequest.score += next.score;
			}
			else if (nextIncrement && !wasIncrement)
			{
				// we cannot make it an increment, because it will inc by the previous SET score
				_pendingScoreRequest.score += next.score;
			}
			else if (!nextIncrement && !wasIncrement)
			{
				_pendingScoreRequest.score = next.score;
			}
			else if (!nextIncrement && wasIncrement)
			{
				// we are switching to an increment, but we cannot do that, because we don't know what the score used to be
				// so we'll fake it
				var current = myStandings.GetOrThrow(
					() => new InvalidOperationException("Indeterminate behaviour for offline leaderboard mocking."))
						   .score;

				// we need to simulate the aggregate add so far...
				current += _pendingScoreRequest.score;

				// add the delta that would put the score at the desired value.
				_pendingScoreRequest.score += (next.score - current);

			}

			if (next.minScore.HasValue)
			{
				_pendingScoreRequest.score = next.score < next.minScore.Value ? next.minScore.Value : next.score;
			}
			if (next.maxScore.HasValue)
			{
				_pendingScoreRequest.score = next.score > next.maxScore.Value ? next.maxScore.Value : next.score;
			}
		}

		void PrepareStats(Dictionary<string, string> stats = null)
		{
			MapOfString statMap = null;
			_pendingScoreRequest.stats.Clear();
			if (stats != null)
			{
				statMap = new MapOfString();
				_pendingScoreRequest.stats.Set(statMap);

				foreach (var kvp in stats)
				{
					statMap[kvp.Key] = kvp.Value;
				}
			}
		}

		async Promise SetPendingScore()
		{
			try
			{
				if (!HasPendingScoreRequest) return;

				var info = await GetLocalAssignment(true);
				await _api.ObjectPutEntry(info.leaderboardId, _pendingScoreRequest);
				await UpdateLocalScore();
				_pendingScoreRequest = null;
			}
			finally
			{
				collection.Save();
			}
		}

		void IPlayerLeaderboardFriend.SetSize(long size)
		{
			this.boardSize = size;
		}

		void IPlayerLeaderboardFriend.SetCurrentScore(RankEntry entry) => SetCurrentScore(entry);
		void SetCurrentScore(RankEntry entry)
		{
			if (!myStandings.HasValue)
			{
				myStandings.Set(new PlayerLeaderboardEntry(entry));
			}
			else
			{
				myStandings.Value.rank = entry.rank;
				myStandings.Value.score = entry.score;
				myStandings.Value.stats = entry.stats;
			}

			myStandings.Value.Update();
			collection.Save();
		}

		void IPlayerLeaderboardFriend.SetCurrentFriendScore(RankEntry entry)
		{
			if (!myStandingsAmongFriends.HasValue)
			{
				myStandingsAmongFriends.Set(new PlayerLeaderboardEntry(entry));
			}
			else
			{
				myStandingsAmongFriends.Value.rank = entry.rank;
				myStandingsAmongFriends.Value.score = entry.score;
				myStandingsAmongFriends.Value.stats = entry.stats;
			}
			myStandingsAmongFriends.Value.Update();
		}

		void IPlayerLeaderboardFriend.Save()
		{
			collection.Save();
		}

	}

	[Serializable]
	public class OptionalPlayerLeaderboardEntry
		: Optional<PlayerLeaderboardEntry>, IObservable, ISerializationCallbackReceiver
	{
		public event Action OnUpdated;
		public event Action<OptionalPlayerLeaderboardEntry> OnDataUpdated;
		private long lastBroadcastChecksum;
		private bool lastHadValue;

		public OptionalPlayerLeaderboardEntry(PlayerLeaderboardEntry entry)
		{
			Value = entry;
			HasValue = true;
			lastHadValue = true;
			lastBroadcastChecksum = entry.GetBroadcastChecksum();
		}
		public OptionalPlayerLeaderboardEntry()
		{

		}

		public void Update()
		{
			if (!HasValue && lastHadValue)
			{
				Invoke();
			}
			else if (HasValue && !lastHadValue)
			{
				Invoke();
			}

			var checksum = Value.GetBroadcastChecksum();
			if (HasValue && lastHadValue && lastBroadcastChecksum != checksum)
			{
				Invoke();
				lastBroadcastChecksum = checksum;
			}

			lastHadValue = HasValue;

		}

		void Invoke()
		{
			OnUpdated?.Invoke();
			OnDataUpdated?.Invoke(this);
		}

		public override void Set(PlayerLeaderboardEntry value)
		{
			base.Set(value);
			value.OnUpdated -= Update;
			value.OnUpdated += Update;
			Update();
		}

		public override void SetValue(object value)
		{
			throw new InvalidOperationException("Cannot use SetValue in an observable option type");
		}

		public override void Clear()
		{
			base.Clear();
			Update();
		}

		public void OnBeforeSerialize()
		{

		}

		public void OnAfterDeserialize()
		{
			if (!HasValue) return;
			Value.OnUpdated -= Update;
			Value.OnUpdated += Update;
		}
	}

	[Serializable]
	public class PlayerLeaderboardEntry
		: DefaultObservable
#if UNITY_EDITOR
		, ISerializationCallbackReceiver
#endif
	{
		#region autogenerated equality memebrs
		protected bool Equals(PlayerLeaderboardEntry other)
		{
			return playerId == other.playerId && rank == other.rank && score.Equals(other.score) && Equals(stats, other.stats);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((PlayerLeaderboardEntry)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = playerId.GetHashCode();
				hashCode = (hashCode * 397) ^ rank.GetHashCode();
				hashCode = (hashCode * 397) ^ score.GetHashCode();
				// note: stats are not part of the check
				return hashCode;
			}
		}

		#endregion
#if UNITY_EDITOR
		[SerializeField]
		[HideInInspector]
		private string displayName;
#endif

		/// <summary>
		/// The player Id for the owner of this entry
		/// </summary>
		public long playerId;

		/// <summary>
		/// The rank of the entry. Lower ranks represent higher scores.
		/// Entries are 1 indexed, so don't expect to see an entry of 0
		/// unless the entry instance has only been instantiated without network initialization.
		/// </summary>
		public long rank;

		/// <summary>
		/// The score of the entry. Higher scores represent better ranks.
		/// </summary>
		public double score;

		/// <summary>
		/// The optional stats associated with an entry
		/// </summary>
		public OptionalArrayOfRankEntryStat stats = new OptionalArrayOfRankEntryStat();

		public PlayerLeaderboardEntry()
		{

		}

		public PlayerLeaderboardEntry(RankEntry entry)
		{
			playerId = entry.gt;
			score = entry.score;
			rank = entry.rank;
			stats = entry.stats;
		}

		public void Update()
		{
			TriggerUpdate();
		}

		public override int GetBroadcastChecksum()
		{
			var hash = 0;
			if (stats?.HasValue ?? false)
			{
				foreach (var kvp in stats.Value)
				{
					hash = CombineHashCodes(hash, kvp.name?.GetHashCode() ?? 1);
					hash = CombineHashCodes(hash, kvp.value?.GetHashCode() ?? 1);
				}
			}

			hash = CombineHashCodes(hash, score.GetHashCode());
			hash = CombineHashCodes(hash, rank.GetHashCode());
			hash = CombineHashCodes(hash, playerId.GetHashCode());
			return hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int CombineHashCodes(int h1, int h2)
		{
			return (((h1 << 5) + h1) ^ h2);
		}


#if UNITY_EDITOR
		public void OnBeforeSerialize()
		{
			displayName = $"{rank} - {playerId}";
		}
		public void OnAfterDeserialize()
		{
		}
#endif
	}

	[Serializable]
	public class LeaderboardDictionary : SerializableDictionaryStringToSomething<PlayerLeaderboard>
	{

	}

}
