using Beamable.Server.Editor.Usam;
using System.Collections.Generic;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableMicroservicesSettingsProvider : SettingsProvider
	{
		public BeamableMicroservicesSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

		[SettingsProviderGroup]
		public static SettingsProvider[] CreateMicroservicesSettingsProvider()
		{
			var allProviders = new List<SettingsProvider>();
			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();

			foreach (var definition in codeService.ServiceDefinitions)
			{
				if (!definition.ExistLocally)
				{
					continue;
				}


				var provider =
					new BeamableMicroservicesSettingsProvider("Project/Beamable Services/" + definition.BeamoId,
					                                          SettingsScope.Project);
				provider.keywords = new HashSet<string>(new[] { "Microservice", definition.BeamoId});
				allProviders.Add(provider);
			}

			return allProviders.ToArray();
		}
	}
}
