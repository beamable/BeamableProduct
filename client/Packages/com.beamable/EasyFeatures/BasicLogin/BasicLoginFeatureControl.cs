using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.BasicLogin.Scripts;
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
				ILoginDeps>();
		}


		[Header("Feature Control"), SerializeField]
		private bool _runOnEnable = true;

		public BeamableViewGroup LoginViewGroup;

		public HomePageView HomePageView;
		public SwitchPageView SwitchPageView;

		public MonoBehaviour[] MainPageViews => new MonoBehaviour[] {HomePageView, SwitchPageView};

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

			await RebuildView();
		}


		public virtual async Promise RebuildView()
		{
			var ctx = LoginViewGroup.AllPlayerContexts.GetSinglePlayerContext();
			var system = ctx.ServiceProvider.GetService<BasicLoginSystem>();
			await system.RefreshData();
			ResolveView();
		}


		public virtual void ResolveView()
		{
			// based on the state, enable the right view.
			var ctx = LoginViewGroup.AllPlayerContexts.GetSinglePlayerContext();
			var viewDeps = ctx.ServiceProvider.GetService<ILoginDeps>();

			if (viewDeps.AvailableSwitch.HasValue)
			{
				SelectView(SwitchPageView);
				return;
			}

			SelectView(HomePageView);
		}

		protected virtual void SelectView(MonoBehaviour view)
		{
			foreach (var page in MainPageViews)
			{
				if (view == page) continue;
				page.gameObject.SetActive(false);
			}
			view.gameObject.SetActive(true);
			LoginViewGroup.EnrichWithPlayerCodes();
		}
	}
}
