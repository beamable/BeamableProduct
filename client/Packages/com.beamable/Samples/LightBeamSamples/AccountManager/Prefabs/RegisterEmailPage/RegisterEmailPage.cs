
using Beamable.Common;
using Beamable.Runtime.LightBeam;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterEmailPage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public TMP_InputField emailInput;
	public TMP_InputField passwordInput;
	public TMP_Text promptText;
	
	public Button checkEmailButton;
	public Button submitButton;
	public Button cancelButton;
	
	public Promise OnInstantiated(LightContext ctx)
	{
		submitButton.gameObject.SetActive(false);
		checkEmailButton.gameObject.SetActive(true);
		passwordInput.gameObject.SetActive(false);

		promptText.text = "Enter email";
		
		
		cancelButton.HandleClicked(async () =>
		{
			await ctx.GotoPage<HomePage>();
		});
		
		checkEmailButton.HandleClicked("checking...", async () =>
		{
			await CheckEmail(ctx, emailInput.text);
		});
		
		submitButton.HandleClicked("registering...", async () =>
		{
			await RegisterEmail(ctx, emailInput.text, passwordInput.text);
		});

		return Promise.Success;
	}

	async Promise CheckEmail(LightContext ctx, string email)
	{
		var isEmailAvailable = await ctx.BeamContext.Accounts.IsEmailAvailable(email);

		if (isEmailAvailable)
		{
			promptText.text = "Enter password";
			passwordInput.gameObject.SetActive(true);
			emailInput.interactable = false;
			submitButton.gameObject.SetActive(true);
			checkEmailButton.gameObject.SetActive(false);
		}
		else
		{
			promptText.text = "Email already taken, enter a different email";
			emailInput.text = "";
		}
	}

	async Promise RegisterEmail(LightContext ctx, string email, string password)
	{
		var operation = await ctx.BeamContext.Accounts.AddEmail(email, password);
		if (operation.isSuccess)
		{
			await ctx.GotoPage<HomePage>();
		}
		else
		{
			promptText.text = $"Error: {operation.error}\n{operation.innerException?.Message}";
		}
	}
}

