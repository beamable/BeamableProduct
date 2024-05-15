// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

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
