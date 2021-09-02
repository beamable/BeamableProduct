using Beamable.Editor.Content;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Content.Components
{
   public class ContentPopupLinkVisualElement : ContentManagerComponent
   {
      public ContentDownloadEntryDescriptor Model { get; set; }
      public VisualElement _checkIcon;

      public ContentPopupLinkVisualElement() : base(nameof(ContentPopupLinkVisualElement))
      {
      }

      public override void Refresh()
      {
         base.Refresh();

         var nameLbl = Root.Q<Label>("title");
         nameLbl.text = Model.ContentId;

         _checkIcon = Root.Q<VisualElement>("checkIcon");
      }

      // Content is downloaded.
      public void MarkChecked()
      {
         _checkIcon.RemoveFromClassList("unchecked");
         _checkIcon.AddToClassList("checked");
      }
   }
}