using Beamable.Common.Dependencies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class BasicLobbyFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		[Header("Feature Control"), SerializeField]
		private bool _runOnEnable = true;
		
		public BeamableViewGroup LobbyViewGroup;
		
		public IEnumerable<BeamableViewGroup> ManagedViewGroups { get => new[] {LobbyViewGroup}; set => LobbyViewGroup = value.FirstOrDefault(); }
		public bool RunOnEnable { get => _runOnEnable; set => _runOnEnable = value; }
		
		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<BasicLobbyPlayerSystem,
				BasicLobbyView.ILobbyDeps>();
		}
		
		
		public void OnEnable()
		{
			LobbyViewGroup.RebuildManagedViews();

			if (!RunOnEnable) return;
			Run();
		}

		public void Run()
		{
			
		}
	}
}
