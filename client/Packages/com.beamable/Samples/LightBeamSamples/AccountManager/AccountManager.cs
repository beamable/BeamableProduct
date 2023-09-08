
using Beamable.Avatars;
using Beamable.Player;
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
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent(config.registerPage);
			builder.AddLightComponent<ForgotPasswordPage, ForgotPasswordModel>(config.forgotPasswordPage);
			builder.AddLightComponent<RecoverEmailPage, RecoverEmailPageModel>(config.recoverPage);
			builder.AddLightComponent<AccountSwitchPage, PlayerAccount>(config.switchPage);
		
			builder.AddLightComponent<AvatarDisplayBehaviour, AccountAvatar>(config.avatarDisplayTemplate);
			builder.AddLightComponent<AccountDisplayBehaviour, PlayerAccount>(config.accountDisplayTemplate);
			builder.AddLightComponent<AccountDetailsBehaviour, PlayerAccount>(config.accountDetailsTemplate);
		});

		await ctx.BeamContext.Accounts.Refresh().ShowLoading(ctx);

		// LightBeamUtilExtensions.Hints["pageType"] = nameof(RecoverEmailPage);
		// LightBeamUtilExtensions.Hints["d_email"] = "\"dingus\"";
		await ctx.Scope.Start<AccountManagementExample>();
	}
}
