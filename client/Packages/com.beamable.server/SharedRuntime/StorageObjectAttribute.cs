using System;

namespace Beamable.Server
{
	[AttributeUsage(AttributeTargets.Class)]
	public class StorageObjectAttribute : Attribute
	{
		public string StorageName
		{
			get;
		}

		public string SourcePath
		{
			get;
		}

		public StorageObjectAttribute(string storageName,
									  [System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
		{
			StorageName = storageName;
			SourcePath = sourcePath;
		}
	}
}
