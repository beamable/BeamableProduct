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
	public enum ServicePublishState	
	{
		Unpublished,
		InProgress,
		Failed,
		Published,
	}
	
   public class PublishManifestEntryVisualElement : MicroserviceComponent
   {
      private static readonly string[] TemplateSizes = {"small", "medium", "large"};

      private const string MICROSERVICE_IMAGE_CLASS = "microserviceImage";
      private const string STORAGE_IMAGE_CLASS = "storageImage";
      
      private Dictionary<ServicePublishState, string> _checkImageClasses = new Dictionary<ServicePublishState, string>()
      {
	      {ServicePublishState.Unpublished, "unpublished"},
	      {ServicePublishState.Published, "published"},
	      {ServicePublishState.InProgress, "publish-inProgress"},
	      {ServicePublishState.Failed, "publish-failed"},
      };

      public IEntryModel Model { get; }
      
      public ILoadingBar LoadingBar	
      {
	      get
	      {
		      _loadingBar.Hidden = false;
		      return _loadingBar;
	      }
      }

      private bool wasPublished;
      private bool oddRow;
      
      private Image _checkImage;
      private LoadingBarElement _loadingBar;
      private string _currentPublishState;

      public PublishManifestEntryVisualElement(IEntryModel model, bool argWasPublished, bool isOddRow) : base(nameof(PublishManifestEntryVisualElement))
      {
         Model = model;
         wasPublished = argWasPublished;
         oddRow = isOddRow;
      }

      public override void Refresh()
      {
         base.Refresh();
         
         _loadingBar = Root.Q<LoadingBarElement>();
         _loadingBar.SmallBar = true;
         _loadingBar.Hidden = true;
         _loadingBar.Refresh();

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
         _checkImage = Root.Q<Image>("checkImage");

         if (Model is ManifestEntryModel serviceModel)
         {
            icon.AddToClassList(MICROSERVICE_IMAGE_CLASS);

            var microserviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(serviceModel.Name);

            if (microserviceModel != null && microserviceModel.Dependencies != null)
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

         UpdateStatus(wasPublished ? ServicePublishState.Published : ServicePublishState.Unpublished);

         if (oddRow)
         {
	         Root.Q<VisualElement>("mainContainer").AddToClassList("oddRow");
         }
      }
      
      public void UpdateStatus(ServicePublishState state)
      {
	      if (state == ServicePublishState.Failed)
	      {
		      _loadingBar.UpdateProgress(0, failed: true);
		      return;
	      }
	      
	      _checkImage.RemoveFromClassList(_currentPublishState);
	      _currentPublishState = _checkImageClasses[state];
	      _checkImage.AddToClassList(_currentPublishState);
      }
   }
}
