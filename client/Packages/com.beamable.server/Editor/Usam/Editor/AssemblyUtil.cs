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

			var beamServices = CodeService.GetBeamServices();
			foreach (var service in beamServices)
			{
				foreach (var reference in service.assemblyReferences)
				{
					if (!_nameToAssembly.TryGetValue(reference, out var assembly)) continue;
					_referencedAssemblies.Add(assembly);
					foreach (var subReference in GetDeeplyReferencedAssemblies(assembly))
					{
						_referencedAssemblies.Add(subReference);
					}
				}
			}
		}

		static bool IsValidAssemblyReference(Assembly assembly)
		{
			foreach (var prefix in _invalidAssemblyPrefixes)
			{
				if (assembly.name.StartsWith(prefix)) return false;
			}

			return true;
		}

		static IEnumerable<Assembly> GetDeeplyReferencedAssemblies(Assembly assembly)
		{
			var references = _assemblyGraph[assembly];
			foreach (var reference in references)
			{
				if (!IsValidAssemblyReference(reference)) continue;
				yield return reference;
			}
			foreach (var reference in references)
			{
				if (!IsValidAssemblyReference(reference)) continue;
				foreach (var subReference in GetDeeplyReferencedAssemblies(reference))
				{
					yield return subReference;
				}
			}
		}

	}
}
