
using Beamable;
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public RectTransform root;
	public CanvasGroup loading;
	
	private async void Start()
	{
		var beamContext = BeamContext.Default;
		var ctx = await beamContext.InitLightBeams(root, loading, builder =>
		{
			
		});
		
		
	}
}
