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
			return attribute.InformationalVersion;
		}
	}
}
