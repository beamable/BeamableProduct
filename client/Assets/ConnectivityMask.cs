using Beamable;
using Beamable.Api.Connectivity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConnectivityMask : MonoBehaviour
{
	private IConnectivityService _connectivityService;

	public TextMeshProUGUI MaskText;

	public bool Global;
	// Start is called before the first frame update
    void Start()
    {
	    _connectivityService = BeamContext.InParent(this).ServiceProvider.GetService<IConnectivityService>();
    }

    // Update is called once per frame
    void Update()
    {
	    MaskText.text = _connectivityService.Disabled
		    ? "Force Disconnected"
		    : "Searching...";
    }

    public void ToggleMask()
    {
	    if (Global)
	    {
			_connectivityService.SetGlobalEnabled( !_connectivityService.GetGlobalEnabled());
	    }
	    else
	    {
		    _connectivityService.ForceDisabled = !_connectivityService.ForceDisabled;
	    }
    }

}
