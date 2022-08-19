using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class MainLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}
		
		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			var dependencies = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(dependencies.IsVisible);
		}
	}
}
