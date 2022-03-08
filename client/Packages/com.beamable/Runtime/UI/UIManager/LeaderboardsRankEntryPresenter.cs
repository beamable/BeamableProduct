using System.Collections.Generic;
using System.Globalization;
using Beamable.AccountManagement;
using Beamable.Avatars;
using Beamable.Common.Api.Leaderboards;
using Beamable.Stats;
using Beamable.UI.Buss;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Beamable.Common.Constants.Features.Leaderboards;

public class LeaderboardsRankEntryPresenter : MonoBehaviour
{
    public TextMeshProUGUI Rank;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Score;
    public Image Avatar;

    public SdfImageBussElement MainBussElement;
    public SdfImageBussElement RankBussElement;

    public NewLoadingIndicator LoadingIndicator;
    
    private long _currentPlayerRank;
    private StatObject _aliasStatObject;
    private StatObject _avatarStatObject;

    private RankEntry _renderingRankEntry;
    
    public void Enrich(RankEntry data, long currentPlayerRank)
    {
        _renderingRankEntry = data;
        _currentPlayerRank = currentPlayerRank;
        _aliasStatObject = AccountManagementConfiguration.Instance.DisplayNameStat;
        _avatarStatObject = AccountManagementConfiguration.Instance.AvatarStat;
    }
    
    public void Enrich(RankEntry data, long currentPlayerRank, StatObject aliasStatObject, StatObject avatarStatObject)
    {
        _renderingRankEntry = data;
        _currentPlayerRank = currentPlayerRank;
        _aliasStatObject = aliasStatObject;
        _avatarStatObject = avatarStatObject;
    }

    public void RebuildRankEntry()
    {
        Rank.text = _renderingRankEntry.rank.ToString();
        Name.text = _renderingRankEntry.GetStat(_aliasStatObject.StatKey) ?? _aliasStatObject.DefaultValue;
        Score.text = _renderingRankEntry.score.ToString(CultureInfo.InvariantCulture);

        string spriteId = _renderingRankEntry.GetStat(_avatarStatObject.StatKey);

        Avatar.sprite = !string.IsNullOrWhiteSpace(spriteId)
            ? GetAvatar(spriteId)
            : AvatarConfiguration.Instance.Default.Sprite;

        if (_currentPlayerRank == _renderingRankEntry.rank)
        {
            MainBussElement?.AddClass(BUSS_CLASS_CURRENT_PLAYER);
        }

        switch (_renderingRankEntry.rank)
        {
            case 1:
                RankBussElement?.AddClass(BUSS_CLASS_FIRST_PLACE);
                break;
            case 2:
                RankBussElement?.AddClass(BUSS_CLASS_SECOND_PLACE);
                break;
            case 3:
                RankBussElement?.AddClass(BUSS_CLASS_THIRD_PLACE);
                break;
        }
        
        LoadingIndicator.ToggleLoadingIndicator(false);
    }

    private Sprite GetAvatar(string id)
    {
        List<AccountAvatar> accountAvatars = AvatarConfiguration.Instance.Avatars;
        AccountAvatar accountAvatar = accountAvatars.Find(avatar => avatar.Name == id);
        return accountAvatar.Sprite;
    }

    public class PoolData : PoolableScrollView.IItem
    {
        public RankEntry RankEntry;

        public float Height
        {
            get;
            set;
        }
    }
}
