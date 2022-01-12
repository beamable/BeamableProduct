using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.Microservice.UI.Components
{
	public class RemoteStorageObjectVisualElement : StorageObjectVisualElement
	{
		public new class UxmlFactory : UxmlFactory<RemoteStorageObjectVisualElement, UxmlTraits>
		{
		}

		protected override string ScriptName => nameof(StorageObjectVisualElement);

		protected override void UpdateRemoteStatusIcon()
		{
			_remoteStatusIcon.ClearClassList();
			string statusClassName = "remoteEnabled";
			_remoteStatusLabel.text = Constants.REMOTE_ONLY;
			_remoteStatusIcon.tooltip = _remoteStatusLabel.text;
			_remoteStatusIcon.AddToClassList(statusClassName);
		}

	}
}
