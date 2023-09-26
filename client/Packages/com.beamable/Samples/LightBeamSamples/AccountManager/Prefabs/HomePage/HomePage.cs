using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using UnityEngine;
using UnityEngine.UI;

public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform playerAccountContainer;
	public Transform playerDetailsContainer;
	public AccountDisplayBehaviour currentAccountDisplay;

	public Button registerEmailButton;
	public Button findAccountButton;
	public Button createNewAccountButton;
	
	public async Promise OnInstantiated(LightBeam ctx)
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
			await ctx.GotoPage<HomePage>();
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
			var details = await ctx.Instantiate<AccountDetailsBehaviour, PlayerAccount>(playerDetailsContainer, currentAccount);
			details.cancelButton.HandleClicked(() =>
			{
				playerDetailsContainer.Clear();
			});
		});
		
		// create all the prefab instances for the other accounts
		foreach (var account in ctx.BeamContext.Accounts)
		{
			if (account.GamerTag == currentAccount.GamerTag) continue; // skip current account
			var component = await ctx.Instantiate<AccountDisplayBehaviour, PlayerAccount>(playerAccountContainer, account);
			component.changeAccountButton.HandleClicked(async () =>
			{
				await ctx.GotoPage<AccountSwitchPage, PlayerAccount>(account);
			});
		}
	}
}
