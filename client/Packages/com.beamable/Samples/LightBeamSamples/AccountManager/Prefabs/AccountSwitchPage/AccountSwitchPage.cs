
using Beamable;
using Beamable.AccountManagement;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Player;
using Beamable.Runtime.LightBeam;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AccountSwitchPage : MonoBehaviour, ILightComponent<PlayerAccount>
{
	[Header("Scene references")]
	public AccountDisplayBehaviour accountDisplay;
	public Button cancelButton;
	public Button switchButton;
	public Button deleteButton;
	
	public Promise OnInstantiated(BeamContext ctx, PlayerAccount account)
	{
		accountDisplay.OnInstantiated(ctx, account);
		
		cancelButton.HandleClicked(async () =>
		{
			await ctx.GotoPage<AccountManagementExample>();
		});
		
		switchButton.HandleClicked("switching...", async () =>
		{
			await account.SwitchToAccount();
			await ctx.GotoPage<AccountManagementExample>();
		});
		
		deleteButton.HandleClicked("deleting...", async () =>
		{
			await account.Remove();
			await ctx.GotoPage<AccountManagementExample>();
		});
		
		return Promise.Success;
	}
}
