using Beamable.Avatars;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccountDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerAccount>
{
	[Header("Scene References")]
	public TMP_Text playerIdLabel;
	public TMP_Text aliasLabel;
	public TMP_Text emailLabel;

	public Image avatarImage;

	public Button changeAccountButton;
	public Button portalButton;
	
	private PlayerAccount _model;
	private LightBeam _ctx;

	public Promise OnInstantiated(LightBeam ctx, PlayerAccount model)
	{
		_ctx = ctx;
		_model = model;
		model.OnUpdated += Refresh;
		Refresh();
		
		return Promise.Success;
	}

	void Refresh()
	{
		playerIdLabel.text = _model.GamerTag.ToString();
		aliasLabel.text = _model.Alias ?? "Anonymous";
		emailLabel.text = _model.Email ?? "";
		avatarImage.sprite = _ctx.Scope.GetService<AvatarConfiguration>().GetAvatarSprite(_model.Avatar);
		
		portalButton.HandleClicked(() => _ctx.OpenPortalRealm($"/players/{_model.GamerTag}"));
	}

	private void OnDestroy()
	{
		if (_model == null) return;
		_model.OnUpdated -= Refresh;
	}

}

