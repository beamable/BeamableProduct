using UnityEngine;

[CreateAssetMenu(menuName = "Beamable/Examples/Account Example Config")]
public class AccountExampleConfig : ScriptableObject
{
	public HomePage homePage;
	public AccountSwitchPage switchPage;
	public RegisterEmailPage registerPage;
	public RecoverEmailPage recoverPage;
	public ForgotPasswordPage forgotPasswordPage;

	public AvatarDisplayBehaviour avatarDisplayTemplate;
	public AccountDisplayBehaviour accountDisplayTemplate;
	public AccountDetailsBehaviour accountDetailsTemplate;
}
