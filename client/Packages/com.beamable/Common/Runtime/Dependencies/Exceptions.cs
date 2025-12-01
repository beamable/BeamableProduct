// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;

namespace Beamable.Common.Dependencies
{
	public class ServiceScopeDisposedException : Exception
	{
		private readonly string _src;
		private readonly Type _requestedType;
		private readonly IDependencyProvider _provider;

		public ServiceScopeDisposedException(string src, Type requestedType, IDependencyProvider provider)
		: base($"Provider scope has been destroyed and can no longer be accessed. method=[{src}] type=[{requestedType.Name}]")
		{
			_src = src;
			_requestedType = requestedType;
			_provider = provider;
		}
	}
}
