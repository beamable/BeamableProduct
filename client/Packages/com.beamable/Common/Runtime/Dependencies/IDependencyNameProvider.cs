// this file was copied from nuget package Beamable.Common@5.3.0
// https://www.nuget.org/packages/Beamable.Common/5.3.0

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
