// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

namespace Beamable.Common.Dependencies
{
	public interface IDependencyNameProvider
	{
		string DependencyProviderName { get; }
	}

	public interface IDependencyScopeNameProvider
	{
		string DependencyScopeName { get; }
	}
}
