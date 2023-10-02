
using Beamable.Common;
using Beamable.Runtime.LightBeams;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class ForgotPasswordModel
{
	public string email;
}

public class ForgotPasswordPage : MonoBehaviour, ILightComponent<ForgotPasswordModel>
{
	[Header("Scene References")]
	public TMP_Text promptText;
	public TMP_InputField codeInput;
	public TMP_InputField passwordInput;
	
	public Button cancelButton;
	public Button submitButton;

	public async Promise OnInstantiated(LightBeam beam, ForgotPasswordModel model)
	{
		await beam.BeamContext.Accounts.ResetPassword(model.email);
		
		promptText.text = $"Enter the code sent to {model.email}, and enter a new password.";
		
		cancelButton.HandleClicked(async () =>
		{
			await beam.GotoPage<RecoverEmailPage, RecoverEmailPageModel>(new RecoverEmailPageModel
			{
				email = model.email
			});
		});
		
		submitButton.HandleClicked("checking...", async () =>
		{
			var newPassword = passwordInput.text;
			var code = codeInput.text;
			var res = await beam.BeamContext.Accounts.ConfirmPassword(code, newPassword);

			if (res.isSuccess)
			{
				await beam.GotoPage<RecoverEmailPage, RecoverEmailPageModel>(new RecoverEmailPageModel
				{
					email = model.email,
					password = newPassword
				});
			}
			else
			{
				codeInput.text = "";
				passwordInput.text = "";
				promptText.text = "There was a problem. Try again";
			}
			
		});

	}
}
