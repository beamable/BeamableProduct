using Beamable.Editor.UI.ContentWindow;
using Beamable.Editor.ContentService;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Content;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		public void ContentHistoryChangesSearch_FiltersByContentIdNameAndType()
		{
			var changes = new[]
			{
				new BeamContentHistoryChangelistEntry { FullId = "currency.gems", Name = "gems", TypeName = "economy" },
				new BeamContentHistoryChangelistEntry { FullId = "items.sword", Name = "Bronze Sword", TypeName = "items" },
				new BeamContentHistoryChangelistEntry { FullId = "stores.shop", Name = "Starter Shop", TypeName = "stores" }
			};

			Assert.That(ContentHistoryChangesSearch.Filter(changes, "currency"),
				Is.EqualTo(new[] { changes[0] }));
			Assert.That(ContentHistoryChangesSearch.Filter(changes, "ECONOMY"),
				Is.EqualTo(new[] { changes[0] }));
			Assert.That(ContentHistoryChangesSearch.Filter(changes, " starter "),
				Is.EqualTo(new[] { changes[2] }));
		}

		[Test]
		public void ContentHistoryChangesSearch_ReturnsEveryEntryForAnEmptySearch()
		{
			var changes = new[]
			{
				new BeamContentHistoryChangelistEntry { FullId = "currency.gems" },
				new BeamContentHistoryChangelistEntry { FullId = "items.sword" }
			};

			Assert.That(ContentHistoryChangesSearch.Filter(changes, "  "), Is.SameAs(changes));
		}

		[Test]
		public void ContentHistoryPublishSearch_UsesCaseInsensitiveExactAffectedContentIdMatches()
		{
			var firstMatch = new BeamContentHistoryEntry
			{
				ManifestUid = "manifest-first",
				AffectedContentIds = new[] { "currency.New_currency_0" }
			};
			var nonMatch = new BeamContentHistoryEntry
			{
				ManifestUid = "manifest-other",
				AffectedContentIds = new[] { "currency.coins" }
			};
			var secondMatch = new BeamContentHistoryEntry
			{
				ManifestUid = "manifest-second",
				AffectedContentIds = new[] { "items.sword", "currency.New_currency_0" }
			};

			var result = ContentHistoryPublishSearch.Filter(
				new[] { firstMatch, nonMatch, secondMatch }, "CURRENCY.NEW_CURRENCY_0");

			Assert.That(result.Entries, Is.EqualTo(new[] { firstMatch, secondMatch }));
			Assert.That(result.ShowNoExactIdHint, Is.False);
		}

		[Test]
		public void ContentHistoryPublishSearch_FallsBackToNormalSearchForAnUnmatchedContentIdPrefix()
		{
			var exactPrefixOnly = new BeamContentHistoryEntry
			{
				ManifestUid = "currency.New_currency_0-candidate",
				AffectedContentIds = new[] { "items.sword" }
			};
			var unrelated = new BeamContentHistoryEntry
			{
				ManifestUid = "manifest-other",
				AffectedContentIds = new[] { "currency.New_currency_0" }
			};

			var result = ContentHistoryPublishSearch.Filter(
				new[] { exactPrefixOnly, unrelated }, "currency.New_currency");

			Assert.That(result.Entries, Is.EqualTo(new[] { exactPrefixOnly }));
			Assert.That(result.ShowNoExactIdHint, Is.True);
		}

		[Test]
		public void ContentHistoryPublishSearch_PreservesNormalAuthorSearchWithoutAnAuditHint()
		{
			var matchingAuthor = new BeamContentHistoryEntry
			{
				ManifestUid = "manifest-first",
				PublishedBy = "dev@beamable.com",
				PublishedByName = "Moe Developer"
			};
			var unrelated = new BeamContentHistoryEntry { ManifestUid = "manifest-second" };

			var result = ContentHistoryPublishSearch.Filter(new[] { matchingAuthor, unrelated }, "moe");

			Assert.That(result.Entries, Is.EqualTo(new[] { matchingAuthor }));
			Assert.That(result.ShowNoExactIdHint, Is.False);
		}

		[Test]
		public void ContentHistoryPublishSearch_HandlesEmptySearchAndMissingAffectedContentIds()
		{
			var entryWithoutAffectedIds = new BeamContentHistoryEntry { ManifestUid = "manifest-first" };
			var entries = new[] { entryWithoutAffectedIds };

			var emptySearch = ContentHistoryPublishSearch.Filter(entries, "  ");
			var unmatchedId = ContentHistoryPublishSearch.Filter(entries, "currency.missing");

			Assert.That(emptySearch.Entries, Is.SameAs(entries));
			Assert.That(emptySearch.ShowNoExactIdHint, Is.False);
			Assert.That(unmatchedId.Entries, Is.Empty);
			Assert.That(unmatchedId.ShowNoExactIdHint, Is.True);
		}

		[Test]
		public void ContentHistoryFilterCache_DoesNotReuseRowsAfterTheEmptySearchCacheIsInvalidated()
		{
			Assert.That(ContentHistoryFilterCache.CanReuse(
				hasCachedRows: false,
				sourceMatches: true,
				cachedSearch: null,
				currentSearch: null), Is.False);
		}

		[Test]
		public void SearchBarClearInteraction_ClearsSearchAndNotifiesTheView()
		{
			var callbackCount = 0;
			var searchData = new SearchData
			{
				searchText = "manifest-198",
				onEndCheck = () => callbackCount++
			};

			SearchBarClearInteraction.Clear(searchData);

			Assert.That(searchData.searchText, Is.Null);
			Assert.That(callbackCount, Is.EqualTo(1));
		}

		[TestCase(EventType.MouseDown, true, true)]
		[TestCase(EventType.MouseUp, true, false)]
		[TestCase(EventType.MouseDown, false, false)]
		public void SearchBarClearInteraction_RecognizesOnlyClearGlyphMouseDown(EventType eventType, bool isPointerOverClearGlyph,
			bool expected)
		{
			Assert.That(SearchBarClearInteraction.IsClearClick(eventType, isPointerOverClearGlyph), Is.EqualTo(expected));
		}

		[Test]
		public void ContentHistoryManifestCopyLayout_PlacesCopyButtonBeforeManifestText()
		{
			var rowRect = new Rect(0, 10, 400, 24);
			const float lineHeight = 18f;
			var changesRect = new Rect(74, 13, 82, lineHeight);

			var layout = ContentHistoryManifestCopyLayout.Create(rowRect, changesRect, 112, 20,
				lineHeight);

			Assert.That(layout.CopyButtonRect.x, Is.EqualTo(changesRect.xMax));
			Assert.That(layout.ManifestRect.x, Is.EqualTo(layout.CopyButtonRect.xMax + 2));
			Assert.That(layout.AuthorRect.x, Is.EqualTo(rowRect.xMax - 112));
		}

		[Test]
		public void ContentWindow_DoesNotStoreStaticGuiContent()
		{
			var staticGuiContentFields = typeof(ContentWindow).GetFields(
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(field => field.FieldType == typeof(GUIContent));

			Assert.That(staticGuiContentFields, Is.Empty);
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
		public void ContentHistoryStreamParser_ParsesMetadataOnlyChangelistEntries()
		{
			const string streamJson = "{\"type\":\"stream\",\"data\":{\"Changelist\":{\"publishedAt\":1234,\"added\":{\"currency.gems\":{\"newVersion\":\"1\",\"newChecksum\":\"new\",\"newTags\":[\"currency\"]}},\"modified\":{\"items.sword\":{\"oldVersion\":\"1\",\"newVersion\":\"2\",\"oldTags\":[\"old\"],\"newTags\":[\"new\"]}},\"removed\":{\"stores.shop\":{\"oldVersion\":\"3\",\"oldChecksum\":\"old\",\"oldTags\":[\"store\"]}}}}}";

			var parsed = ContentHistoryStreamParser.TryParseChangelist(streamJson, out var entries);

			Assert.That(parsed, Is.True);
			Assert.That(entries, Has.Length.EqualTo(3));
			Assert.That(entries.Select(entry => entry.FullId), Is.EqualTo(new[] { "currency.gems", "items.sword", "stores.shop" }));
			Assert.That(entries.Select(entry => entry.TypeName), Is.EqualTo(new[] { "currency", "items", "stores" }));
			Assert.That(entries.Select(entry => entry.Name), Is.EqualTo(new[] { "gems", "sword", "shop" }));
			Assert.That(entries.Select(entry => entry.ChangeStatus), Is.EqualTo(new[]
			{
				(int)ContentStatus.Created,
				(int)ContentStatus.Modified,
				(int)ContentStatus.Deleted
			}));
			Assert.That(entries.All(entry => entry.ChangeDate == 1234), Is.True);
			Assert.That(entries.All(entry => string.IsNullOrEmpty(entry.JsonFilePath)), Is.True);
			Assert.That(entries[0].NewTags, Is.EqualTo(new[] { "currency" }));
			Assert.That(entries[1].OldTags, Is.EqualTo(new[] { "old" }));
			Assert.That(entries[1].NewTags, Is.EqualTo(new[] { "new" }));
			Assert.That(entries[2].OldTags, Is.EqualTo(new[] { "store" }));
		}

		[Test]
		public void ContentHistoryStreamParser_LeavesMalformedContentIdsListable()
		{
			const string streamJson = "{\"type\":\"stream\",\"data\":{\"Changelist\":{\"publishedAt\":1,\"added\":{\"malformed\":{\"newVersion\":\"1\"}}}}}";

			var parsed = ContentHistoryStreamParser.TryParseChangelist(streamJson, out var entries);

			Assert.That(parsed, Is.True);
			Assert.That(entries, Has.Length.EqualTo(1));
			Assert.That(entries[0].FullId, Is.EqualTo("malformed"));
			Assert.That(entries[0].TypeName, Is.Empty);
			Assert.That(entries[0].Name, Is.EqualTo("malformed"));
		}

		[TestCase("{\"type\":\"eof\",\"data\":{}}")]
		[TestCase("{\"type\":\"stream\",\"data\":{}}")]
		public void ContentHistoryStreamParser_RejectsInvalidChangelistStream(string streamJson)
		{
			var parsed = ContentHistoryStreamParser.TryParseChangelist(streamJson, out var entries);

			Assert.That(parsed, Is.False);
			Assert.That(entries, Is.Empty);
		}

		[Test]
		public void ContentHistoryRequestFactory_CreatesSingleContentSyncRequest()
		{
			var request = ContentHistoryRequestFactory.CreateContentSync("manifest-123", "currency.gems");

			Assert.That(request.manifestUid, Is.EqualTo("manifest-123"));
			Assert.That(request.contentIds, Is.EqualTo(new[] { "currency.gems" }));
		}

		[Test]
		public void ContentHistoryRequestSlot_CancelsReplacedRequestAndKeepsTheNewestCurrent()
		{
			var cancelled = new List<object>();
			var slot = new ContentHistoryRequestSlot<object>(cancelled.Add);
			var first = new object();
			var second = new object();

			slot.Replace(first);
			slot.Replace(second);
			slot.Release(first);

			Assert.That(cancelled, Is.EqualTo(new[] { first }));
			Assert.That(slot.IsCurrent(second), Is.True);

			slot.CancelActive();

			Assert.That(cancelled, Is.EqualTo(new[] { first, second }));
			Assert.That(slot.IsCurrent(second), Is.False);
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

		[Test]
		public void ContentHistoryInspectorPreview_MarksOnlyRegisteredPreviewAsReadOnly()
		{
			var preview = ScriptableObject.CreateInstance<ContentObject>();
			var ordinaryContent = ScriptableObject.CreateInstance<ContentObject>();
			try
			{
				ContentHistoryInspectorPreview.Register(preview);

				Assert.That(ContentHistoryInspectorPreview.IsReadOnly(preview), Is.True);
				Assert.That(ContentHistoryInspectorPreview.IsReadOnly(ordinaryContent), Is.False);

				ContentHistoryInspectorPreview.Release(preview);
				Assert.That(ContentHistoryInspectorPreview.IsReadOnly(preview), Is.False);
			}
			finally
			{
				ContentHistoryInspectorPreview.Release(preview);
				UnityEngine.Object.DestroyImmediate(preview);
				UnityEngine.Object.DestroyImmediate(ordinaryContent);
			}
		}

		[Test]
		public void ContentHistoryInspectorPreview_ClearsOnlyItsOwnSelection()
		{
			var preview = ScriptableObject.CreateInstance<ContentObject>();
			var unrelatedSelection = ScriptableObject.CreateInstance<ContentObject>();
			try
			{
				Assert.That(ContentHistoryInspectorPreview.ShouldClearSelection(preview, preview), Is.True);
				Assert.That(ContentHistoryInspectorPreview.ShouldClearSelection(preview, unrelatedSelection), Is.False);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(preview);
				UnityEngine.Object.DestroyImmediate(unrelatedSelection);
			}
		}

		[TestCase(4, 4, 9, 9, true)]
		[TestCase(5, 4, 9, 9, false)]
		[TestCase(4, 4, 10, 9, false)]
		public void ContentHistoryPreviewRequest_RequiresBothRequestVersionsToMatch(int currentSelectionVersion,
			int requestSelectionVersion, int currentPreviewVersion, int requestPreviewVersion, bool expectedCurrent)
		{
			Assert.That(ContentHistoryPreviewRequest.IsCurrent(currentSelectionVersion, requestSelectionVersion,
				currentPreviewVersion, requestPreviewVersion), Is.EqualTo(expectedCurrent));
		}

		[Test]
		public void ContentHistoryInspectorPreview_CreatesTypedPreviewFromCliJson()
		{
			const string json = "{\"id\":\"currency.gems\",\"version\":\"123\",\"properties\":{\"clientPermission\":{\"data\":{\"write_self\":false}},\"icon\":{\"data\":{\"referenceKey\":\"f819a6beb22c04c8d9f8222f930252b5\",\"subObjectName\":\"\"}},\"startingAmount\":{\"data\":0}}}";
			ContentObject preview = null;
			try
			{
				var created = ContentHistoryInspectorPreview.TryCreate(json, "currency.gems", typeof(CurrencyContent),
					out preview, out var exception);

				Assert.That(created, Is.True, exception?.ToString());
				Assert.That(preview, Is.TypeOf<CurrencyContent>());
				Assert.That(preview.Id, Is.EqualTo("currency.gems"));
				Assert.That(((CurrencyContent)preview).startingAmount, Is.EqualTo(0));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(preview);
			}
		}

		[Test]
		public void ContentHistoryInspectorPreview_RejectsMalformedJson()
		{
			var created = ContentHistoryInspectorPreview.TryCreate("{", "currency.gems", typeof(CurrencyContent),
				out var preview, out var exception);

			Assert.That(created, Is.False);
			Assert.That(preview, Is.Null);
			Assert.That(exception, Is.Not.Null);
		}

		[Test]
		public void ContentHistoryInspectorPreview_RejectsNonContentObjectType()
		{
			var created = ContentHistoryInspectorPreview.TryCreate("{}", "currency.gems", typeof(ScriptableObject),
				out var preview, out var exception);

			Assert.That(created, Is.False);
			Assert.That(preview, Is.Null);
			Assert.That(exception, Is.Not.Null);
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
