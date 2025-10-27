// this file was copied from nuget package Beamable.Common@6.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/6.1.0-PREVIEW.RC1

using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public enum ContentSnapshotType
	{
		Local = 0,
		Shared = 1
	}
}
