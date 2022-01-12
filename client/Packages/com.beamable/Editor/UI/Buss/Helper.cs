using System;
using System.Collections.Generic;
using System.Linq;

namespace Editor.UI.Buss
{
	public static class Helper
	{
		public static IEnumerable<Type> GetAllClassesInheritedFrom(Type baseClass) =>
			AppDomain.CurrentDomain.GetAssemblies()
			         .SelectMany(assembly => assembly.GetTypes())
			         .Where(x => x.IsClass && !x.IsAbstract && x.IsInheritedFrom(baseClass));

		public static List<string> GetAllClassesNamesInheritedFrom(Type baseClass) =>
			GetAllClassesInheritedFrom(baseClass).Select(x => x.Name).ToList();
	}
}
