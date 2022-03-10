using System;
using Beamable;
using Beamable.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// We can also ship views that have no default implementation for their dependencies. This is due to the fact that users can easily implement these over our own systems.
/// This file exemplifies that.
/// </summary>
public class UserProfileView : MonoBehaviour, ISyncBeamableView
{
    public interface IRankDeps
    {
        long GetUserRank();
    }
    
    public interface IHeaderDeps
    {
        string GetUserAlias();
        Sprite GetUserAvatar();
    }
    
    public BeamableViewGroup.PlayerCountMode SupportedMode => BeamableViewGroup.PlayerCountMode.SinglePlayerUI;
    public int GetEnrichOrder() => int.MaxValue;
    

    public TextMeshProUGUI Alias;
    public Image Avatar;

    public TextMeshProUGUI Rank;

    public virtual void EnrichWithContext(BeamContext currentContext)
    {
        var headerDeps = currentContext.ServiceProvider.GetService<IHeaderDeps>();
        var rankDeps = currentContext.ServiceProvider.GetService<IRankDeps>();

        Alias.text = headerDeps.GetUserAlias();
        Avatar.sprite = headerDeps.GetUserAvatar();

        Rank.text = $"Rank {rankDeps.GetUserRank()}";
    }

    public void EnrichWithContext(BeamContext currentContext, int playerIndex)
    {
        throw new NotSupportedException($"This UI does not know how to render multiple players. You can know that by looking at {SupportedMode}.");
    }
}

