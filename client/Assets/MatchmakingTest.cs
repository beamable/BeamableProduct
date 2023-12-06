using Beamable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchmakingTest : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
	    await BeamContext.Default.OnReady;
    }
}
