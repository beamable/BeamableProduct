// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

using static Beamable.Common.Constants.Features.Content;

namespace Beamable.Common.Content
{
	public enum ContentVisibility
	{
		Public, Private
	}

	public static class ContentVisibilityExtensions
	{
		public static ContentVisibility FromString(string str)
		{
			switch (str?.ToLower())
			{
				case PUBLIC: return ContentVisibility.Public;
				case PRIVATE: return ContentVisibility.Private;
				default: return ContentVisibility.Public;
			}
		}
	}
}
