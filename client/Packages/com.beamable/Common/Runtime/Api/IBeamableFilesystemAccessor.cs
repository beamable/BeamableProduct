using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Common.Api
{
	public interface IBeamableFilesystemAccessor
	{
		string GetPersistentDataPathWithoutTrailingSlash();
	}
}