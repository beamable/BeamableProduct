using System.Linq;
using System.Reflection;

namespace Beamable.Common
{
	public static class ObjectExtensions
	{
		public static void TryInvokeCallback(this object target, string callbackMethodName, BindingFlags bindingFlags = 
			                                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
		{
			var method = target.GetType().GetMethods(bindingFlags).FirstOrDefault(m => m.Name == callbackMethodName);
			if (method == null)
				return;
			if (!method.GetParameters().Any())
				method.Invoke(target, null);
		}
	}
}
