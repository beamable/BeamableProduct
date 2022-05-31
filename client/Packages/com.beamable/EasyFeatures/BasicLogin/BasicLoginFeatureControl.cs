using Beamable.Common;
using Beamable.Common.Dependencies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLogin
{
	[BeamContextSystem]
	public class BasicLoginFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		[RegisterBeamableDependencies()]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<BasicLoginSystem,
				BasicLoginView.ILoginDeps>();
		}


		[Header("Feature Control"), SerializeField]
		private bool _runOnEnable = true;

		public BeamableViewGroup LoginViewGroup;

		public IEnumerable<BeamableViewGroup> ManagedViewGroups  { get => new[] { LoginViewGroup }; set => LoginViewGroup = value.FirstOrDefault(); }
		public bool RunOnEnable => _runOnEnable;

		public virtual void OnEnable()
		{
			// Ask the view group to update it's managed views.
			LoginViewGroup.RebuildManagedViews();

			if (!RunOnEnable) return;
			Run();
		}

		public virtual async void Run()
		{
			await LoginViewGroup.RebuildPlayerContexts(LoginViewGroup.AllPlayerCodes);

			// TODO: Enter loading state.

			await RefreshView();
		}

		public virtual async Promise RefreshView()
		{
			var ctx = LoginViewGroup.AllPlayerContexts.GetSinglePlayerContext();
			var system = ctx.ServiceProvider.GetService<BasicLoginSystem>();
			await system.RefreshData();
			await LoginViewGroup.EnrichWithPlayerCodes();
		}
	}
}
