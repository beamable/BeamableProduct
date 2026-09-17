using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.UI.ContentWindow
{
	/// <summary>
	/// Builds the local-time, progressively grouped publish-history hierarchy used by the Content Window.
	/// Keeping the date rules here makes the IMGUI view a renderer instead of a second source of grouping logic.
	/// </summary>
	public static class ContentHistoryTimeBuckets
	{
		public static IReadOnlyList<ContentHistoryTimeBucket> Create(IReadOnlyList<BeamContentHistoryEntry> entries, DateTime localNow)
		{
			var todayEntries = new List<BeamContentHistoryEntry>();
			var thisWeekEntries = new List<BeamContentHistoryEntry>();
			var earlierThisMonthEntries = new List<BeamContentHistoryEntry>();
			var olderMonths = new Dictionary<DateTime, List<BeamContentHistoryEntry>>();
			var startOfToday = localNow.Date;
			var startOfThisWeek = startOfToday.AddDays(-((7 + (int)startOfToday.DayOfWeek - (int)DayOfWeek.Monday) % 7));

			foreach (var entry in entries ?? Array.Empty<BeamContentHistoryEntry>())
			{
				if (entry == null)
				{
					continue;
				}

				var localDate = DateTimeOffset.FromUnixTimeMilliseconds(entry.CreatedDate).LocalDateTime.Date;
				if (localDate == startOfToday)
				{
					todayEntries.Add(entry);
				}
				else if (localDate >= startOfThisWeek && localDate < startOfToday)
				{
					thisWeekEntries.Add(entry);
				}
				else if (localDate.Year == localNow.Year && localDate.Month == localNow.Month)
				{
					earlierThisMonthEntries.Add(entry);
				}
				else
				{
					var month = new DateTime(localDate.Year, localDate.Month, 1);
					if (!olderMonths.TryGetValue(month, out var monthEntries))
					{
						monthEntries = new List<BeamContentHistoryEntry>();
						olderMonths.Add(month, monthEntries);
					}
					monthEntries.Add(entry);
				}
			}

			var buckets = new List<ContentHistoryTimeBucket>();
			AddBucketIfNotEmpty(buckets, "today", "Today", true, todayEntries);
			AddBucketIfNotEmpty(buckets, "this-week", "This week", true, thisWeekEntries);
			AddBucketIfNotEmpty(buckets, "earlier-this-month", "Earlier this month", true, earlierThisMonthEntries);

			var previousMonth = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-1);
			var secondPreviousMonth = previousMonth.AddMonths(-1);
			AddMonthIfPresent(buckets, olderMonths, previousMonth);
			AddMonthIfPresent(buckets, olderMonths, secondPreviousMonth);

			foreach (var yearGroup in olderMonths
				.Where(pair => pair.Key != previousMonth && pair.Key != secondPreviousMonth)
				.GroupBy(pair => pair.Key.Year)
				.OrderByDescending(group => group.Key))
			{
				var yearBucket = new ContentHistoryTimeBucket($"year:{yearGroup.Key}", yearGroup.Key.ToString(), false);
				foreach (var monthGroup in yearGroup.OrderByDescending(pair => pair.Key))
				{
					yearBucket.AddChild(CreateMonthBucket(monthGroup.Key, monthGroup.Value));
				}
				buckets.Add(yearBucket);
			}

			return buckets;
		}

		public static IReadOnlyList<string> GetAncestorKeys(IReadOnlyList<ContentHistoryTimeBucket> buckets, string manifestUid)
		{
			if (string.IsNullOrEmpty(manifestUid))
			{
				return Array.Empty<string>();
			}

			foreach (var bucket in buckets ?? Array.Empty<ContentHistoryTimeBucket>())
			{
				var keys = new List<string>();
				if (TryGetAncestorKeys(bucket, manifestUid, keys))
				{
					return keys;
				}
			}

			return Array.Empty<string>();
		}

		public static IReadOnlyList<ContentHistoryTimeBucketRow> BuildVisibleRows(
			IReadOnlyList<ContentHistoryTimeBucket> buckets, ISet<string> expandedBucketKeys)
		{
			var rows = new List<ContentHistoryTimeBucketRow>();
			foreach (var bucket in buckets ?? Array.Empty<ContentHistoryTimeBucket>())
			{
				AddVisibleRows(rows, bucket, expandedBucketKeys);
			}
			return rows;
		}

		public static IReadOnlyCollection<string> GetDefaultExpandedKeys(IReadOnlyList<ContentHistoryTimeBucket> buckets)
		{
			var keys = new HashSet<string>();
			foreach (var bucket in buckets ?? Array.Empty<ContentHistoryTimeBucket>())
			{
				AddDefaultExpandedKeys(bucket, keys);
			}
			return keys;
		}

		public static string GetFirstManifestUid(ContentHistoryTimeBucket bucket)
		{
			if (bucket == null)
			{
				return string.Empty;
			}

			if (bucket.Entries.Count > 0)
			{
				return bucket.Entries[0].ManifestUid;
			}

			foreach (var child in bucket.Children)
			{
				var manifestUid = GetFirstManifestUid(child);
				if (!string.IsNullOrEmpty(manifestUid))
				{
					return manifestUid;
				}
			}

			return string.Empty;
		}

		private static void AddBucketIfNotEmpty(ICollection<ContentHistoryTimeBucket> buckets, string key, string label,
			bool isExpandedByDefault, IReadOnlyList<BeamContentHistoryEntry> entries)
		{
			if (entries.Count > 0)
			{
				buckets.Add(new ContentHistoryTimeBucket(key, label, isExpandedByDefault, entries));
			}
		}

		private static void AddMonthIfPresent(ICollection<ContentHistoryTimeBucket> buckets,
			IReadOnlyDictionary<DateTime, List<BeamContentHistoryEntry>> olderMonths, DateTime month)
		{
			if (olderMonths.TryGetValue(month, out var entries))
			{
				buckets.Add(CreateMonthBucket(month, entries));
			}
		}

		private static ContentHistoryTimeBucket CreateMonthBucket(DateTime month, IReadOnlyList<BeamContentHistoryEntry> entries)
		{
			return new ContentHistoryTimeBucket($"month:{month:yyyy-MM}", month.ToString("MMMM yyyy"), false, entries);
		}

		private static void AddVisibleRows(ICollection<ContentHistoryTimeBucketRow> rows, ContentHistoryTimeBucket bucket,
			ISet<string> expandedBucketKeys)
		{
			rows.Add(new ContentHistoryTimeBucketRow(bucket, null));
			if (expandedBucketKeys == null || !expandedBucketKeys.Contains(bucket.Key))
			{
				return;
			}

			foreach (var child in bucket.Children)
			{
				AddVisibleRows(rows, child, expandedBucketKeys);
			}

			foreach (var entry in bucket.Entries)
			{
				rows.Add(new ContentHistoryTimeBucketRow(null, entry));
			}
		}

		private static void AddDefaultExpandedKeys(ContentHistoryTimeBucket bucket, ISet<string> keys)
		{
			if (bucket.IsExpandedByDefault)
			{
				keys.Add(bucket.Key);
			}

			foreach (var child in bucket.Children)
			{
				AddDefaultExpandedKeys(child, keys);
			}
		}

		private static bool TryGetAncestorKeys(ContentHistoryTimeBucket bucket, string manifestUid, ICollection<string> keys)
		{
			keys.Add(bucket.Key);
			if (bucket.Entries.Any(entry => entry.ManifestUid == manifestUid))
			{
				return true;
			}

			foreach (var child in bucket.Children)
			{
				if (TryGetAncestorKeys(child, manifestUid, keys))
				{
					return true;
				}
			}

			keys.Remove(bucket.Key);
			return false;
		}
	}

	public readonly struct ContentHistoryTimeBucketRow
	{
		public ContentHistoryTimeBucket Bucket { get; }
		public BeamContentHistoryEntry Entry { get; }

		public ContentHistoryTimeBucketRow(ContentHistoryTimeBucket bucket, BeamContentHistoryEntry entry)
		{
			Bucket = bucket;
			Entry = entry;
		}
	}

	public class ContentHistoryTimeBucket
	{
		private readonly List<BeamContentHistoryEntry> _entries;
		private readonly List<ContentHistoryTimeBucket> _children = new();

		public string Key { get; }
		public string Label { get; }
		public bool IsExpandedByDefault { get; }
		public IReadOnlyList<BeamContentHistoryEntry> Entries => _entries;
		public IReadOnlyList<ContentHistoryTimeBucket> Children => _children;
		public int EntryCount => _entries.Count + _children.Sum(child => child.EntryCount);

		public ContentHistoryTimeBucket(string key, string label, bool isExpandedByDefault,
			IReadOnlyList<BeamContentHistoryEntry> entries = null)
		{
			Key = key;
			Label = label;
			IsExpandedByDefault = isExpandedByDefault;
			_entries = entries?.ToList() ?? new List<BeamContentHistoryEntry>();
		}

		public void AddChild(ContentHistoryTimeBucket child)
		{
			if (child != null)
			{
				_children.Add(child);
			}
		}
	}
}
