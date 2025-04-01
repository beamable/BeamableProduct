using System;

namespace Beamable.Player.CloudSaving
{
	[Serializable]
	public class CloudSaveEntry
	{
		/// <summary>
		/// The Key of the CloudSave, the key is also the FileName
		/// </summary>
		public string key;

		/// <summary>
		/// Size in bytes of the file
		/// </summary>
		public int size;

		/// <summary>
		/// The long parsed value of dateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
		/// </summary>
		public long lastModified;

		/// <summary>
		/// MD5 hash used as checksum for the file
		/// </summary>
		public string eTag;

		[NonSerialized]
		public bool isModified;

		[NonSerialized]
		public bool isDeleted;

		public CloudSaveEntry(string key, int size, long lastModified, string eTag, bool isModified)
		{
			this.key = key;
			this.size = size;
			this.lastModified = lastModified;
			this.eTag = eTag;
			this.isModified = isModified;
		}
	}
}
