using Beamable.Editor.Login.UI;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Beamable.Editor.NoUser
{
   public class NoUserVisualElement : VisualElement
   {
      private const string Asset_UXML_Login =
         "Packages/com.beamable/Editor/UI/NoUser/nouser.uxml";

      private const string Asset_USS_Login =
         "Packages/com.beamable/Editor/UI/NoUser/nouser.uss";

      private VisualElement _root;

      public NoUserVisualElement()
      {
         var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Asset_UXML_Login);
         _root = treeAsset.CloneTree();
         this.AddStyleSheet(Asset_USS_Login);

         _root.Q<Button>().clickable.clicked += () =>
         {
            // go to login.
            var _ = LoginWindow.CheckLogin();
         };

         this.Add(_root);
      }
   }
}