using System.Globalization;
using Beamable.Common.Api.Leaderboards;
using Beamable.Modules.Generics;
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
#pragma warning restore CS0649

        protected override void Refresh()
        {
            _rank.text = Data.rank.ToString();
            _name.text = Data.GetStat("name");    // Temp solution
            _score.text = Data.score.ToString(CultureInfo.InvariantCulture);
        }
    }
}