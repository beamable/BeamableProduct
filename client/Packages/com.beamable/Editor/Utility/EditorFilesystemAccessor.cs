using Beamable.Common.Api;
using UnityEngine;

namespace Beamable.Editor
{
	public class EditorFilesystemAccessor : IBeamableFilesystemAccessor
	{
		public string GetPersistentDataPathWithoutTrailingSlash()
		{
			return "./Library/BeamableEditor";
		}
	}
}
