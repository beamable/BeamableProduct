// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿using System;

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
