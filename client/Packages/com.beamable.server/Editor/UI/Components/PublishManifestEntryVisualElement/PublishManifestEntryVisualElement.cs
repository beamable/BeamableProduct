using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
   public class PublishManifestEntryVisualElement : MicroserviceComponent
   {
      private static readonly string[] TemplateSizes = {"small", "medium", "large"};

      private const string MICROSERVICE_IMAGE_CLASS = "microserviceImage";
      private const string STORAGE_IMAGE_CLASS = "storageImage";
      private const string UNPUBLISHED_IMAGE_CLASS = "unpublished";
      private const string PUBLISHED_IMAGE_CLASS = "published";
      
      public IEntryModel Model { get; }

      private bool wasPublished;
      private bool oddRow;

      public PublishManifestEntryVisualElement(IEntryModel model, bool argWasPublished, bool isOddRow) : base(nameof(PublishManifestEntryVisualElement))
      {
         Model = model;
         wasPublished = argWasPublished;
         oddRow = isOddRow;
      }

      public override void Refresh()
      {
         base.Refresh();

         var checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
         checkbox.Refresh();
         checkbox.SetWithoutNotify(Model.Enabled);
         checkbox.OnValueChanged += b => Model.Enabled = b;

         var sizeDropdown = Root.Q<DropdownVisualElement>("sizeDropdown");
         sizeDropdown.Setup(TemplateSizes.ToList(), null);
         sizeDropdown.Refresh();

         var nameLabel = Root.Q<Label>("nameMS");
         nameLabel.text = Model.Name;

         var commentField = Root.Q<TextField>("commentsText");
         commentField.value = Model.Comment;
         commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);
         
         var icon = Root.Q<Image>("typeImage");

         if (Model is ManifestEntryModel serviceModel)
         {
            icon.AddToClassList(MICROSERVICE_IMAGE_CLASS);

            var microserviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(serviceModel.Name);

            if (microserviceModel.Dependencies != null)
            {
               List<string> dependencies = new List<string>();
               foreach (var dep in microserviceModel.Dependencies)
               {
                  dependencies.Add(dep.Name);
               }
               
               var depsList = Root.Q<ExpandableListVisualElement>("depsList");
               depsList.Setup(dependencies);
            }
         }
         else
         {
            icon.AddToClassList(STORAGE_IMAGE_CLASS);
         }

         SetPublishedIcon();

         if (oddRow)
         {
            Root.Q<VisualElement>("row").AddToClassList("oddRow");
         }
      }

      private void SetPublishedIcon()
      {
         string ussClass = wasPublished ? PUBLISHED_IMAGE_CLASS : UNPUBLISHED_IMAGE_CLASS;
         Root.Q<Image>("checkImage").AddToClassList(ussClass);
      }
   }
}