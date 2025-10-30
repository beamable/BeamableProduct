using Beamable;
using UnityEngine;

public class Logic : MonoBehaviour
{
    // the beam context is visible in the inspector.
    public BeamContext beamContext;
    async void Start()
    {
        beamContext = await BeamContext.Default.Instance;
        Debug.Log($"Player Id: {beamContext.PlayerId}");
    }
}
