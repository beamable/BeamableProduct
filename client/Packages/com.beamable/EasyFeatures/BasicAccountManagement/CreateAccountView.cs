using Beamable.EasyFeatures.Components;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class CreateAccountView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public BeamInputField PasswordInput;

		protected IDependencies System;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;

			if (!IsVisible)
			{
				return;
			}
			
			PasswordInput.Setup(true);
		}
	}
}
