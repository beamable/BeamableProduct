using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable;
using Beamable.Common;
using Beamable.UI.Buss;
using Beamable.UI.Sdf;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerLeaderboardView : LeaderboardView
{
    public BeamableViewGroup OwnerGroup;
    public Button[] Buttons;

    public Color[] AvatarColors;

    public override BeamableViewGroup.PlayerCountMode SupportedMode => BeamableViewGroup.PlayerCountMode.MultiplayerUI;

    public override Promise EnrichWithContext(BeamContext currentContext)
    {
        throw new NotSupportedException();
    }

    public override async Promise EnrichWithContext(List<BeamContext> managedPlayers, int mainPlayerIndex)
    {
        var promises = new List<Promise>
        {
            base.EnrichWithContext(managedPlayers[mainPlayerIndex])
        };

        var secondaryPlayers = managedPlayers.Except(new[] { managedPlayers[mainPlayerIndex] }).ToList();
        for (var i = 0; i < secondaryPlayers.Count; i++)
        {
            var currentSecondaryPlayer = secondaryPlayers[i];
            var leaderboardDeps = currentSecondaryPlayer.ServiceProvider.GetService<ILeaderboardDeps>();


            var testMode = FeatureConfig.TestMode;
            var leaderboardRef = FeatureConfig.LeaderboardRef;
            var entriesAmount = FeatureConfig.EntriesAmount;

            leaderboardDeps.TestMode = testMode;
            promises.Add(leaderboardDeps.UpdateState(leaderboardRef, 0, entriesAmount));
        }

        for (var i = 0; i < promises.Count; i++) await promises[i];

        for (var i = 0; i < secondaryPlayers.Count; i++)
        {
            var currentSecondaryPlayer = secondaryPlayers[i];
            var leaderboardDeps = currentSecondaryPlayer.ServiceProvider.GetService<ILeaderboardDeps>();

            var playerIdx = managedPlayers.IndexOf(currentSecondaryPlayer);
            var userAvatar = leaderboardDeps.Avatars[leaderboardDeps.CurrentUserIndexInLeaderboard];
            var sdfImage = Buttons[i].GetComponentsInChildren<SdfImageBussElement>().FirstOrDefault(a => a.name == "Avatar");
            if (sdfImage != null)
            {
                var property = new SpriteBussProperty(userAvatar);
                var color = new SingleColorBussProperty(AvatarColors[playerIdx]);
                sdfImage.InlineStyle.TryAddProperty(BussStyle.BackgroundImage.Key, property, out var _);
                if (!sdfImage.InlineStyle.TryAddProperty(BussStyle.BackgroundColor.Key, color, out _))
                    sdfImage.InlineStyle.GetPropertyProvider(BussStyle.BackgroundColor.Key).SetProperty(color);

                sdfImage.RecalculateStyle();
            }


            Buttons[i].onClick.AddListener(() => { AvatarClicked(managedPlayers.IndexOf(currentSecondaryPlayer)); });
        }

        void AvatarClicked(int playerIndex)
        {
            OwnerGroup.MainPlayerIdx = playerIndex;
            OwnerGroup.Enrich();
        }
    }
}
