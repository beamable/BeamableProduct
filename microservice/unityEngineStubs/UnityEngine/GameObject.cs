
namespace UnityEngine
{
	public class GameObject : Object
	{
		public Component[] GetComponents<T>() where T : Component => new Component[] { };
	}
}
