// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

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
