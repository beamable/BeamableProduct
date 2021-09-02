using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
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
      public ManifestEntryModel Model { get; set; }

      public PublishManifestEntryVisualElement() : base(nameof(PublishManifestEntryVisualElement))
      {
      }

      public override void Refresh()
      {
         base.Refresh();

         var checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
         checkbox.Refresh();
         checkbox.SetWithoutNotify(Model.Enabled);
         checkbox.OnValueChanged += b => Model.Enabled = b;

         var templateDropdown = new PopupField<string>(TemplateSizes.ToList(), 0);
         templateDropdown.AddToClassList("template");
         templateDropdown.SetValueWithoutNotify(Model.TemplateId);
         templateDropdown.RegisterValueChangedCallback(ce => { Model.TemplateId = ce.newValue;});
         Root.Q<VisualElement>("SizeC").Add(templateDropdown);

         var nameLabel = Root.Q<Label>("nameMS");
         nameLabel.text = Model.ServiceName;

         var commentField = Root.Q<TextField>("commentsText");
         commentField.value = Model.Comment;
         commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);
      }
      }
   }