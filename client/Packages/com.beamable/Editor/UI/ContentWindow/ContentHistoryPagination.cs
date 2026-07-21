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

		public static ContentHistoryVisibleRange GetVisibleRange(int itemCount, float scrollPosition, float viewportHeight, float rowHeight)
		{
			if (itemCount <= 0 || viewportHeight <= 0 || rowHeight <= 0)
			{
				return new ContentHistoryVisibleRange(0, 0);
			}

			var firstIndex = Math.Clamp((int)Math.Floor(Math.Max(0, scrollPosition) / rowHeight), 0, itemCount);
			var visibleRowCount = (int)Math.Ceiling(viewportHeight / rowHeight) + 1;
			var lastExclusive = Math.Min(itemCount, firstIndex + visibleRowCount);
			return new ContentHistoryVisibleRange(firstIndex, lastExclusive);
		}
	}

	public struct ContentHistoryVisibleRange
	{
		public readonly int FirstIndex;
		public readonly int LastExclusive;

		public ContentHistoryVisibleRange(int firstIndex, int lastExclusive)
		{
			FirstIndex = firstIndex;
			LastExclusive = lastExclusive;
		}
	}
}
