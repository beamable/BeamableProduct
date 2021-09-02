using Beamable.Editor.Content.Models;
using Beamable.Editor.Content;
using UnityEngine;
using Beamable.Editor.UI.Buss.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Config;
using Beamable.Editor.Content.Components;
using Beamable.Editor.Environment;
using Beamable.Editor.Modules.Theme;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class ActionBarVisualElement : MicroserviceComponent
    {
        public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as ActionBarVisualElement;

            }
        }
        
        public ActionBarVisualElement() : base(nameof(ActionBarVisualElement))
        {
        }

        public event Action OnStartAllClicked;
        public event Action OnBuildAllClicked;
        public event Action OnPublishClicked;
        public event Action OnRefreshButtonClicked;
        public event Action OnCreateNewClicked;
        private Button _refreshButton;
        private Button _createNew;
        private Button _startAll;
        private Button _infoButton;
        private Button _publish;
        private Button _buildAll;
        
        public event Action OnInfoButtonClicked;

        public override void Refresh()
        {
            base.Refresh();
            
            
            _refreshButton = Root.Q<Button>("refreshButton");
            _refreshButton.clickable.clicked += () => { OnRefreshButtonClicked?.Invoke(); };

            _createNew = Root.Q<Button>("createNew");
            _createNew.clickable.clicked += () => { OnCreateNewClicked?.Invoke(); };
            _createNew.SetEnabled(!DockerCommand.DockerNotInstalled);
            
            _startAll = Root.Q<Button>("startAll");
            _startAll.clickable.clicked += () => { OnStartAllClicked?.Invoke(); };
            _startAll.SetEnabled(!DockerCommand.DockerNotInstalled);
            
            _buildAll = Root.Q<Button>("buildAll");
            _buildAll.tooltip =
                "Build services, if service is already running, it will rebuild it and run again";
            _buildAll.clickable.clicked += () => { OnBuildAllClicked?.Invoke(); };
            _buildAll.SetEnabled(!DockerCommand.DockerNotInstalled);
            
            _publish = Root.Q<Button>("publish");
            _publish.clickable.clicked += () => { OnPublishClicked?.Invoke(); };
            _publish.SetEnabled(!DockerCommand.DockerNotInstalled);
            
            _infoButton = Root.Q<Button>("infoButton");
            _infoButton.clickable.clicked += () => { OnInfoButtonClicked?.Invoke(); };
            UpdateTextButtonTexts(false);
        }

        public void UpdateTextButtonTexts(bool allServicesSelected)
        {
            var startLabel = _startAll.Q<Label>();
            startLabel.text = allServicesSelected ? "Start all" : "Start selected";
            var buildLabel = _buildAll.Q<Label>();
            buildLabel.text = allServicesSelected ? "Build all" : "Build selected";
        }
    }

    
}