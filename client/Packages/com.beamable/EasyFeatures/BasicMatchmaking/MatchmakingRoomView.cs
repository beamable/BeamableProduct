using Beamable.EasyFeatures;
using System;
using UnityEngine;

namespace EasyFeatures.BasicMatchmaking
{
	public class MatchmakingRoomView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		
		public Action<string> OnError;
		
		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			
		}
	}
}
