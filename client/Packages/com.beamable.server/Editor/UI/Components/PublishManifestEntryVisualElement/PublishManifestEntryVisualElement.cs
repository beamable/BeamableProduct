using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
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

         var typeLabel = Root.Q<Label>("typeLabel");
         typeLabel.text = Model.Type;
         
         var nameLabel = Root.Q<Label>("nameMS");
         nameLabel.text = Model.Name;

         var commentField = Root.Q<TextField>("commentsText");
         commentField.value = Model.Comment;
         commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);
         
         var icon = Root.Q<Image>("typeImage");

         if (Model is ManifestEntryModel serviceModel)
         {
            icon.AddToClassList(MICROSERVICE_IMAGE_CLASS);  
            
            var serviceDescriptor = Microservices.Descriptors.Find(descriptor => descriptor.Name == serviceModel.Name);
            var serviceDependencies = new List<ServiceDependency>();
            foreach (var storage in serviceDescriptor.GetStorageReferences())
            {
               serviceDependencies.Add(new ServiceDependency
               {
                  id = storage.Name, 
                  type = "storage"
               });
            }

            serviceModel.Dependencies = serviceDependencies;
            
            if (serviceModel.Dependencies != null)
            {
               string[] dependencies = new string[serviceModel.Dependencies.Count];
               for (int i = 0; i < dependencies.Length; i++)
               {
                  dependencies[i] = serviceModel.Dependencies[i].id;
               }
               
               var dropdown = Root.Q<DropdownVisualElement>("depsDropdown");
               if (dependencies.Length > 0)
               {
                  dropdown.Setup(dependencies.ToList(), selected =>
                  {
                     // don't allow to change selection
                     if (selected != dependencies[0])
                     {
                        dropdown.Set(dependencies[0]);
                     }
                  });
                  dropdown.Refresh();  
               }
               else
               {
                  dropdown.visible = false;
               }
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