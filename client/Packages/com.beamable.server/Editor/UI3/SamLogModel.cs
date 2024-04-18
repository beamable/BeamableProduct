using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.Microservice.UI3
{
	[Serializable]
	public class SamLogModel
	{
		// public List<
	}

	[Serializable]
	public class SamServiceLogs
	{
		public string beamoId;
		public List<BeamTailLogMessageForClient> messages;
	}
}
