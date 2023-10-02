
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Runtime.LightBeams;
using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplayBehaviour : MonoBehaviour, ILightComponent<AccountAvatar>
{
	public Button mainButton;
	public Image avatarImage;
	private AvatarConfiguration _config;

	public Promise OnInstantiated(LightBeam beam, AccountAvatar model)
	{
		_config = beam.Scope.GetService<AvatarConfiguration>();
		avatarImage.sprite = (model?.Sprite) ?? _config.Avatars[0].Sprite;
		return Promise.Success;
	}
}

