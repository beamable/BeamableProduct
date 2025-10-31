using Beamable;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Logic : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField playerId;
    public Button portalButton;
    
    // the beam context is visible in the inspector.
    public BeamContext beamContext;
    
    async void Start()
    {
        beamContext = await BeamContext.Default.Instance;
        Debug.Log($"Player Id: {beamContext.PlayerId}");

        playerId.text = beamContext.PlayerId.ToString();
        portalButton.onClick.AddListener(() => LightBeam.OpenPortalRealm(beamContext, $"/players/{beamContext.PlayerId}"));
    }
    
}
