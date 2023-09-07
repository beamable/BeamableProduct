
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Runtime.LightBeam;
using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplayBehaviour : MonoBehaviour, ILightComponent<AccountAvatar>
{
	public Button mainButton;
	public Image avatarImage;
	private AvatarConfiguration _config;

	public Promise OnInstantiated(LightContext context, AccountAvatar model)
	{
		_config = context.Scope.GetService<AvatarConfiguration>();
		avatarImage.sprite = (model?.Sprite) ?? _config.Avatars[0].Sprite;
		return Promise.Success;
	}
}

