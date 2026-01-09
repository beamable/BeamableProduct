using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public enum AutoSnapshotActionType
	{
		Publish,
		Sync,
		Restore
	}
}
