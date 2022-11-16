using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class CurrentAccountView : MonoBehaviour, ISyncBeamableView
	{
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			throw new System.NotImplementedException();
		}
	}
}
