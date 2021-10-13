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
   public class StorageDepencencyWarningVisualElement : BeamableVisualElement
   {
      public StorageDepencencyWarningModel StorageDepencencyWarningModel { get; set; }
      public StorageDepencencyWarningVisualElement() : base(
         $"{BeamableComponentsConstants.COMP_PATH}/{nameof(StorageDepencencyWarningVisualElement)}/{nameof(StorageDepencencyWarningVisualElement)}")
      {
      }

      public override void Refresh()
      {
         base.Refresh();
         
         var titleLabel = Root.Q<Label>("announcement-title");
         titleLabel.text = StorageDepencencyWarningModel.TitleLabelText;
         titleLabel.AddTextWrapStyle();
         
         var descriptionLabel = Root.Q<Label>("announcement-description");
         descriptionLabel.text = StorageDepencencyWarningModel.DescriptionLabelText;
         descriptionLabel.AddTextWrapStyle();
      }
   }
}