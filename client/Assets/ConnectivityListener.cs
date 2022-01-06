using Beamable;
using Beamable.Api.Connectivity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConnectivityListener : MonoBehaviour
{
	public TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
	    BeamContext.Default.ServiceProvider.GetService<IConnectivityService>().OnConnectivityChanged += connected =>
	    {
		    text.text = connected
			    ? "Connected."
			    : "Disconnected!";
	    };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
