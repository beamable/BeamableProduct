
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class RecoverEmailPageModel
{
	public string email;
	public string password;
}

public class RecoverEmailPage : MonoBehaviour, ILightComponent<RecoverEmailPageModel>
{
	[Header("Scene References")]
	public TMP_Text promptText;

	public TMP_InputField emailInput;
	public TMP_InputField passwordInput;

	public Button cancelButton;
	public Button checkEmailButton;
	public Button loginButton;
	public Button switchButton;
	public Button forgotPasswordButton;

	public Transform accountPreviewContainer;

	[Header("Runtime data")]
	public PlayerRecoveryOperation recoveryOperation;

	public Promise OnInstantiated(LightBeam beam, RecoverEmailPageModel model)
	{
		promptText.text = "Login into email";

		passwordInput.text = model?.password;
		emailInput.text = model?.email;

		var needsEmail = string.IsNullOrEmpty(model?.email);

		this.EnableObjects(needsEmail, checkEmailButton);
		this.EnableObjects(!needsEmail, passwordInput, loginButton, forgotPasswordButton);
		this.DisableObjects(switchButton);

		cancelButton.HandleClicked(async () =>
		{
			await beam.GotoPage<HomePage>();
		});

		forgotPasswordButton.HandleClicked(async () =>
		{
			await beam.GotoPage<ForgotPasswordPage, ForgotPasswordModel>(new ForgotPasswordModel
			{
				email = emailInput.text
			});
		});

		checkEmailButton.HandleClicked("checking...", async () =>
		{
			await CheckForAccount(beam, emailInput.text);
		});

		loginButton.HandleClicked("logging in...", async () =>
		{
			await Login(beam, emailInput.text, passwordInput.text);
		});

		switchButton.HandleClicked("switching...", async () =>
		{
			await recoveryOperation.account.SwitchToAccount();
			await beam.Scope.GotoPage<HomePage>();
		});

		return Promise.Success;
	}

	async Promise CheckForAccount(LightBeam ctx, string email)
	{
		var unknownEmail = await ctx.BeamContext.Accounts.IsEmailAvailable(email);
		if (unknownEmail)
		{
			promptText.text = "Account does not exist. Try again.";
			emailInput.text = "";
		}
		else
		{
			promptText.text = "Enter password";
			emailInput.interactable = false;
			this.EnableObjects(passwordInput,
							   loginButton,
							   forgotPasswordButton);
			this.DisableObjects(checkEmailButton);
		}
	}

	async Promise Login(LightBeam ctx, string email, string password)
	{
		recoveryOperation = await ctx.BeamContext.Accounts.RecoverAccountWithEmail(email, password);

		if (recoveryOperation.isSuccess)
		{
			promptText.text = "Switch to account?";

			if (recoveryOperation.realmAlreadyHasGamerTag)
			{
				promptText.text = "Merge account?";
			}

			this.DisableObjects(emailInput,
								passwordInput,
								loginButton,
								forgotPasswordButton);

			accountPreviewContainer.Clear();
			await ctx.Instantiate<AccountDisplayBehaviour, PlayerAccount>(accountPreviewContainer, recoveryOperation.account);

			this.EnableObjects(switchButton);
		}
		else
		{
			passwordInput.text = "";
			promptText.text = "Invalid credentials. Try again.";
		}
	}
}

