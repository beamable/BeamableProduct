
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccountDetailsBehaviour : MonoBehaviour, ILightComponent<PlayerAccount>
{
	[Header("Scene References")]
	public TMP_InputField aliasInput;

	public Image avatarImage;
	public Transform avatarContainer;

	public Button cancelButton;
	public Button saveButton;

	[Header("Runtime Data")]
	public AccountAvatar selectedAvatar;

	public async Promise OnInstantiated(LightBeam ctx, PlayerAccount model)
	{
		aliasInput.text = model.Alias;

		var avatarConfig = ctx.Scope.GetService<AvatarConfiguration>();
		selectedAvatar = avatarConfig.FindAvatar(model.Avatar);
		avatarImage.sprite = avatarConfig.GetAvatarSprite(model.Avatar);

		avatarContainer.Clear();
		foreach (var avatar in avatarConfig.Avatars)
		{
			var instance = await ctx.Instantiate<AvatarDisplayBehaviour, AccountAvatar>(avatarContainer, avatar);

			instance.mainButton.HandleClicked(() =>
		  {
			  selectedAvatar = avatar;
			  avatarImage.sprite = selectedAvatar.Sprite;
		  });
		}

		saveButton.HandleClicked("saving...", async () =>
		{
			await model.SetAlias(aliasInput.text);
			await model.SetAvatar(selectedAvatar?.Name);
			cancelButton.onClick.Invoke(); // simulate click.
		});

	}
}

