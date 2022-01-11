using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	public class Tester22 : MonoBehaviour
	{
		public string input;

		[ContextMenu("Clean")]
		public void Clean()
		{
			Debug.LogWarning(BussNameUtility.CleanString(input));
		}
	}
}
