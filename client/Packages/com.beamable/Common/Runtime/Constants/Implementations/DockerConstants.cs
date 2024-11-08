// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

﻿using System;

namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Docker
			{
				[Obsolete]
				public const string CPU_LINUX_ARM_64 = "linux/arm64";
				public const string CPU_LINUX_AMD_64 = "linux/amd64";

				public static readonly string[] CPU_SUPPORTED = new string[] { CPU_LINUX_AMD_64 };
			}
		}
	}
}
