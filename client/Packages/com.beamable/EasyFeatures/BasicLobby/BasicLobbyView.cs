using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class BasicLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface ILobbyDeps : IBeamableViewDeps
		{
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{

		}
	}
}
