using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class MainLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IMainLobbyViewDeps : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}
		
		[Header("View Configuration")]
		[SerializeField] private int _enrichOrder;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Debug.Log("Enrich from MainLobbyView");
			BeamContext ctx = managedPlayers.GetSinglePlayerContext();
			
			var system = ctx.ServiceProvider.GetService<IMainLobbyViewDeps>();
			
			gameObject.SetActive(system.IsVisible);
		}
	}
}
