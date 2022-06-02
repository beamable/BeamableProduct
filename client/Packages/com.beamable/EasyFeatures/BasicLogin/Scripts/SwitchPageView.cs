using UnityEngine;

namespace Beamable.EasyFeatures.BasicLogin.Scripts
{
	public class SwitchPageView : MonoBehaviour, ISyncBeamableView
	{
		[Header("View Configuration")]
		public int EnrichOrder;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{

		}
	}
}
