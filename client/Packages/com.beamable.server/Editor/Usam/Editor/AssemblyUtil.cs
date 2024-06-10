using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class AssemblyUtil
	{


		private static Assembly[] _assemblies;
		private static Dictionary<string, Assembly> _nameToAssembly;
		private static Dictionary<Assembly, Assembly[]> _assemblyGraph;

		private static HashSet<Assembly> _referencedAssemblies = new HashSet<Assembly>();

		private static string[] _invalidAssemblyPrefixes = new string[] { "UnityEngine.", "UnityEditor." };

		public static HashSet<Assembly> ReferencedAssemblies => _referencedAssemblies;

		public static void Reload()
		{
			_assemblies = CompilationPipeline.GetAssemblies();

			// CompilationPipeline.
			// Create a dictionary to store the assembly dependencies
			_assemblyGraph = new Dictionary<Assembly, Assembly[]>();
			_nameToAssembly = new Dictionary<string, Assembly>();
			_referencedAssemblies.Clear();
			// Iterate through each assembly and its references
			foreach (Assembly assembly in _assemblies)
			{
				string assemblyName = assembly.name;
				_nameToAssembly[assemblyName] = assembly;
				_assemblyGraph[assembly] = assembly.assemblyReferences;
			}

			//TODO this needs to be refactored to use ServicesDefinitions, which right now doesn't have the access to the assemblies list
			/*var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			foreach (var definition in codeService.ServiceDefinitions)
			{
				foreach (var reference in definition.)
				{
					if (!_nameToAssembly.TryGetValue(reference.name, out var assembly)) continue;
					if (!CsharpProjectUtil.IsValidReference(assembly.name)) continue;
					_referencedAssemblies.Add(assembly);
					foreach (var subReference in GetDeeplyReferencedAssemblies(assembly))
					{
						_referencedAssemblies.Add(subReference);
					}
				}
			}*/
		}

		static IEnumerable<Assembly> GetDeeplyReferencedAssemblies(Assembly assembly)
		{
			var references = _assemblyGraph[assembly];
			foreach (var reference in references)
			{
				if (!CsharpProjectUtil.IsValidReference(reference.name)) continue;
				yield return reference;
			}
			foreach (var reference in references)
			{
				if (!CsharpProjectUtil.IsValidReference(reference.name)) continue;
				foreach (var subReference in GetDeeplyReferencedAssemblies(reference))
				{
					yield return subReference;
				}
			}
		}

	}
}
