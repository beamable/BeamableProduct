
using Beamable.Runtime.LightBeam;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;
	
	[Header("Asset references")]
	public AccountExampleConfig config;
	
	async void Start()
	{
		var ctx = await this.InitLightBeams(root, loadingBlocker, builder =>
		{
			builder.RegisterAccountPages(config);
		});

		await ctx.Scope.GotoPage<AccountManagementExample>();
	}
}
