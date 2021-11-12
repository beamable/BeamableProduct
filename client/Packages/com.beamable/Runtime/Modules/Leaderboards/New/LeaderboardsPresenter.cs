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
        [SerializeField] private LeaderboardsRankEntriesPresenter _rankEntries;
        [SerializeField] private LeaderboardsRankEntryPresenter _currentPlayerEntryPresenter;
#pragma warning restore CS0649
        
        protected override void Awake()
        {
            base.Awake();
            Model.Initialize(_leaderboardRef, _entriesPerPage);

            _previousPageButton.Setup(Model.PreviousPageClicked);
            _nextPageButton.Setup(Model.NextPageClicked);
        }

        protected override void RefreshRequested()
        {
            _rankEntries.ClearData();
        }

        protected override void Refresh()
        {
            _rankEntries.Setup(Model.CurrentRankEntries);
            _currentPlayerEntryPresenter.Setup(Model.CurrentUserRankEntry);

            _previousPageButton.interactable = Model.FirstEntryId > 0;
        }
    }
}