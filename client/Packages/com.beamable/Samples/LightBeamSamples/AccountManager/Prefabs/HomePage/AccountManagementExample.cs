using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using UnityEngine;
using UnityEngine.UI;

public class AccountManagementExample : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform playerAccountContainer;
	public Transform playerDetailsContainer;
	public AccountDisplayBehaviour currentAccountDisplay;

	public Button registerEmailButton;
	public Button findAccountButton;
	public Button createNewAccountButton;

	public AccountDisplayBehaviour widget;
	
	
	public async Promise OnInstantiated(LightContext ctx)
	{
		
		// clear old data
		playerAccountContainer.Clear();
		playerDetailsContainer.Clear();
		
		await ctx.BeamContext.Accounts.Refresh();
		var currentAccount = ctx.BeamContext.Accounts.Current;
		
		// set up the register button
		registerEmailButton.interactable = !currentAccount.HasEmail;
		registerEmailButton.HandleClicked(async () =>
		{
			await ctx.GotoPage<RegisterEmailPage>();
		});
		
		createNewAccountButton.HandleClicked(async () =>
		{
			var newAccount = await ctx.BeamContext.Accounts.CreateNewAccount();
			await newAccount.SwitchToAccount();
			await ctx.GotoPage<AccountManagementExample>();
		});
		
		// set up the find account button
		findAccountButton.HandleClicked(async () =>
		{
			await ctx.GotoPage<RecoverEmailPage, RecoverEmailPageModel>(null);
		});
		
		// set up the account detail page
		await currentAccountDisplay.OnInstantiated(ctx, currentAccount);
		currentAccountDisplay.changeAccountButton.HandleClicked(async () =>
		{
			var details = await ctx.NewLightComponent<AccountDetailsBehaviour, PlayerAccount>(playerDetailsContainer, currentAccount);
			details.cancelButton.HandleClicked(() =>
			{
				playerDetailsContainer.Clear();
			});
		});
		
		// create all the prefab instances for the other accounts
		// foreach (var account in ctx.BeamContext.Accounts)
		// {
		// 	if (account.GamerTag == currentAccount.GamerTag) continue; // skip current account
		// 	var component = await ctx.NewLightComponent<AccountDisplayBehaviour, PlayerAccount>(playerAccountContainer, account);
		// 	component.changeAccountButton.HandleClicked(async () =>
		// 	{
		// 		await ctx.GotoPage<AccountSwitchPage, PlayerAccount>(account);
		// 	});
		// }
		
		foreach (var account in ctx.BeamContext.Accounts)
		{
			if (account.GamerTag == currentAccount.GamerTag) continue; // skip current account

			var component = Object.Instantiate(widget, playerAccountContainer);
			// TODO: handle deeplinking somehow?
			await component.OnInstantiated(ctx, account);
			
			// component = BeamUtil.Instantiate(widget, container)
			// component.OnInstantiated(account) 
			
			
			// var component = await ctx.NewLightComponent<AccountDisplayBehaviour, PlayerAccount>(playerAccountContainer, account);
			component.changeAccountButton.HandleClicked(async () =>
			{
				await ctx.GotoPage<AccountSwitchPage, PlayerAccount>(account);
			});
		}
	}
}
