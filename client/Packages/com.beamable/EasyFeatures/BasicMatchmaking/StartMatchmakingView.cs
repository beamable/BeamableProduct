using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.Basicmatchmaking
{
	public class StartMatchmakingView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
			List<SimGameType> GameTypes { get; }
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		
		public Action<string> OnError;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			var dependencies = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(dependencies.IsVisible);
		}
	}
}
