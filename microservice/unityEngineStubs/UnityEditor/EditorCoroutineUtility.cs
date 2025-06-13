using System.Collections;

namespace Unity.EditorCoroutines.Editor
{
	public static class EditorCoroutineUtility
	{
		public static EditorCoroutine StartCoroutine(IEnumerator routine, object owner)
		{
			return new EditorCoroutine(routine, owner);
		}

		public static void StopCoroutine(EditorCoroutine validateCoroutine)
		{
			
		}
	}
}
