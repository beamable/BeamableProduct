using System;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamStorageAssetJson : ScriptableObject
	{
		public string fileName;
		public string json;
	}
}
