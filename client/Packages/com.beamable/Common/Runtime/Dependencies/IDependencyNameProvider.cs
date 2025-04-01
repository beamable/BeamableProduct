// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC3

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
