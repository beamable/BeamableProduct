using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
			provider.keywords = new HashSet<string>(new[] { "Microservice"});

			return provider;
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			BeamEditorContext.Default.OnReady.Then(async (_) =>
			{
				var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
				await codeService.OnReady;
				codeService.OnServicesRefresh -= RefreshData;
				codeService.OnServicesRefresh += RefreshData;
				RefreshData(null);
			});
		}

		private void RefreshData(List<IBeamoServiceDefinition> _)
		{
			SettingsService.NotifySettingsProviderChanged();
		}
	}
}
