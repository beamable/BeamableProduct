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
    public class StorageObjectVisualElement : ServiceBaseVisualElement<MongoStorageModel>
    {
        protected override void UpdateStatusIcon()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateRemoteStatusIcon()
        {
            throw new NotImplementedException();
        }
    }
}