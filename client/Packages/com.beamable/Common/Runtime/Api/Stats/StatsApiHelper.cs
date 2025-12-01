// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿namespace Beamable.Common.Api.Stats
{
	public static class StatsApiHelper
	{
		public static string GeneratePrefix(StatsDomainType domain, StatsAccessType access)
		{
			string domainValue = ParseDomainType(domain);

			string accessValue = ParseAccessValue(access);
			return $"{domainValue}.{accessValue}.player.";
		}
		
		public static  string GeneratePrefix(StatsDomainType domain, StatsAccessType access, long userId)
		{
			return $"{GeneratePrefix(domain, access)}{userId}";
		}

		public static string ParseDomainType(StatsDomainType domain)
		{
			return domain switch
			{
				StatsDomainType.Client => "client",
				StatsDomainType.Game => "game",
				_ => string.Empty
			};
		}
		
		public static string ParseAccessValue(StatsAccessType access)
		{
			return access switch
			{
				StatsAccessType.Private => "private",
				StatsAccessType.Public => "public",
				_ => string.Empty
			};
		}
	}
}
