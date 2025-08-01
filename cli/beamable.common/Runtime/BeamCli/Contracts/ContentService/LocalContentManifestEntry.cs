using Beamable.Common.BeamCli;
using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
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
		/// Whether this content was modified on top of a <see cref="ReferenceManifestUid"/>
		/// that is no longer the latest AND that it was modified in the latest one relative to the reference one.
		/// </summary>
		public bool IsInConflict;

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
		/// The latest time that this content were changed
		/// </summary>
		public long LatestUpdateAtDate;

		/// <summary>
		/// This is done this way because Unreal can't generate the enums properly but... unity can use this type directly.
		/// So... we declare this as a helper.
		/// </summary>
		public ContentStatus StatusEnum => (ContentStatus)CurrentStatus;
	}
}
