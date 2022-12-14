using Beamable;
using Beamable.Player;
using Beamable.Server.Clients;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DudTest : MonoBehaviour
{
	public string code;
	public int index;

	private BeamContext _ctx;
	public PlayerAccounts accounts;

	// Start is called before the first frame update
    async void Start()
    {
	    _ctx = BeamContext.ForPlayer(code);
	    accounts = _ctx.Accounts;
	    await _ctx.OnReady;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("New user")]
    public async void NewUser()
    {
	    await accounts.CreateNewAccount();
    }
    
    [ContextMenu("SwitchToIndex")]
    public async void Switch()
    {
	    await accounts[index].SwitchToAccount();
    }
}
