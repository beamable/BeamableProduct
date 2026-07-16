using Beamable.Editor.UI.ContentWindow;
using Beamable.Editor.ContentService;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Content;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentHistoryPaginationTests
	{
		[TestCase(1, 10)]
		[TestCase(10, 10)]
		[TestCase(50, 50)]
		[TestCase(51, 50)]
		public void ClampPageSize_StaysWithinSupportedRange(int requestedPageSize, int expectedPageSize)
		{
			Assert.That(ContentHistoryPagination.ClampPageSize(requestedPageSize), Is.EqualTo(expectedPageSize));
		}

		[TestCase(0, 10, 0)]
		[TestCase(1, 10, 1)]
		[TestCase(10, 10, 1)]
		[TestCase(11, 10, 2)]
		[TestCase(127, 10, 13)]
		public void GetPageCount_ReturnsExpectedPageCount(int entryCount, int pageSize, int expectedPageCount)
		{
			Assert.That(ContentHistoryPagination.GetPageCount(entryCount, pageSize), Is.EqualTo(expectedPageCount));
		}

		[TestCase(-1, 0, 0)]
		[TestCase(0, 3, 0)]
		[TestCase(4, 3, 2)]
		public void ClampPageIndex_KeepsSelectionInsideAvailablePages(int requestedPageIndex, int pageCount, int expectedPageIndex)
		{
			Assert.That(ContentHistoryPagination.ClampPageIndex(requestedPageIndex, pageCount), Is.EqualTo(expectedPageIndex));
		}

		[Test]
		public void ContentConfiguration_HistoryEntriesPerPage_DefaultsToTen()
		{
			var configuration = ScriptableObject.CreateInstance<ContentConfiguration>();

			Assert.That(configuration.HistoryEntriesPerPage, Is.EqualTo(10));
		}

		[Test]
		public void ContentHistoryEntryCache_MergesNewestEntriesAndRemovesDeletedEntries()
		{
			var cache = new ContentHistoryEntryCache();
			cache.Apply(new[]
			{
				new BeamContentHistoryEntry { ManifestUid = "older", CreatedDate = 1 },
				new BeamContentHistoryEntry { ManifestUid = "newer", CreatedDate = 2 }
			});

			cache.Apply(new[] { new BeamContentHistoryEntry { ManifestUid = "older", CreatedDate = 3 } }, new[] { "newer" });

			Assert.That(cache.Entries, Has.Count.EqualTo(1));
			Assert.That(cache.Entries[0].ManifestUid, Is.EqualTo("older"));
			Assert.That(cache.Entries[0].CreatedDate, Is.EqualTo(3));
		}

		[Test]
		public void ContentHistoryEntryCache_ReusesSortedSnapshotUntilEntriesChange()
		{
			var cache = new ContentHistoryEntryCache();
			cache.Apply(new[] { new BeamContentHistoryEntry { ManifestUid = "first", CreatedDate = 1 } });

			var initialSnapshot = cache.Entries;
			Assert.That(cache.Entries, Is.SameAs(initialSnapshot));

			cache.Apply(new[] { new BeamContentHistoryEntry { ManifestUid = "second", CreatedDate = 2 } });

			var updatedSnapshot = cache.Entries;
			Assert.That(updatedSnapshot, Is.Not.SameAs(initialSnapshot));
			Assert.That(updatedSnapshot.Select(entry => entry.ManifestUid), Is.EqualTo(new[] { "second", "first" }));

			cache.Apply(null, new[] { "second" });
			var removedSnapshot = cache.Entries;
			Assert.That(removedSnapshot, Is.Not.SameAs(updatedSnapshot));
			Assert.That(removedSnapshot.Select(entry => entry.ManifestUid), Is.EqualTo(new[] { "first" }));

			cache.Clear();
			Assert.That(cache.Entries, Is.Empty);
		}

		[TestCase(0, 0f, 100f, 20f, 0, 0)]
		[TestCase(10, 0f, 40f, 20f, 0, 3)]
		[TestCase(10, 40f, 40f, 20f, 2, 5)]
		[TestCase(10, 180f, 40f, 20f, 9, 10)]
		[TestCase(2, 0f, 200f, 20f, 0, 2)]
		public void GetVisibleRange_ReturnsOnlyRowsIntersectingTheViewport(int itemCount, float scrollPosition, float viewportHeight,
			float rowHeight, int expectedFirstIndex, int expectedLastExclusive)
		{
			var range = ContentHistoryPagination.GetVisibleRange(itemCount, scrollPosition, viewportHeight, rowHeight);

			Assert.That(range.FirstIndex, Is.EqualTo(expectedFirstIndex));
			Assert.That(range.LastExclusive, Is.EqualTo(expectedLastExclusive));
		}

		[Test]
		public void ContentHistoryOperationException_FormatsFullCopyableDiagnostic()
		{
			var error = new ContentHistoryOperationException(
				"Load publish changes",
				"Unable to load changes for this publish. Check your connection and try again.",
				"manifest-123",
				new InvalidOperationException("Network connection was lost."));

			Assert.That(error.Diagnostic, Does.Contain("Load publish changes"));
			Assert.That(error.Diagnostic, Does.Contain("manifest-123"));
			Assert.That(error.Diagnostic, Does.Contain("InvalidOperationException"));
			Assert.That(error.Diagnostic, Does.Contain("Network connection was lost."));
		}

		[Test]
		public void ContentHistoryStreamParser_ParsesCamelCaseEntryFields()
		{
			var entries = string.Join(",", Enumerable.Range(1, 21).Select(index =>
				$"{{\"manifestUid\":\"manifest-{index}\",\"createdDate\":{index},\"publishedBy\":\"user-{index}\",\"publishedByName\":\"User {index}\",\"affectedContentIds\":[\"currency.coins\"]}}"));
			var streamJson = $"{{\"ts\":1,\"type\":\"stream\",\"data\":{{\"EventType\":0,\"EntriesPage\":{{\"Entries\":[{entries}]}},\"EntriesToRemove\":[]}}}}";

			var parsedEntries = ContentHistoryStreamParser.ParseEntries(streamJson);

			Assert.That(parsedEntries.Count, Is.EqualTo(21));
			Assert.That(parsedEntries.All(entry => !string.IsNullOrEmpty(entry.ManifestUid)), Is.True);
			Assert.That(parsedEntries[0].ManifestUid, Is.EqualTo("manifest-1"));
			Assert.That(parsedEntries[20].AffectedContentIds, Is.EqualTo(new[] { "currency.coins" }));
		}

		[Test]
		public void ContentHistoryStreamParser_ParsesCamelCaseChangeEntries()
		{
			const string streamJson = "{\"ts\":1,\"type\":\"stream\",\"data\":{\"ContentEntries\":[{\"fullId\":\"currency.New_currency_1\",\"typeName\":\"currency\",\"changeStatus\":2,\"jsonFilePath\":\"deleted.json\"},{\"fullId\":\"currency.coins\",\"typeName\":\"currency\",\"changeStatus\":4,\"jsonFilePath\":\"modified.json\"}]}}";

			var parsedChanges = ContentHistoryStreamParser.ParseChanges(streamJson);

			Assert.That(parsedChanges, Has.Length.EqualTo(2));
			Assert.That(parsedChanges[0].FullId, Is.EqualTo("currency.New_currency_1"));
			Assert.That(parsedChanges[0].ChangeStatus, Is.EqualTo(2));
			Assert.That(parsedChanges[1].TypeName, Is.EqualTo("currency"));
			Assert.That(parsedChanges[1].ChangeStatus, Is.EqualTo(4));
		}

		[Test]
		public void ContentHistoryStreamParser_TryParseChanges_RejectsTerminalReportsWithoutReplacingStreamEntries()
		{
			const string streamJson = "{\"ts\":1,\"type\":\"stream\",\"data\":{\"ContentEntries\":[{\"fullId\":\"stores.Store_Coin\",\"typeName\":\"stores\",\"changeStatus\":4,\"jsonFilePath\":\"content.json\"}]}}";
			const string terminalJson = "{\"ts\":2,\"type\":\"eof\",\"data\":{}}";

			Assert.That(ContentHistoryStreamParser.TryParseChanges(streamJson, out var streamEntries), Is.True);
			Assert.That(streamEntries, Has.Length.EqualTo(1));
			Assert.That(ContentHistoryStreamParser.TryParseChanges(terminalJson, out _), Is.False);
			Assert.That(streamEntries[0].FullId, Is.EqualTo("stores.Store_Coin"));
		}

		[Test]
		public void ContentHistoryStreamParser_TryParseChanges_RejectsStreamWithoutChangeEntries()
		{
			const string streamJson = "{\"ts\":1,\"type\":\"stream\",\"data\":{}}";

			Assert.That(ContentHistoryStreamParser.TryParseChanges(streamJson, out var entries), Is.False);
			Assert.That(entries, Is.Empty);
		}

		[Test]
		public void ContentWindowStatus_ContainsHistoryMode()
		{
			Assert.That(Enum.IsDefined(typeof(ContentWindowStatus), "History"), Is.True);
		}

		[Test]
		public void ContentHistoryTimeBuckets_GroupsRecentPublishesAndArchivesOlderMonths()
		{
			var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local);
			var buckets = ContentHistoryTimeBuckets.Create(new[]
			{
				HistoryEntry("today", now.AddHours(-1)),
				HistoryEntry("this-week", now.Date.AddDays(-1)),
				HistoryEntry("earlier-this-month", now.Date.AddDays(-8)),
				HistoryEntry("june", new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Local)),
				HistoryEntry("may", new DateTime(2026, 5, 31, 12, 0, 0, DateTimeKind.Local)),
				HistoryEntry("april", new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Local)),
				HistoryEntry("last-year", new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Local))
			}, now);

			Assert.That(buckets.Select(bucket => bucket.Label), Is.EqualTo(new[]
			{
				"Today", "This week", "Earlier this month", "June 2026", "May 2026", "2026", "2025"
			}));
			Assert.That(buckets.Take(3).All(bucket => bucket.IsExpandedByDefault), Is.True);
			Assert.That(buckets[3].IsExpandedByDefault, Is.False);
			Assert.That(buckets[5].Children.Select(bucket => bucket.Label), Is.EqualTo(new[] { "April 2026" }));
			Assert.That(buckets[6].Children.Select(bucket => bucket.Label), Is.EqualTo(new[] { "December 2025" }));
		}

		[Test]
		public void ContentHistoryTimeBuckets_ReturnsAncestorKeysForOlderPublish()
		{
			var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local);
			var entry = HistoryEntry("april", new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Local));
			var buckets = ContentHistoryTimeBuckets.Create(new[] { entry }, now);

			Assert.That(ContentHistoryTimeBuckets.GetAncestorKeys(buckets, entry.ManifestUid),
				Is.EqualTo(new[] { "year:2026", "month:2026-04" }));
		}

		[Test]
		public void ContentHistoryTimeBuckets_UsesMondayAsTheStartOfThisWeek()
		{
			var monday = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Local);
			var buckets = ContentHistoryTimeBuckets.Create(new[]
			{
				HistoryEntry("sunday", monday.AddDays(-1)),
				HistoryEntry("saturday", monday.AddDays(-2))
			}, monday);

			Assert.That(buckets.Select(bucket => bucket.Label), Is.EqualTo(new[] { "Earlier this month" }));
			Assert.That(buckets[0].EntryCount, Is.EqualTo(2));
		}

		[Test]
		public void ContentHistoryTimeBuckets_ExpandingMonthShowsEveryPublishInThatMonth()
		{
			var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local);
			var buckets = ContentHistoryTimeBuckets.Create(Enumerable.Range(1, 12)
				.Select(day => HistoryEntry($"may-{day}", new DateTime(2026, 5, day, 12, 0, 0, DateTimeKind.Local)))
				.ToArray(), now);
			var mayBucket = buckets.Single(bucket => bucket.Label == "May 2026");

			var rows = ContentHistoryTimeBuckets.BuildVisibleRows(buckets, new HashSet<string> { mayBucket.Key });

			Assert.That(rows.Count(row => row.Entry != null), Is.EqualTo(12));
		}

		[Test]
		public void ContentHistoryTimeBuckets_DefaultWeekCanBeCollapsed()
		{
			var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local);
			var buckets = ContentHistoryTimeBuckets.Create(new[]
			{
				HistoryEntry("today", now.AddHours(-1)),
				HistoryEntry("this-week", now.Date.AddDays(-1))
			}, now);
			var expandedKeys = ContentHistoryTimeBuckets.GetDefaultExpandedKeys(buckets).ToHashSet();

			expandedKeys.Remove("this-week");
			var rows = ContentHistoryTimeBuckets.BuildVisibleRows(buckets, expandedKeys);

			Assert.That(rows.Any(row => row.Entry?.ManifestUid == "today"), Is.True);
			Assert.That(rows.Any(row => row.Entry?.ManifestUid == "this-week"), Is.False);
		}

		[TestCase(false, false, ContentHistoryRowVisualState.Normal)]
		[TestCase(false, true, ContentHistoryRowVisualState.Hovered)]
		[TestCase(true, false, ContentHistoryRowVisualState.Selected)]
		[TestCase(true, true, ContentHistoryRowVisualState.Selected)]
		public void ContentHistoryRowInteraction_PrioritizesSelectionOverHover(bool isSelected, bool isHovered,
			ContentHistoryRowVisualState expectedState)
		{
			Assert.That(ContentHistoryRowInteraction.GetVisualState(isSelected, isHovered), Is.EqualTo(expectedState));
		}

		[TestCase(null, null, false)]
		[TestCase("entry:one", "entry:one", false)]
		[TestCase(null, "entry:one", true)]
		[TestCase("entry:one", "bucket:this-week", true)]
		[TestCase("entry:one", null, true)]
		public void ContentHistoryRowInteraction_RepaintsOnlyWhenTheHoveredRowChanges(string currentRowKey,
			string nextRowKey, bool shouldRepaint)
		{
			Assert.That(ContentHistoryRowInteraction.ShouldRepaint(currentRowKey, nextRowKey), Is.EqualTo(shouldRepaint));
		}

		private static BeamContentHistoryEntry HistoryEntry(string manifestUid, DateTime createdAt)
		{
			return new BeamContentHistoryEntry
			{
				ManifestUid = manifestUid,
				CreatedDate = new DateTimeOffset(createdAt).ToUnixTimeMilliseconds(),
				AffectedContentIds = Array.Empty<string>()
			};
		}
	}
}
