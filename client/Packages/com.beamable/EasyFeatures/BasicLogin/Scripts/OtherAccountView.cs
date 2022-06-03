using Beamable.AccountManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLogin.Scripts
{
	public class OtherAccountView : MonoBehaviour, ISyncBeamableView
	{

		[Header("View Configuration")]
		public int EnrichOrder;

		public UnityEvent OnAccountSelected;

		public TextMeshProUGUI AliasText;
		public TextMeshProUGUI SubText;
		public AccountAvatarBehaviour AccountAvatarBehaviour;
		public Button SwitchButton;

		public int OtherUserIndex;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			var currentUser = viewDeps.AvailableUsers[OtherUserIndex];
			AliasText.text = currentUser.alias;
			SubText.text = currentUser.subtext;
			AccountAvatarBehaviour.Refresh(currentUser.avatarId);

			SwitchButton.onClick.AddListener(() => OnButtonPressed(managedPlayers));
		}

		public virtual void OnButtonPressed(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			var currentUser = viewDeps.AvailableUsers[OtherUserIndex];
			viewDeps.OfferUserSelection(currentUser);

			OnAccountSelected?.Invoke();
		}

	}
}
