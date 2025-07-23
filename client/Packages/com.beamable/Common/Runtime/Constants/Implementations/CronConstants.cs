// this file was copied from nuget package Beamable.Common@5.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/5.1.0-PREVIEW.RC1

﻿namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Cron
			{
				private const string CRON_LOCALIZATION_DATABASE_DIR =
					Directories.BEAMABLE_PACKAGE + "/Editor/UI/CronExpression/Resources";

				public const string CRON_LOCALIZATION_DATABASE_ASSET =
					CRON_LOCALIZATION_DATABASE_DIR + "/CronLocalizationDatabase.asset";
			}
		}
	}
}
