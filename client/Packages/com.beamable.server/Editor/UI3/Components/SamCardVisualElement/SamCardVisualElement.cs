using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor.Usam;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI3.Components.SamCardVisualElement
{
	public class SamCardVisualElement : BeamableVisualElement
	{
		private readonly string _beamoId;
		
		public SamCardVisualElement(string beamoId) : base($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/UI3/Components/{nameof(SamCardVisualElement)}/{nameof(SamCardVisualElement)}")
		{
			_beamoId = beamoId;
		}

		public override void Refresh()
		{
			base.Refresh();

			var model = Provider.GetService<SamModel>();
			var service = model.idToService[_beamoId]; // TODO: exception catch.
			
			Root.Q<Label>("card-txt-type").text = "service";
			Root.Q<Label>("card-txt-title").text = _beamoId;

			var logRoot = Root.Q<VisualElement>("card-logs");
			var logContainer = new SamLogsVisualElement(_beamoId);
			logRoot.Add(logContainer);
			logContainer.Refresh();
			
			
			var dropdownBtn = Root.Q<VisualElement>("card-btn-dropdown");
			dropdownBtn.tooltip = Constants.Tooltips.Microservice.MORE;
			
			var swaggerBtn = Root.Q<VisualElement>("card-btn-swagger");
			swaggerBtn.tooltip = "Goto OpenAPI Specification";
			swaggerBtn.AddManipulator(new Clickable(async () =>
			{
				var shouldOpen = service.isRunning;
				if (!service.isRunning)
				{
					shouldOpen = EditorUtility.DisplayDialog("Service Is Not Running",
					                                         $"The {_beamoId} service is not running, which means the Open API documentation will not load. Would you like to open the page anyway, even though the documentation won't load?",
					                                         "open", "cancel");
				}

				if (shouldOpen)
				{
					await Provider.GetService<CodeService>().OpenSwagger(_beamoId);
				}
			}));

			
			var codeBtn = Root.Q<VisualElement>("card-btn-code");
			codeBtn.tooltip = "Goto Source Code";
			codeBtn.AddManipulator(new Clickable(() =>
			{
				Provider.GetService<CodeService>().OpenMicroserviceFile(_beamoId);
			}));
			
			var runBtn = Root.Q<VisualElement>("card-btn-start");
			if (service.isRunning)
			{
				runBtn.tooltip = $"Stop {_beamoId}";
				Root.EnableInClassList("running", true);
			}
			else
			{
				runBtn.tooltip = $"Run {_beamoId}";
			}
			runBtn.AddManipulator(new Clickable(() =>
			{
				runBtn.SetEnabled(false);
				if (service.isRunning)
				{
					Provider.GetService<CodeService>().StopStandaloneMicroservice(_beamoId);
				}
				else
				{
					Provider.GetService<CodeService>().RunStandaloneMicroservice(_beamoId);
				}
			}));
			
		}
	}
}
