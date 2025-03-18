using System;

namespace Beamable.Player.CloudSaving
{
	public struct DataConflictDetail : IEquatable<DataConflictDetail>
	{
		/// <summary>
		/// The Name of the Save file that has a conflict between local and cloud data.
		/// </summary>
		public string FileName;
		/// <summary>
		/// The <see cref="CloudSaveEntry"/> data of the Local Save
		/// </summary>
		public CloudSaveEntry LocalSaveEntry;
		/// <summary>
		/// The <see cref="CloudSaveEntry"/> data of the Cloud Save
		/// </summary>
		public CloudSaveEntry CloudSaveEntry;

		/// <summary>
		/// The path for the local saved file
		/// </summary>
		public string LocalFilePath;
		
		/// <summary>
		/// The path for the temporarily download Remote File.
		/// </summary>
		public string CloudFilePath;

		public bool Equals(DataConflictDetail other)
		{
			return FileName == other.FileName && 
			       Equals(LocalSaveEntry, other.LocalSaveEntry) &&
			       Equals(CloudSaveEntry, other.CloudSaveEntry) && 
			       LocalFilePath == other.LocalFilePath &&
			       CloudFilePath == other.CloudFilePath;
		}

		public override bool Equals(object obj)
		{
			return obj is DataConflictDetail other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FileName, LocalSaveEntry, CloudSaveEntry, LocalFilePath, CloudFilePath);
		}
	}
}
