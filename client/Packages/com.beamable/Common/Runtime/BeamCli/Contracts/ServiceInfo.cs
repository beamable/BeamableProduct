using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[Serializable]
	public class ServiceInfo
	{
		public string name;
		public string dockerBuildPath;
		public string dockerfilePath;
		
	}
}
