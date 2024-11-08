// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

using Beamable.Common.Spew;
using System;
using System.Reflection;

namespace Beamable.Common.Util
{
	public static class BeamAssemblyVersionUtil
	{
		/// <inheritdoc cref="GetVersion"/>
		public static string GetVersion<T>() => GetVersion(typeof(T));

		/// <summary>
		/// Get the version number of the Beamable assembly that provides the given type.
		/// When Beamable assemblies are built, they use the <code> InformationalVersion </code> msbuild attribute
		/// to set the full semantic version version.
		/// <para>
		/// This function should not be used from the Unity SDK. This function is only applicable when using Nuget to ingest
		/// Beamable assemblies. 
		/// </para>
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static string GetVersion(Type t)
		{
			var attribute = t.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			
			// starting with net8, the attribute includes the commit hash, which we don't want.
			return RemoveSourceLinkFromVersion(attribute.InformationalVersion);
		}

		/// <summary>
		/// A utility method to take a semver string and remove the SourceLink part of the string, which
		/// is signaled by a '+' character.
		/// In Beamable's use cases, the SourceLink invalidates several versioning assumptions.
		/// https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/source-link 
		/// </summary>
		/// <param name="informationVersion"></param>
		/// <returns></returns>
		public static string RemoveSourceLinkFromVersion(string informationVersion)
		{
			var plusSymbolIndex = informationVersion.IndexOf('+');
			if (plusSymbolIndex >= 0)
			{
				informationVersion = informationVersion.Substring(0, plusSymbolIndex);
			}

			return informationVersion;
		}
	}
}
