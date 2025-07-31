using Beamable;
using Beamable.Editor.BeamCli;
using UnityEngine.TestTools;

namespace Tests.Runtime
{
	public class SetupBeam : IPrebuildSetup
	{
		public void Setup()
		{
			BeamCliUtil.InitializeBeamCli();
		}
	}
}
