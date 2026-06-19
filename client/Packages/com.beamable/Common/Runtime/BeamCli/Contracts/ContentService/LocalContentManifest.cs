// this file was copied from nuget package Beamable.Common@7.2.0
// https://www.nuget.org/packages/Beamable.Common/7.2.0

﻿using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public class LocalContentManifest
	{
		public string OwnerCid;
		public string OwnerPid;
		public string ManifestId;

		public LocalContentManifestEntry[] Entries;
	}
}
