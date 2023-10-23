using System;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[CreateAssetMenu]
	[Serializable]
	public class BeamServiceAsset : ScriptableObject
	{
		public BeamServiceSignpost data;
	}
}
