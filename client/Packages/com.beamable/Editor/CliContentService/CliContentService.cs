using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Editor.UI.ContentWindow;
using System;

namespace Editor.CliContentManager
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private BeamLocalContentManifest _cachedManifest;

		public BeamLocalContentManifest CachedManifest => _cachedManifest;
		public void ReceiveStorageHandle(StorageHandle<CliContentService> handle)
		{
			//
		}
	}
}
