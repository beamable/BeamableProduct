using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using System;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class PublishWindow : CommandRunnerWindow
    {
        private static readonly Vector2 MIN_SIZE = new Vector2(860, 550);
        
    	private bool isSet = false;

        public static PublishWindow ShowPublishWindow()
        {
            var wnd = CreateInstance<PublishWindow>();
            wnd.name = Constants.Publish;
            wnd.titleContent = new GUIContent(Constants.Publish);

            ((PublishWindow) wnd).ShowUtility();
            wnd.minSize = MIN_SIZE;
            wnd.position = new Rect(wnd.position.x, wnd.position.y + 40, wnd.minSize.x, wnd.minSize.y);

            Microservices.GenerateUploadModel().Then(model =>
            {
                wnd._model = model;
                wnd.Refresh();
            });
            
            return wnd;
        }

        private ManifestModel _model;

        private void OnEnable()
        {
	        VisualElement root = this.GetRootVisualContainer();
	        
            if (isSet)
            {
                this.Refresh();

                Microservices.GenerateUploadModel().Then(model =>
                {
                    this._model = model;
                    this.Refresh();
                });
            }
        }
        
        void Refresh()
        {
            var container = this.GetRootVisualContainer();
            container.Clear();
            var e = new PublishPopup { Model = _model };
            
            e.OnCloseRequested += Close;
            e.OnSubmit += async (model) =>
            {
	            WindowStateUtility.DisableAllWindows();
	            e.PrepareForPublish();
	            await Microservices.Deploy(model, this, e.ServiceDeployed, false);
                Close();
            };

            container.Add(e);
            e.PrepareParent();
            e.Refresh();
            Repaint();
            

            isSet = true;
        }
    }
}
