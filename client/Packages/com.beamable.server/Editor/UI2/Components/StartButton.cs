using Beamable.Common;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System;
using UnityEngine.UIElements;

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
			var isRunning = _definition.IsRunningLocaly == BeamoServiceStatus.Running;
			EnableInClassList("running", isRunning);
		}

		private void HandleStartButtonClicked()
		{
			Action<Unit> callback = _ => _codeService.RefreshServices().Then(_ => { });
			if (_definition.IsRunningLocaly == BeamoServiceStatus.Running)
			{
				_definition.Builder.TryToStart().ToPromise().Then(callback);
			}
			else
			{
				_definition.Builder.TryToStop().ToPromise().Then(callback);
			}
		}
	}
}
