using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class SocialFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		public IEnumerable<BeamableViewGroup> ManagedViewGroups { get; }

		[SerializeField]
		private bool _runOnEnable = true;
		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}

		public BeamableViewGroup ViewGroup;

		protected BeamContext Context;
		
		public void OnEnable()
		{
			ViewGroup.RebuildManagedViews();

			if (!_runOnEnable)
			{
				return;
			}
			
			Run();
		}

		public async void Run()
		{
			await ViewGroup.RebuildPlayerContexts(ViewGroup.AllPlayerCodes);

			Context = ViewGroup.AllPlayerContexts[0];
			await Context.OnReady;
		}
	}
}
