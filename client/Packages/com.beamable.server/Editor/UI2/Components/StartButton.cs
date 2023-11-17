using Beamable.Common;
using Beamable.Server.Editor.Usam;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Usam;

namespace Beamable.Editor.Microservice.UI2.Components
{
	public class StartButton : Button, IBeamoServiceElement
	{
		public new class UxmlFactory : UxmlFactory<StartButton, UxmlTraits> { }

		private CodeService _codeService;
		private IBeamoServiceDefinition _definition;

		public void FeedData(IBeamoServiceDefinition definition, BeamEditorContext editorContext)
		{
			HandleUpdate(definition);
			_definition.Updated -= HandleUpdate;
			_definition.Updated += HandleUpdate;
			clickable.clicked -= HandleStartButtonClicked;
			clickable.clicked += HandleStartButtonClicked;
			_codeService = editorContext.ServiceScope.GetService<CodeService>();
		}

		private void HandleUpdate(IBeamoServiceDefinition definition)
		{
			_definition = definition;
			EnableInClassList("building", enabledSelf);
			var isRunning = _definition.IsRunningLocaly == ServiceStatus.Running;
			EnableInClassList("running", isRunning);
		}

		private void HandleStartButtonClicked()
		{
			Action<Unit> callback = _ => _codeService.RefreshServices().Then(_ => { });
			if (_definition.IsRunningLocaly == ServiceStatus.Running)
			{
				_codeService.Stop(new[] { _definition }).Then(callback);
			}
			else
			{
				_codeService.Run(new[] { _definition }).Then(callback);
			}
		}
	}
}
