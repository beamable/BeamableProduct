using Beamable.Common.Dependencies;
using Beamable.Common.Leaderboards;
using System;

namespace Beamable.Player
{
	public interface IPlayerLeaderboardsFriend
	{
		void Save();
	}

	/// <summary>
	/// A <see cref="PlayerLeaderboards"/> contains a set of <see cref="PlayerLeaderboard"/>,
	/// accessible via the <see cref="PlayerLeaderboards.GetBoard(string)"/> method.
	/// <para>
	/// The resulting <see cref="PlayerLeaderboard"/> contain data about player performance. 
	/// </para>
	/// </summary>
	[Serializable]
	public class PlayerLeaderboards :
		IPlayerLeaderboardsFriend,
		IStorageHandler<PlayerLeaderboards>, IServiceStorable
	{
		private readonly IDependencyProvider _provider;
		private StorageHandle<PlayerLeaderboards> _handle;

		public LeaderboardDictionary boards = new LeaderboardDictionary();

		public PlayerLeaderboards(IDependencyProvider provider)
		{
			_provider = provider;
		}

		public void ReceiveStorageHandle(StorageHandle<PlayerLeaderboards> handle)
		{
			_handle = handle;
		}

		public void OnBeforeSaveState()
		{
		}

		public void OnAfterLoadState()
		{
			foreach (var kvp in boards)
			{
				var friend = kvp.Value as IPlayerLeaderboardFriend;
				friend.Hydrate(_provider);
				kvp.Value.collection = this;
			}
		}

		/// <summary>
		/// Get a <see cref="PlayerLeaderboard"/> for the given board content reference.
		/// </summary>
		/// <param name="leaderboardRef">
		/// A reference to a <see cref="LeaderboardContent"/>.
		/// </param>
		/// <returns>A <see cref="PlayerLeaderboard"/></returns>
		public PlayerLeaderboard GetBoard(LeaderboardRef leaderboardRef) => GetBoard(leaderboardRef?.Id);

		/// <summary>
		/// Get a <see cref="PlayerLeaderboard"/> for the given board id.
		/// 
		/// </summary>
		/// <param name="boardId">
		/// A leaderboard id usually starts with "leaderboards.", and is followed by the name of the board.
		/// Consider using the <see cref="GetBoard(Beamable.Common.Leaderboards.LeaderboardRef)"/> overload instead
		/// to get a <see cref="PlayerLeaderboard"/> with a content reference.
		/// </param>
		/// <returns>A <see cref="PlayerLeaderboard"/></returns>
		/// <exception cref="ArgumentException">The <paramref name="boardId"/> is invalid.</exception>
		public PlayerLeaderboard GetBoard(string boardId)
		{
			if (string.IsNullOrEmpty(boardId))
			{
				throw new ArgumentException("board id cannot be null", nameof(boardId));
			}

			if (!boards.TryGetValue(boardId, out var board))
			{
				boards[boardId] = board = DependencyBuilder.Instantiate<PlayerLeaderboard>(_provider);
				board.boardId = boardId;
				board.collection = this;
			}

			return board;
		}

		void IPlayerLeaderboardsFriend.Save()
		{
			_handle.Save();
		}
	}
}
