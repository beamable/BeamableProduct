// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿namespace Beamable.Common.Util
{
	public class DocsPageHelper
	{
		private const string CliBaseUrl = "https://help.beamable.com/CLI-{0}/{1}";
		private const string UnityBaseUrl = "https://help.beamable.com/Unity-{0}/{1}";
		
		public static string GetCliDocsPageUrl(string subpage, string version)
		{
			return GetDocsFullUrl(CliBaseUrl, subpage, version);
		}
		
		public static string GetUnityDocsPageUrl(string subpage, string version)
		{
			return GetDocsFullUrl(UnityBaseUrl, subpage, version);
		}
		

		private static string GetDocsFullUrl(string baseDocPage, string subpage, string version = "Latest")
		{
			return string.Format(baseDocPage, version, subpage);
		}

	}
}
