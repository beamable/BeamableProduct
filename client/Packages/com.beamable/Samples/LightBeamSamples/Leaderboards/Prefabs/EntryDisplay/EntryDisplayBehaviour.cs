
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntryDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerLeaderboardEntry>
{
	[Header("Configuration")]
	public Color selfColor;
	
	[Header("Scene References")]
	public TMP_Text aliasLabel;
	public TMP_Text playerIdLabel;
	public TMP_Text rankLabel;
	public TMP_Text scoreLabel;
	public Image avatarImage;
	public Image backgroundImage;
	
	public async Promise OnInstantiated(LightBeam beam, PlayerLeaderboardEntry model)
	{
		if (model.playerId == beam.BeamContext.PlayerId)
		{
			backgroundImage.color = selfColor;
		}
		
		var statsService = beam.BeamContext.Api.StatsService;
		var stats = await statsService.GetStats("client", "public", "player", model.playerId);
		
		if (!stats.TryGetValue("alias", out var alias))
		{
			alias = "Anonymous";
		}

		if (!stats.TryGetValue("avatar", out var avatar))
		{
			avatar = "1";
		}

		aliasLabel.text = alias;
		rankLabel.text = model.rank.ToString("000");
		scoreLabel.text = model.score.ToString(".");
		playerIdLabel.text = model.playerId.ToString();
		
		var config = beam.Scope.GetService<AvatarConfiguration>();
		for (var i = 0 ; i < config.Avatars.Count; i ++)
		{
			if (config.Avatars[i].Name == avatar)
			{
				avatarImage.sprite = config.Avatars[i].Sprite;
				break;
			}

			if (config.Avatars.Count - 1 == i)
			{
				// if the end of list is here, just use the first avatar.
				avatarImage.sprite = config.Avatars[0].Sprite;
			}
		}
	
	}
	
}

