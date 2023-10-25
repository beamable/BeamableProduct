using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplayBehaviour : MonoBehaviour, ILightComponent<PlayerItem>
{
	[Header("Scene References")]
	public TextMeshProUGUI typeText;
	public TextMeshProUGUI idText;
	public Image icon;
	
	private PlayerItem _model;
	private LightBeam _ctx;

    public Promise OnInstantiated(LightBeam beam, PlayerItem model)
    {
	    _ctx = beam;
	    _model = model;

	    model.OnUpdated += Refresh;
	    Refresh();
	    
	    return Promise.Success;
    }

    private async void Refresh()
    {
	    typeText.text = $"Name: {_model.Content.name}";
	    idText.text = $"Id: {_model.Content.Id}";

	    icon.sprite = _model.Content.icon.Asset != null ? await _model.Content.icon.LoadSprite() : null;
    }
}
