
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
		var context = BeamContext.InParent(this);
		await context.CreateLightBeam(root, loading, (builder) => { });

	}
}
