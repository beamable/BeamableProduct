using Beamable;
using Beamable.Server;
using Beamable.Server.Clients;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeSafeChecks : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
	    var ctx = BeamContext.Default;
	    await ctx.OnReady;
	    var client = ctx.Microservices().HotDoggo();
	    client.AttachIdentity<SolanaCloudIdentity>("x");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
