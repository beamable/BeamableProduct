using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PartyFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		private enum View
		{
			Party,
			Create,
			Join,
			Invite,
		}

		[SerializeField] private bool _runOnEnable = true;
		[SerializeField] private BeamableViewGroup _partyViewGroup;

		public IEnumerable<BeamableViewGroup> ManagedViewGroups
		{
			get;
		}

		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}

		public void OnEnable()
		{
			_partyViewGroup.RebuildManagedViews();

			if (!_runOnEnable)
			{
				return;
			}
			
			Run();
		}

		public void Run()
		{
			throw new System.NotImplementedException();
		}
	}
}
