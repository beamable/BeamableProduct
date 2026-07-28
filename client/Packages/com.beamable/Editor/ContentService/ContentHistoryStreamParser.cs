using Beamable.Editor.BeamCli.Commands;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Adapts the CLI's content-history stream payloads to the generated Unity DTOs.
	/// The generated DTOs use Pascal-case members, while the CLI emits camel-case fields inside history entries and changelists;
	/// keeping this mapping here avoids changing generated command code or the CLI wire contract.
	/// </summary>
	public static class ContentHistoryStreamParser
	{
		public static IReadOnlyList<BeamContentHistoryEntry> ParseEntries(string streamJson)
		{
			var update = Parse(streamJson);
			return update?.Entries ?? Array.Empty<BeamContentHistoryEntry>();
		}

		public static BeamContentHistoryChangelistEntry[] ParseChanges(string streamJson)
		{
			return TryParseChanges(streamJson, out var entries)
				? entries
				: Array.Empty<BeamContentHistoryChangelistEntry>();
		}

		public static bool TryParseChanges(string streamJson, out BeamContentHistoryChangelistEntry[] entries)
		{
			var report = JsonUtility.FromJson<ContentHistoryChangeStreamReport>(streamJson);
			if (report == null || report.type != "stream" || report.data?.ContentEntries == null)
			{
				entries = Array.Empty<BeamContentHistoryChangelistEntry>();
				return false;
			}

			entries = report.data.ContentEntries
				.Where(entry => !string.IsNullOrEmpty(entry.fullId))
				.Select(entry => new BeamContentHistoryChangelistEntry
				{
					OldVersion = entry.oldVersion,
					OldChecksum = entry.oldChecksum,
					OldTags = entry.oldTags,
					NewVersion = entry.newVersion,
					NewChecksum = entry.newChecksum,
					NewTags = entry.newTags,
					JsonFilePath = entry.jsonFilePath,
					FullId = entry.fullId,
					TypeName = entry.typeName,
					Name = entry.name,
					ChangeStatus = entry.changeStatus,
					ChangeDate = entry.changeDate
				})
				.ToArray();
			return true;
		}

		public static bool TryParseChangelist(string streamJson, out BeamContentHistoryChangelistEntry[] entries)
		{
			entries = Array.Empty<BeamContentHistoryChangelistEntry>();
			try
			{
				if (Json.Deserialize(streamJson) is not ArrayDict report ||
					!TryGetString(report, "type", out var reportType) || reportType != "stream" ||
					!TryGetObject(report, "data", out var data) ||
					!TryGetObject(data, "Changelist", out var changelist))
				{
					return false;
				}

				var publishedAt = TryGetLong(changelist, "publishedAt", out var timestamp) ? timestamp : 0;
				var parsedEntries = new List<BeamContentHistoryChangelistEntry>();
				AddChangelistEntries(changelist, "added", ContentStatus.Created, publishedAt, parsedEntries);
				AddChangelistEntries(changelist, "modified", ContentStatus.Modified, publishedAt, parsedEntries);
				AddChangelistEntries(changelist, "removed", ContentStatus.Deleted, publishedAt, parsedEntries);
				entries = parsedEntries.ToArray();
				return true;
			}
			catch (Exception)
			{
				entries = Array.Empty<BeamContentHistoryChangelistEntry>();
				return false;
			}
		}

		public static ContentHistoryStreamUpdate Parse(string streamJson)
		{
			var report = JsonUtility.FromJson<ContentHistoryStreamReport>(streamJson);
			if (report == null || report.type != "stream" || report.data == null || report.data.EventType != 0)
			{
				return null;
			}

			var entries = report.data.EntriesPage?.Entries?
				.Where(entry => !string.IsNullOrEmpty(entry.manifestUid))
				.Select(entry => new BeamContentHistoryEntry
				{
					ManifestUid = entry.manifestUid,
					CreatedDate = entry.createdDate,
					PublishedBy = entry.publishedBy,
					PublishedByName = entry.publishedByName,
					AffectedContentIds = entry.affectedContentIds
				})
				.ToArray() ?? Array.Empty<BeamContentHistoryEntry>();

			return new ContentHistoryStreamUpdate(entries, report.data.EntriesToRemove);
		}

		private static void AddChangelistEntries(ArrayDict changelist, string key, ContentStatus status, long publishedAt,
			ICollection<BeamContentHistoryChangelistEntry> entries)
		{
			if (!TryGetObject(changelist, key, out var category))
			{
				return;
			}

			foreach (var pair in category)
			{
				if (string.IsNullOrEmpty(pair.Key) || pair.Value is not ArrayDict entry)
				{
					continue;
				}

				var separatorIndex = pair.Key.IndexOf('.', StringComparison.Ordinal);
				var typeName = separatorIndex > 0 ? pair.Key.Substring(0, separatorIndex) : string.Empty;
				var name = separatorIndex >= 0 && separatorIndex < pair.Key.Length - 1
					? pair.Key.Substring(separatorIndex + 1)
					: pair.Key;
				entries.Add(new BeamContentHistoryChangelistEntry
				{
					OldVersion = GetString(entry, "oldVersion"),
					OldChecksum = GetString(entry, "oldChecksum"),
					OldTags = GetStringArray(entry, "oldTags"),
					NewVersion = GetString(entry, "newVersion"),
					NewChecksum = GetString(entry, "newChecksum"),
					NewTags = GetStringArray(entry, "newTags"),
					FullId = pair.Key,
					TypeName = typeName,
					Name = name,
					ChangeStatus = (int)status,
					ChangeDate = publishedAt
				});
			}
		}

		private static bool TryGetObject(ArrayDict source, string key, out ArrayDict value)
		{
			if (source != null && source.TryGetValue(key, out var rawValue) && rawValue is ArrayDict dictionary)
			{
				value = dictionary;
				return true;
			}

			value = null;
			return false;
		}

		private static bool TryGetString(ArrayDict source, string key, out string value)
		{
			value = GetString(source, key);
			return value != null;
		}

		private static string GetString(ArrayDict source, string key)
		{
			return source != null && source.TryGetValue(key, out var value) ? value?.ToString() : null;
		}

		private static string[] GetStringArray(ArrayDict source, string key)
		{
			if (source == null || !source.TryGetValue(key, out var value) || value is not List<object> values)
			{
				return null;
			}

			return values.Select(item => item?.ToString()).ToArray();
		}

		private static bool TryGetLong(ArrayDict source, string key, out long value)
		{
			value = 0;
			return source != null && source.TryGetValue(key, out var rawValue) &&
				long.TryParse(rawValue?.ToString(), out value);
		}

		[Serializable]
		private class ContentHistoryStreamReport
		{
			public string type;
			public ContentHistoryStreamData data;
		}

		[Serializable]
		private class ContentHistoryStreamData
		{
			public int EventType;
			public ContentHistoryStreamEntriesPage EntriesPage;
			public string[] EntriesToRemove;
		}

		[Serializable]
		private class ContentHistoryStreamEntriesPage
		{
			public ContentHistoryStreamEntry[] Entries;
		}

		[Serializable]
		private class ContentHistoryStreamEntry
		{
			public string manifestUid;
			public long createdDate;
			public string publishedBy;
			public string publishedByName;
			public string[] affectedContentIds;
		}

		[Serializable]
		private class ContentHistoryChangeStreamReport
		{
			public string type;
			public ContentHistoryChangeStreamData data;
		}

		[Serializable]
		private class ContentHistoryChangeStreamData
		{
			public ContentHistoryChangeStreamEntry[] ContentEntries;
		}

		[Serializable]
		private class ContentHistoryChangeStreamEntry
		{
			public string oldVersion;
			public string oldChecksum;
			public string[] oldTags;
			public string newVersion;
			public string newChecksum;
			public string[] newTags;
			public string jsonFilePath;
			public string fullId;
			public string typeName;
			public string name;
			public int changeStatus;
			public long changeDate;
		}
	}

	public class ContentHistoryStreamUpdate
	{
		public readonly IReadOnlyList<BeamContentHistoryEntry> Entries;
		public readonly IReadOnlyList<string> EntriesToRemove;

		public ContentHistoryStreamUpdate(IReadOnlyList<BeamContentHistoryEntry> entries, IReadOnlyList<string> entriesToRemove)
		{
			Entries = entries;
			EntriesToRemove = entriesToRemove;
		}
	}
}
