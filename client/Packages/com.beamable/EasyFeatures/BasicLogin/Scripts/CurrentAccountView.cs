using Beamable.AccountManagement;
using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLogin.Scripts
{
	public class CurrentAccountView : MonoBehaviour, ISyncBeamableView
	{

		[Header("View Configuration")]
		public int EnrichOrder;

		public TextMeshProUGUI AliasText;
		public TextMeshProUGUI SubText;
		public AccountAvatarBehaviour AccountAvatarBehaviour;

		public int GetEnrichOrder() => EnrichOrder;

		public virtual void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			var currentUser = viewDeps.CurrentUser;
			AliasText.text = currentUser.alias;
			SubText.text = currentUser.subtext;
			AccountAvatarBehaviour.Refresh(currentUser.avatarId);
		}
	}
}
