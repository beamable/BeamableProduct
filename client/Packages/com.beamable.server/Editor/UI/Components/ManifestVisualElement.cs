using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Server.Editor.UI.Components
{
   public class ManifestVisualElement : BeamableVisualElement
   {
      public Action OnCancel;
      public Action<ManifestModel> OnSubmit;

      public ManifestModel Model { get; }

      public const string COMMON = Constants.SERVER_UI + "/Components/manifestVisualElement";

      public ManifestVisualElement(ManifestModel model) : base(COMMON)
      {
         Model = model;
      }

      public override void Refresh()
      {
         base.Refresh();

         var container = Root.Q<VisualElement>("container");

         var templateSizes = new []{"small", "medium", "large"};

         var cancelButton = Root.Q<Button>("cancelButton");
         cancelButton.clickable.clicked += () => OnCancel?.Invoke();

         var continueButton = Root.Q<Button>("continueButton");
         continueButton.clickable.clicked += () => OnSubmit?.Invoke(Model);

         var generalComments = Root.Q<TextField>("general-comment");
         generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

         foreach (var kvp in Model.Services)
         {
            var service = kvp.Key;
            var data = kvp.Value;

            var rowElem = new VisualElement();
            rowElem.AddToClassList("service");

            var label = new Label(service);
            label.AddToClassList("serviceName");

            var enabledToggle = new Toggle();
            enabledToggle.AddToClassList("enabled");
            enabledToggle.SetValueWithoutNotify(data.Enabled);
            enabledToggle.RegisterValueChangedCallback(ce => { data.Enabled = ce.newValue; });


            var templateDropdown = new PopupField<string>(templateSizes.ToList(), 0);
            templateDropdown.AddToClassList("template");
            templateDropdown.SetValueWithoutNotify(data.TemplateId);
            templateDropdown.RegisterValueChangedCallback(ce => { data.TemplateId = ce.newValue;});

            var commentField = new TextField();
            commentField.AddToClassList("comment");
            commentField.value = data.Comment;
            commentField.RegisterValueChangedCallback(ce => data.Comment = ce.newValue);


            rowElem.Add(enabledToggle);
            rowElem.Add(label);
            rowElem.Add(templateDropdown);
            rowElem.Add(commentField);
            container.Add(rowElem);
         }
      }
   }

   public class ManifestModel
   {
      public Dictionary<string, ServiceReference> ServerManifest;

      public Dictionary<string, ManifestEntryModel> Services;
      public string Comment;
   }

   public class ManifestEntryModel
   {
      public string ServiceName;
      public string Comment;

      public bool Enabled
      {
         get => _enabled;
         set => SetEnabled(value);
      }

      public string TemplateId
      {
         get => _templateId;
         set => SetTemplateId(value);
      }

      private string _templateId;

      private void SetTemplateId(string templateId)
      {
         _templateId = templateId;
         var service = MicroserviceConfiguration.Instance.GetEntry(ServiceName);
         service.TemplateId = templateId;
      }

      private bool _enabled;
      private void SetEnabled(bool enabled)
      {
         _enabled = enabled;
         var service = MicroserviceConfiguration.Instance.GetEntry(ServiceName);
         service.Enabled = enabled;
      }
   }
}