using System.Diagnostics;
using UnityEngine;

namespace Beamable
{
	public static class BeamableUndoUtility
	{
		[Conditional("UNITY_EDITOR")]
		public static void Undo(Object obj, string message) => UnityEditor.Undo.RecordObject(obj, message);
	}
}
