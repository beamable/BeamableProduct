using UnityEditor;
using UnityEngine;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Server.Editor.UI
{
   public class DeployWindow : CommandRunnerWindow
   {
      public static DeployWindow ShowDeployPopup()
      {
         var wnd = CreateInstance<DeployWindow>();
         wnd.titleContent = new GUIContent("Deploy");

         ((EditorWindow) wnd).ShowUtility();

         Microservices.GenerateUploadModel().Then(model =>
         {
            wnd._model = model;
            wnd.Refresh();
         });

         return wnd;
      }


      //TODO: Use BeamableComponentsConstants instead? - srivello
      private const string PATH = "Packages/com.beamable/Editor/UI/Common/Components/BeamablePopupWindow";

      private VisualElement _windowRoot;
      private VisualElement _contentRoot;
      private VisualElement _container;
      private ManifestModel _model;


      private void OnEnable()
      {
         VisualElement root = this.GetRootVisualContainer();
         var uiAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{PATH}/beamablePopupWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         _windowRoot.AddStyleSheet($"{PATH}/beamablePopupWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         root.Add(_windowRoot);
      }

      public void Refresh()
      {
         _container = _windowRoot.Q<VisualElement>("container");
         _container.Clear();


         var e = new ManifestVisualElement(_model);
         e.OnCancel += Close;
         e.OnSubmit += async (model) =>
         {
            /*
             * We need to build each image...
             * upload each image that is different than whats in the manifest...
             * upload the manifest file...
             */
            e.parent.Remove(e);

            await Microservices.Deploy(_model, this);
            Close();
         };

         _container.Add(e);
         e.Refresh();
         Repaint();
      }
   }
}