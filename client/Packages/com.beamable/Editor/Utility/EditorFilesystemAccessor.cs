using Beamable.Common.Api;
using UnityEngine;

namespace Beamable.Editor
{
	public class EditorFilesystemAccessor : IBeamableFilesystemAccessor
	{
		public string GetPersistentDataPathWithoutTrailingSlash()
		{
			// TODO: this may collide if you have multiple org/game projects open
			// TODO: include a hash of the current /Assets folder
			return Application.persistentDataPath;
		}
	}
}
