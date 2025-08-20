using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public struct ManifestAuthor
	{
		public string Email;
		public long AccountId;
	}
}
