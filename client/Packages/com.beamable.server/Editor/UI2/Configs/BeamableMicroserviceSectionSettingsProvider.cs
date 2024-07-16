using System.Collections.Generic;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	public class BeamableMicroserviceSectionSettingsProvider : SettingsProvider
	{
		public BeamableMicroserviceSectionSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

		[SettingsProvider]
		public static SettingsProvider CreateMicroservicesSectionProvider()
		{
			var provider =
				new BeamableMicroserviceSectionSettingsProvider("Project/Beamable Services",
																SettingsScope.Project);
			provider.keywords = new HashSet<string>(new[] { "Microservice" });

			return provider;
		}
	}
}
