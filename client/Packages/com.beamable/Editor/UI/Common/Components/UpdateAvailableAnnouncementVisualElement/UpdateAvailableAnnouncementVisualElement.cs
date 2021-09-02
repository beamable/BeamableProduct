using Beamable.Editor.Environment;
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
   public class UpdateAvailableAnnouncementVisualElement : BeamableVisualElement
   {
      public UpdateAvailableAnnouncementModel UpdateAvailableAnnouncementModel { get; set; }
      public UpdateAvailableAnnouncementVisualElement() : base(
         $"{BeamableComponentsConstants.COMP_PATH}/{nameof(UpdateAvailableAnnouncementVisualElement)}/{nameof(UpdateAvailableAnnouncementVisualElement)}")
      {
      }

      public override void Refresh()
      {
         base.Refresh();
         
         var titleLabel = Root.Q<VisualElement>("title");
         titleLabel.Add(UpdateAvailableAnnouncementModel.TitleElement);
         
         var descLabel = Root.Q<VisualElement>("desc");
         descLabel.Add(UpdateAvailableAnnouncementModel.DescriptionElement);
         
         var ignoreButton = Root.Q<Button>("ignore");
         ignoreButton.text = UpdateAvailableAnnouncementModel.IgnoreActionText;
         ignoreButton.clickable.clicked += () => UpdateAvailableAnnouncementModel.OnIgnore?.Invoke();

         var whatsnewButton = Root.Q<Button>("whatsnew");
         if (BeamablePackages.BeamablePackageUpdateMeta.IsBlogSiteAvailable)
         {
            whatsnewButton.text = UpdateAvailableAnnouncementModel.WhatsNewActionText;
            whatsnewButton.clickable.clicked += () => UpdateAvailableAnnouncementModel.OnWhatsNew?.Invoke();
            whatsnewButton.visible = true;
         }
         else
         {
            whatsnewButton.visible = false;
         }

         var installButton = Root.Q<Button>("install");
         installButton.text = UpdateAvailableAnnouncementModel.InstallActionText;
         installButton.clickable.clicked += () => UpdateAvailableAnnouncementModel.OnInstall?.Invoke();

         switch (UpdateAvailableAnnouncementModel.Status)
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