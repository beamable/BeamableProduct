using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI.Components;
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
        public static PublishWindow ShowPublishWindow()
        {
            var wnd = CreateInstance<PublishWindow>();
            wnd.titleContent = new GUIContent(Constants.Publish);

            ((PublishWindow) wnd).ShowUtility();
            wnd.minSize = Constants.WindowSizeMinimum;
            wnd.position = new Rect(wnd.position.x, wnd.position.y, wnd.minSize.x, wnd.minSize.y);

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
        }

        void Refresh()
        {
            var container = this.GetRootVisualContainer();
            container.Clear();


            var e = new PublishPopup {Model = _model};
            e.OnCloseRequested += Close;
            e.OnSubmit += async (model) =>
            {
                /*
                 * We need to build each image...
                 * upload each image that is different than whats in the manifest...
                 * upload the manifest file...
                 */
                e.parent.Remove(e);

                await Microservices.Deploy(model, this);
                Close();
            };

            container.Add(e);
            e.Refresh();
            Repaint();
        }
    }
}