using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceAsset : ScriptableObject
	{
		public BeamServiceSignpost data;

		private void OnValidate()
		{
			if (!CheckAllValidAssemblies()) return; //TODO: We should show in the asset inspector in case there are invalid references in there
			
			List<string> allNames = data.assemblyReferences.Length > 0 ? data.assemblyReferences.Select(rf => rf.name).ToList() : new List<string>();
			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			_ = codeService.UpdateServiceReferences(data.name, allNames);
		}

		private bool CheckAllValidAssemblies()
		{

			//Check if there is any null reference in the array
			foreach (AssemblyDefinitionAsset assembly in data.assemblyReferences)
			{
				if (assembly == null) return false;
			}

			List<string> names = data.assemblyReferences.Select(rf => rf.name).ToList();

			//Check if there are duplicates in the list
			if (names.Count != names.Distinct().Count())
			{
				return false;
			}

			//Check if that reference is a reference that we can add to the microservice
			foreach (var referenceName in names)
			{
				if (!CsharpProjectUtil.IsValidReference(referenceName))
				{
					return false;
				}
			}

			return true;
		}
	}
}
