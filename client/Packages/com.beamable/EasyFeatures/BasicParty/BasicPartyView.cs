using Beamable.EasyFeatures;
using UnityEngine;

public class BasicPartyView : MonoBehaviour, ISyncBeamableView
{
	public interface IDependencies : IBeamableViewDeps
	{
		
	}
	
	[SerializeField] private int _enrichOrder;

	public int GetEnrichOrder() => _enrichOrder;

	public void EnrichWithContext(BeamContextGroup managedPlayers)
	{
		throw new System.NotImplementedException();
	}
}
