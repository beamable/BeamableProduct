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
#pragma warning restore CS0649

        protected override void Awake()
        {
            base.Awake();
            Model.Initialize(_leaderboardRef.Id, _entriesPerPage);
        }

        protected override void Initialized()
        {
                   
        }

        protected override void Refresh()
        {
            
        }
    }
}