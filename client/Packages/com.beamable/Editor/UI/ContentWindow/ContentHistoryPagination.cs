using System;

namespace Beamable.Editor.UI.ContentWindow
{
	public static class ContentHistoryPagination
	{
		public const int MinimumPageSize = 10;
		public const int MaximumPageSize = 50;

		public static int ClampPageSize(int pageSize)
		{
			return Math.Clamp(pageSize, MinimumPageSize, MaximumPageSize);
		}

		public static int GetPageCount(int entryCount, int pageSize)
		{
			if (entryCount <= 0)
			{
				return 0;
			}

			return (entryCount + ClampPageSize(pageSize) - 1) / ClampPageSize(pageSize);
		}

		public static int ClampPageIndex(int pageIndex, int pageCount)
		{
			return pageCount <= 0 ? 0 : Math.Clamp(pageIndex, 0, pageCount - 1);
		}
	}
}
