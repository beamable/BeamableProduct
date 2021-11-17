using Beamable.Common.Api;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Platform.SDK
{
	public class PlatformFilesystemAccessor : IBeamableFilesystemAccessor

	{
		public string GetPersistentDataPathWithoutTrailingSlash()
		{
			return Application.persistentDataPath;
		}
	}
}
