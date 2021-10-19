using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable;
using Beamable.Common.Content;
using UnityEngine;

public class TestResolveInheritanceEdgeCase : MonoBehaviour
{
    public KingOfTheHillLink KingOfTheHillLink;
    public KingOfTheHillRef KingOfTheHillRef;
    //
    // public ContentBLink contentBLink;
    
    public SimGameTypeLink simGameTypeLink;
    public SimGameTypeRef simGameTypeRef;
    
    // Start is called before the first frame update
    void Start()
    {
       SetupBeamable();
    }
    
    private async void SetupBeamable()
    { 
        var beamableAPI = await API.Instance;
        beamableAPI.ContentService.Subscribe(clientManifest =>
        {
            Debug.Log($"#1. ContentService, all object count = {clientManifest.entries.Count}");
        });

        
        await KingOfTheHillLink.Resolve().Then(cntA => Debug.Log($"Direct Access to KingOfTheHillContentA Link{cntA}"));
        await KingOfTheHillRef.Resolve().Then(cntA => Debug.Log($"Direct Access to KingOfTheHillContentA Ref{cntA}"));

        // await contentBLink.Resolve().FlatMap(cntB => cntB.KingOfTheHillLink.Resolve()).Then(cntA => Debug.Log($"Indirect Access to Link via ContentB {cntA}"));
        // await contentBLink.Resolve().FlatMap(cntB => cntB.KingOfTheHillRef.Resolve()).Then(cntA => Debug.Log($"Indirect Access to Ref via ContentB {cntA}"));
        
        await simGameTypeLink.Resolve().Then(cnt => Debug.Log($"Direct Access to SimGameType Link{cnt}"));
        await simGameTypeRef.Resolve().Then(cnt => Debug.Log($"Direct Access to SimGameType Ref{cnt}"));
    }

}
