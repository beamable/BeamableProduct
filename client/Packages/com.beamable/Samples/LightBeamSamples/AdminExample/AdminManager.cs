
using Beamable;
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;

public class AdminManager : MonoBehaviour
{
	[Header("Scene References")]
	public RectTransform root;
	public CanvasGroup loading;

	[Header("Asset references")]
	public ConsoleBehaviour consoleTemplate;
	
	private BeamContext _ctx;

	async void Start()
	{
		_ctx = BeamContext.Default;
		await _ctx.CreateLightBeam(root, loading, builder =>
		{
			builder.AddLightComponent(consoleTemplate);
		});
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tilde))
		{
			// _ctx.GotoPage<ConsoleBehaviour>();
		}
	}
}

