// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

﻿namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Config
			{
				public const string BASE_PATH = Directories.BEAMABLE_PACKAGE_EDITOR + "/Config";
				public const string BASE_UI_PATH = Directories.BEAMABLE_PACKAGE_EDITOR_UI + "/Config";

				public const string ALIAS_KEY = "alias";
				public const string PID_KEY = "pid";
				public const string CID_KEY = "cid";
				public const string LAST_PID_KEY = "last-pid";

				public const string BEAMABLE_SETTINGS_RESOURCE_NAME = "Beamable.properties";
				public const string BEAMABLE_SETTINGS_RESOURCE_SPLITTER = "__BEAM__SETTING__SPLIT__";
			}
		}
	}
}
