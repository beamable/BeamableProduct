// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

using System;

namespace Beamable.Server
{
	public static class IServiceProviderExtensions
	{
		public static T GetService<T>(this IServiceProvider provider)
		{
			return (T)provider.GetService(typeof(T));
		}
	}
}
