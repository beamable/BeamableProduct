// this file was copied from nuget package Beamable.Server.Common@4.2.0-PREVIEW.RC5
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0-PREVIEW.RC5

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
