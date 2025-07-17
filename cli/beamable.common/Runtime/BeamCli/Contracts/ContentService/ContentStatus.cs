using System;

namespace Beamable.Common.BeamCli.Contracts
{

	[CliContractType, Flags]
	public enum ContentStatus
	{
		Invalid = 0,
		Created = 1 << 0,
		Deleted = 1 << 1,
		Modified = 1 << 2,
		UpToDate = 1 << 3,
	}
}
