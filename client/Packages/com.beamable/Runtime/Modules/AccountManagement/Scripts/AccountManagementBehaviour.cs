using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Coroutines;
using Beamable.UI.Scripts;
using System.Collections;
using UnityEngine;
using static Beamable.Common.Constants.URLs;

namespace Beamable.AccountManagement
{
	[HelpURL(Documentations.URL_DOC_LOGIN_FLOW)]
	public class AccountManagementBehaviour : MonoBehaviour
	{
		public MenuManagementBehaviour MenuManager;
		public AccountManagementConfiguration Configuration => AccountManagementConfiguration.Instance;

		// Start is called before the first frame update
		void Start()
		{
			BeamContext.Default.OnReady.Then(de => { 
				BeamContext.Default.OnShutdownComplete -= HandleShutdown;
				BeamContext.Default.OnShutdownComplete += HandleShutdown;
				Configuration.Overrides.HandleUserChange(MenuManager, BeamContext.Default.AuthorizedUser.Value); 
			});
		}

		private void HandleShutdown()
		{
			BeamContext.Default.OnReady.Then(
				_ => Start());
		}

		public void Toggle(bool accountDesiredState)
		{
			if (!accountDesiredState && MenuManager.IsOpen)
			{
				MenuManager.CloseAll();
			}
			else if (accountDesiredState && !MenuManager.IsOpen)
			{
				MenuManager.Show<AccountMainMenu>();
			}
		}

		public void ShowExistingAccount(User user)
		{
			var menu = MenuManager.Show<AccountExistsSelect>();
			menu.SetForUser(user);
		}

		public void ShowLoggedInAccount(User user)
		{
			if (!MenuManager) return;
			Configuration.Overrides.HandleUserChange(MenuManager, user);
		}

		public void ShowLoading(LoadingArg arg)
		{
			StartCoroutine(ShowLoadingNextFrame(arg));
		}

		IEnumerator ShowLoadingNextFrame(LoadingArg arg)
		{
			yield return Yielders.EndOfFrame;
			var menu = MenuManager.Show<LoadingPopup>();
			menu.Message = arg.Message;
			arg.Promise.Then(_ => menu.Hide());
		}

	}
}
