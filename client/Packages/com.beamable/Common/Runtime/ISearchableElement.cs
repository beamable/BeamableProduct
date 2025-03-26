// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

namespace Beamable.Common.Runtime
{
	public interface ISearchableElement
	{
		string DisplayName { get; }

		int GetOrder();
		bool IsAvailable();
		bool IsToSkip(string filter);
		string GetClassNameToAdd();
	}
}
