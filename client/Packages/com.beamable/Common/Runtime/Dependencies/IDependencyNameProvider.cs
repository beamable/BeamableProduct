// this file was copied from nuget package Beamable.Common@6.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/6.1.0-PREVIEW.RC1

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
