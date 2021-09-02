using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Buss;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Toolbox.Components
{
   public class WhatsNewAnnouncementVisualElement : BeamableVisualElement
   {
      public WhatsNewAnnouncementModel WhatsNewAnnouncementModel { get; set; }
      public WhatsNewAnnouncementVisualElement() : base(
         $"{BeamableComponentsConstants.COMP_PATH}/{nameof(WhatsNewAnnouncementVisualElement)}/{nameof(WhatsNewAnnouncementVisualElement)}")
      {
      }

      public override void Refresh()
      {
         base.Refresh();
         
         var titleLabel = Root.Q<VisualElement>("title");
         var descLabel = Root.Q<VisualElement>("desc");
         
         var ignoreButton = Root.Q<Button>("ignore");
         var whatsnewButton = Root.Q<Button>("whatsnew");

         titleLabel.Add(WhatsNewAnnouncementModel.TitleElement);
         descLabel.Add(WhatsNewAnnouncementModel.DescriptionElement);
         
         ignoreButton.text = WhatsNewAnnouncementModel.IgnoreActionText;
         whatsnewButton.text = WhatsNewAnnouncementModel.WhatsNewActionText;

         ignoreButton.clickable.clicked += () => WhatsNewAnnouncementModel.OnIgnore?.Invoke();
         whatsnewButton.clickable.clicked += () => WhatsNewAnnouncementModel.OnWhatsNew?.Invoke();

         switch (WhatsNewAnnouncementModel.Status)
         {
            case ToolboxAnnouncementStatus.INFO:
               AddToClassList("info");
               break;
            case ToolboxAnnouncementStatus.WARNING:
               AddToClassList("warning");
               break;
            case ToolboxAnnouncementStatus.DANGER:
               AddToClassList("danger");
               break;
         }
         
      }
   }
}