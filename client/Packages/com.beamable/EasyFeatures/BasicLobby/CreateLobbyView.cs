using Beamable.Common.Content;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
			List<SimGameType> GameTypes { get; }
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
