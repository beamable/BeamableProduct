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
		[SerializeField] private int _enrichOrder;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			var dependencies = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(dependencies.IsVisible);

			if (!dependencies.IsVisible)
			{
				return;
			}
		}
	}
}
