using Beamable.Editor.Content.Helpers;
using Beamable.Editor.Content.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Content.Extensions
{
	public static class ContentSorterExtensions
	{
		public static List<ContentItemDescriptor> Sort(this List<ContentItemDescriptor> contentItems,
			ContentSortType contentSortType = ContentSortType.IdAZ)
		{
			if (contentItems == null)
				return null;

			if (contentItems.Count == 0)
				return contentItems;

			var sortedContentItems = new List<ContentItemDescriptor>();

			switch (contentSortType)
			{
				case ContentSortType.IdAZ:
					sortedContentItems = contentItems.OrderBy(x => x.Name).ToList();
					break;
				case ContentSortType.IdZA:
					sortedContentItems = contentItems.OrderByDescending(x => x.Name).ToList();
					break;
				case ContentSortType.TypeAZ:
					sortedContentItems = contentItems.OrderBy(x => x.ContentType.TypeName).ToList();
					break;
				case ContentSortType.TypeZA:
					sortedContentItems = contentItems.OrderByDescending(x => x.ContentType.TypeName).ToList();
					break;
				// case ContentSortType.PublishedDate:
				// 	Debug.LogWarning("NOT IMPLEMENTED");
				// 	break;
				case ContentSortType.RecentlyUpdated:
					sortedContentItems = contentItems.OrderByDescending(x => x.LastChanged).ToList();
					break;
				case ContentSortType.Status:
					sortedContentItems = contentItems.OrderBy(x => x.Status).ToList();
					break;
			}

			contentItems.Clear();
			contentItems.AddRange(sortedContentItems);
			return contentItems;
		}
	}
}
