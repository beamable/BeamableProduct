using Beamable.Editor.BeamCli.Commands;
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
