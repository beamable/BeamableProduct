using Beamable.Common.Dependencies;
using System;

namespace Beamable.Player
{
	[Serializable]
	public class PlayerLeaderboards :
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
		}

		public PlayerLeaderboard GetBoard(string boardId)
		{
			if (!boards.TryGetValue(boardId, out var board))
			{
				boards[boardId] = board = DependencyBuilder.Instantiate<PlayerLeaderboard>(_provider);
				board.boardId = boardId;
			}

			return board;
		}
	}
}
