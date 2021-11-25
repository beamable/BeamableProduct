using Beamable.AccountManagement;
using System.Globalization;
using Beamable.Common.Api.Leaderboards;
using Beamable.Constats;
using Beamable.Modules.Generics;
using Beamable.Stats;
using Beamable.UI.Buss;
using TMPro;
using UnityEngine;

namespace Beamable.UI.Leaderboards
{
    public class LeaderboardsRankEntryPresenter : DataPresenter<RankEntry>
    {
#pragma warning disable CS0649
        [SerializeField] private TextMeshProUGUI _rank;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _score;
        
        [SerializeField] private SdfImageBussElement _mainBussElement;
        [SerializeField] private SdfImageBussElement _rankBussElement;

        [SerializeField] private bool _highlightCurrentUser;
#pragma warning restore CS0649

	    private long _currentPlayerRank;
	    private StatObject _aliasStatObject;

	    public override void Setup(RankEntry data, params object[] additionalParams)
	    {
		    _currentPlayerRank = (long) additionalParams[0];
		    _aliasStatObject = AccountManagementConfiguration.Instance.DisplayNameStat;
		    base.Setup(data, additionalParams);
	    }

	    protected override void Refresh()
        {
            _rank.text = Data.rank.ToString();
            _name.text = Data.GetStat(_aliasStatObject.StatKey) ?? _aliasStatObject.DefaultValue;
            _score.text = Data.score.ToString(CultureInfo.InvariantCulture);

            if (_currentPlayerRank == Data.rank && _highlightCurrentUser)
            {
	            _mainBussElement?.AddClass(LeaderboardsConstants.BUSS_CLASS_CURRENT_PLAYER);
            }

            switch (Data.rank)
            {
	            case 1:
		            _rankBussElement?.AddClass(LeaderboardsConstants.BUSS_CLASS_FIRST_PLACE);
		            break;
	            case 2:
		            _rankBussElement?.AddClass(LeaderboardsConstants.BUSS_CLASS_SECOND_PLACE);
		            break;
	            case 3:
		            _rankBussElement?.AddClass(LeaderboardsConstants.BUSS_CLASS_THIRD_PLACE);
		            break;
            }
        }
    }
}
