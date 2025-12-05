
using System;

namespace UnityEngine
{
   public class ScriptableObject
   {
      public string name;
      public static T CreateInstance<T>() where T : new()
      {
	      return new T();
      }
      public static ScriptableObject CreateInstance(Type type)
      {
	      return new ScriptableObject();
      }
   }
}
