using Beamable.Common;
using Beamable.Content;
using System;
using System.Collections.Generic;

namespace Editor.UI2.ContentWindow
{
	[Serializable]
	public class ContentPsCommandEvent
	{
		public const int EVT_TYPE_FullRebuild = 0;
		public const int EVT_TYPE_RemotePublished = 1;
		public const int EVT_TYPE_ChangedContent = 2;

		public int EventType;

		// Entries in here are to be added/updated in your in-memory state
		public List<LocalContentManifest> RelevantManifestsAgainstLatest;

		// The entries in here are to be removed from your in-memory state.
		public List<LocalContentManifest> ToRemoveLocalEntries;
	}

	[Serializable]
	public class LocalContentManifest
	{
		public string OwnerCid;
		public string OwnerPid;
		public string ManifestId;

		public LocalContentManifestEntry[] Entries;
	}

	[Serializable]
	public struct LocalContentManifestEntry
	{
		/// <summary>
		/// The full content id string.
		/// </summary>
		public string FullId;

		/// <summary>
		/// The type part of the <see cref="FullId"/>.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// The name part of the <see cref="FullId"/>
		/// </summary>
		public string Name;

		/// <summary>
		/// Int value of <see cref="ContentStatus"/> for this entry.
		/// </summary>
		public int CurrentStatus;

		/// <summary>
		/// The MD5 hash of the properties of this content.
		/// See <see cref="ContentService.CalculateChecksum(in Constants.Features.Content.ContentFile)"/>.
		/// </summary>
		public string Hash;

		/// <summary>
		/// The tags stored in this <see cref="ContentFile.Tags"/>.
		/// </summary>
		public string[] Tags;

		/// <summary>
		/// The array of <see cref="TagsStatus"/>.
		/// </summary>
		public int[] TagsStatus;

		/// <summary>
		/// The path to the local <see cref="ContentFile"/>.
		/// This is empty when the file is deleted locally.
		/// </summary>
		public string JsonFilePath;

		/// <summary>
		/// ID of the last published manifest to which this content was ever sync'ed. 
		/// </summary>
		public string ReferenceManifestUid;

		/// <summary>
		/// This is done this way because Unreal can't generate the enums properly but... unity can use this type directly.
		/// So... we declare this as a helper.
		/// </summary>
		public ContentStatus StatusEnum => (ContentStatus)CurrentStatus;
	}

	public enum ContentStatus
	{
		Valid,
		NotValid
	}
}
