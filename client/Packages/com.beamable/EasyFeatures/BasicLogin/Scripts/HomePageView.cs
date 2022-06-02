using UnityEngine;
using UnityEngine.Events;

namespace Beamable.EasyFeatures.BasicLogin.Scripts
{
	public class HomePageView : MonoBehaviour, ISyncBeamableView
	{
		[Header("View Configuration")]
		public int EnrichOrder;

		public OtherAccountView OtherAccountViewPrefab;
		public RectTransform OtherAccountContainer;


		public UnityEvent OnOtherAccountSelected;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var viewDeps = currentContext.ServiceProvider.GetService<ILoginDeps>();

			// need to spawn up the other accounts based on the actual data available in the view deps.
			for (var i = 0; i < viewDeps.AvailableUsers.Count; i++)
			{
				var otherView = Instantiate(OtherAccountViewPrefab, OtherAccountContainer);
				otherView.OtherUserIndex = i;
				otherView.OnAccountSelected = OnOtherAccountSelected;
				otherView.EnrichWithContext(managedPlayers);
			}
		}
	}
}
