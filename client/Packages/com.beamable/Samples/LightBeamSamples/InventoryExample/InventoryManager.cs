
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
	public RectTransform root;
	public CanvasGroup loading;
	
	private async void Start()
	{
		var ctx = await this.InitLightBeams(root, loading, builder =>
		{
			
		});
		
		
	}
}
