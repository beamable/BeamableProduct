using Beamable.Common;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class UsamAssemblyService
	{
		private readonly UsamService _usam;
		private Assembly[] _assemblies;
		private Dictionary<string, Assembly> _nameToAssembly;
		private Dictionary<Assembly, Assembly[]> _assemblyGraph;
		private HashSet<Assembly> _referencedAssemblies = new HashSet<Assembly>();
		public Dictionary<string, string> beamoIdToClientHintPath = new Dictionary<string, string>();

		public HashSet<Assembly> ReferencedAssemblies => _referencedAssemblies;
		public Assembly[] AllAssemblies => _assemblies;

		public UsamAssemblyService(UsamService usam)
		{
			_usam = usam;
		}

		public void Reload()
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

			foreach (var definition in _usam.latestManifest.services)
			{
				{ // set the hint path, either as the default, or if there is an assembly definition matching the convention
					foreach (var kvp in _nameToAssembly)
					{
						var matchesConvention =
							kvp.Key.EndsWith($"{definition.beamoId}.client",
							                 StringComparison.InvariantCultureIgnoreCase);
						if (matchesConvention)
						{
							var hintPath = Path.GetDirectoryName(kvp.Value.sourceFiles[0]);
							beamoIdToClientHintPath[definition.beamoId] = Path.Combine(hintPath, $"{definition.beamoId}Client.cs");
						}
					}
				}

				foreach (var reference in definition.unityReferences)
				{
					if (!_nameToAssembly.TryGetValue(reference.AssemblyName, out var assembly)) continue;
					if (!CsharpProjectUtil.IsValidReference(assembly.name)) continue;
					_referencedAssemblies.Add(assembly);
					foreach (var subReference in GetDeeplyReferencedAssemblies(assembly))
					{
						_referencedAssemblies.Add(subReference);
					}
				}
			}
		}

		IEnumerable<Assembly> GetDeeplyReferencedAssemblies(Assembly assembly)
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
