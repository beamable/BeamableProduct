using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface ICreateLobbyDeps : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}
		
		[Header("View Configuration")]
		[SerializeField] private int _enrichOrder;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Debug.Log("Enrich from CreateLobbyView");
			
			BeamContext ctx = managedPlayers.GetSinglePlayerContext();
			
			var system = ctx.ServiceProvider.GetService<ICreateLobbyDeps>();
			
			gameObject.SetActive(system.IsVisible);
		}
	}
}
