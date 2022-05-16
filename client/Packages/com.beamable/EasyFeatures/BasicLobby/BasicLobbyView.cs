using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class BasicLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public enum View
		{
			MainMenu,
			CreateLobby,
			JoinLobby,
		}
		
		public interface ILobbyDeps : IBeamableViewDeps
		{
			View ActiveView { get; set; }
		}
		
		[Header("View Configuration")]
		[SerializeField] private int _enrichOrder;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Debug.Log("Enrich from BasicLobbyView");
		}
	}
}
