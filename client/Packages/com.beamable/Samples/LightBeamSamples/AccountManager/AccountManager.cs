
using Beamable;
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
		// TODO: try/catch
        
		var beamContext = BeamContext.Default;
		await beamContext.OnReady.ShowLoading(null);
		var ctx = await beamContext.InitLightBeams(null ,loadingBlocker, builder =>
		{
			builder.AddLightComponent<AccountManagementExample>(config.homePage);
			builder.AddLightComponent<RegisterEmailPage>(config.registerPage);
			builder.AddLightComponent<ForgotPasswordPage, ForgotPasswordModel>(config.forgotPasswordPage);
			builder.AddLightComponent<RecoverEmailPage, RecoverEmailPageModel>(config.recoverPage);
			builder.AddLightComponent<AccountSwitchPage, PlayerAccount>(config.switchPage);
			
			builder.AddLightComponent<AvatarDisplayBehaviour, AccountAvatar>(config.avatarDisplayTemplate);
			builder.AddLightComponent<AccountDisplayBehaviour, PlayerAccount>(config.accountDisplayTemplate);
			builder.AddLightComponent<AccountDetailsBehaviour, PlayerAccount>(config.accountDetailsTemplate);
		});
		
        await ctx.GotoPage<AccountManagementExample>();
	}

	// var ctx = await this.InitLightBeams(root, loadingBlocker, builder =>
	// {
	// 	builder.AddSingleton(config);
	// 		
	// 	// builder.AddSingleton<>()
	// 	/*
	// 	 * how will I write UI?
	// 	 * when are these components available? When can I use them?
	//   * how can I animate component entry?
	// 	 */
	//
	// 	builder.AddLightComponent<AccountManagementExample>(config.homePage);
	// 	builder.AddLightComponent<RegisterEmailPage>(config.registerPage);
	// 	builder.AddLightComponent<ForgotPasswordPage, ForgotPasswordModel>(config.forgotPasswordPage);
	// 	builder.AddLightComponent<RecoverEmailPage, RecoverEmailPageModel>(config.recoverPage);
	// 	builder.AddLightComponent<AccountSwitchPage, PlayerAccount>(config.switchPage);
	// 	
	// 	builder.AddLightComponent<AvatarDisplayBehaviour, AccountAvatar>(config.avatarDisplayTemplate);
	// 	builder.AddLightComponent<AccountDisplayBehaviour, PlayerAccount>(config.accountDisplayTemplate);
	// 	builder.AddLightComponent<AccountDetailsBehaviour, PlayerAccount>(config.accountDetailsTemplate);
	// });
	//
	//
	// 	await ctx.GotoPage<AccountManagementExample>();
	// }
	//
	
	
	
	// await ctx.BeamContext.Accounts.Refresh().ShowLoading(ctx);
	// LightBeamUtilExtensions.Hints["pageType"] = nameof(RecoverEmailPage);
	// LightBeamUtilExtensions.Hints["d_email"] = "\"dingus\"";
	// await ctx.Scope.Start<AccountManagementExample>();
}
