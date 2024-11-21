using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;

public class PlayerDisplayModel
{
	public PlayerSocial social;
	public long playerId;
}

public class PlayerDisplay : MonoBehaviour, ILightComponent<PlayerDisplayModel>
{
	public TextMeshProUGUI playerTitle;

	private PlayerDisplayModel _model;
	private LightBeam _ctx;

    public Promise OnInstantiated(LightBeam beam, PlayerDisplayModel model)
    {
	    _ctx = beam;
	    _model = model;
	    Init();

	    return Promise.Success;
    }

    private void Init()
    {
	    playerTitle.text = $"Player Id: {_model.playerId.ToString()}";
    }
}
