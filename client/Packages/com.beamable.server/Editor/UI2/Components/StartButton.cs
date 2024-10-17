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
			_definition = definition;
			_definition.Builder.OnIsRunningChanged -= HandleUpdate;
			_definition.Builder.OnIsRunningChanged += HandleUpdate;
			clickable.clicked -= HandleStartButtonClicked;
			clickable.clicked += HandleStartButtonClicked;
			_codeService = editorContext.ServiceScope.GetService<CodeService>();
		}

		private void HandleUpdate(bool runningState)
		{
			SetEnabled(true);
			EnableInClassList("building", false);
		}

		private void HandleStartButtonClicked()
		{
			EnableInClassList("building", true);
			SetEnabled(false);

			if (_definition.IsRunningLocally)
			{
				_definition.Builder.TryToStop();
			}
			else
			{
				_definition.Builder.TryToStart();
			}
		}
	}
}
