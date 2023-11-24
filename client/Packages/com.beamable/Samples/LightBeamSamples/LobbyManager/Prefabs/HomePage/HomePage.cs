using Beamable;
using Beamable.Common;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Button createLobbyBtn;
	public Button findLobbyBtn;
	public Button enterLobbyBtn;

	public Promise OnInstantiated(LightBeam ctx)
	{
		createLobbyBtn.HandleClicked(() =>
		{
			ctx.GotoPage<CreateLobbyDisplayBehaviour>();
		});
		
		findLobbyBtn.HandleClicked(() =>
		{
			ctx.GotoPage<FindLobbyDisplayBehaviour>();
		});
		
		return Promise.Success;
	}
}
