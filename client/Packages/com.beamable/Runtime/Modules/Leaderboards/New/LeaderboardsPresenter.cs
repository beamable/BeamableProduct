using Beamable.Common.Leaderboards;
using Beamable.Modules.Generics;
using UnityEngine;

namespace Beamable.UI.Leaderboards
{
	public class LeaderboardsPresenter : ModelPresenter<LeaderboardsModel>
	{
#pragma warning disable CS0649
		[SerializeField] private LeaderboardRef _leaderboardRef;
		[SerializeField] private int _entriesPerPage;
		[SerializeField] private GenericButton _previousPageButton;
		[SerializeField] private GenericButton _nextPageButton;
		[SerializeField] private GenericButton _topButton;
		[SerializeField] private LeaderboardsRankEntriesPresenter _rankEntries;
		[SerializeField] private LeaderboardsRankEntryPresenter _currentUserRankEntry;

		[Header("Debug")]
		[SerializeField] private bool _testMode;
#pragma warning restore CS0649

		protected override void Awake()
		{
			base.Awake();
			
			// TODO: add assertions for configuration
			if (_testMode)
			{
				Debug.LogWarning($"Use are using {name} in test mode");
			}
			
			Model.Initialize(_leaderboardRef, _entriesPerPage, _testMode);

			Model.OnScrollRefresh += OnScrollRefresh;

			_topButton.Setup(Model.ScrollToTopButtonClicked);
			_previousPageButton.Setup(Model.PreviousPageClicked);
			_nextPageButton.Setup(Model.NextPageClicked);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Model.OnScrollRefresh -= OnScrollRefresh;
		}

		protected override void RefreshRequested()
		{
			_rankEntries.ClearData();
		}

		protected override void Refresh()
		{
			_rankEntries.Setup(Model.CurrentRankEntries, Model.CurrentUserRankEntry.rank);
			_currentUserRankEntry.Setup(Model.CurrentUserRankEntry, Model.CurrentUserRankEntry.rank);

			_previousPageButton.interactable = Model.FirstEntryId > 0;
		}

		private void OnScrollRefresh()
		{
			_rankEntries.ScrollToTop();
		}
	}
}
