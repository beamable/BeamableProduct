// unset

using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using System;
using System.IO;
using UnityEngine.UIElements;

namespace Beamable.Editor.Environment
{
	public class BeamableEnvironmentOverridesVisualElement : BeamableBasicVisualElement
	{
		private TextField _environmentField;
		private TextField _apiUrlField;
		private TextField _portalUrlField;
		private TextField _beamMongoExpressUrlField;
		private TextField _dockerRegistryUrl;
		private Toggle _isUnityVspField;
		private TextField _sdkVersionField;
		
		public const string EnvDevPath = "Packages/com.beamable/Runtime/Environment/Resources/env-dev.json";
		public const string EnvStagingPath = "Packages/com.beamable/Runtime/Environment/Resources/env-staging.json";
		public const string EnvProdPath = "Packages/com.beamable/Runtime/Environment/Resources/env-prod.json";

		public BeamableEnvironmentOverridesVisualElement() : base(
		                                                           $"{Constants.Directories.BEAMABLE_PACKAGE_EDITOR}/Environment/BeamableEnvironmentOverrides/BeamableEnvironmentOverridesVisualElement.uss",
		                                                           false) { }

		public override void Init()
		{
			base.Init();

			var buttonsContainer = new VisualElement();
			buttonsContainer.AddToClassList("buttonsContainer");
			buttonsContainer.AddToClassList("topContainer");
			Root.Add(buttonsContainer);
			
			AddButton("Dev", buttonsContainer, LoadDevData);
			AddButton("Staging", buttonsContainer, LoadStagingData);
			AddButton("Prod", buttonsContainer, LoadProdData);

			_environmentField = AddTextField("Environment", BeamableEnvironment.Environment, Root);
			_apiUrlField = AddTextField("API URL", BeamableEnvironment.ApiUrl, Root);
			_portalUrlField = AddTextField("Portal URL", BeamableEnvironment.PortalUrl, Root);
			_beamMongoExpressUrlField = AddTextField("Beam Mongo URL", BeamableEnvironment.BeamMongoExpressUrl, Root);
			_dockerRegistryUrl = AddTextField("Docker Registry URL", BeamableEnvironment.DockerRegistryUrl, Root);
			_isUnityVspField = AddToggle("Is Unity Vsp", BeamableEnvironment.Data.IsUnityVsp, Root);
			_sdkVersionField = AddTextField("SDK Version", BeamableEnvironment.SdkVersion.ToString(), Root);

			var bottomButtonsContainer = new VisualElement();
			buttonsContainer.AddToClassList("buttonsContainer");
			Root.Add(bottomButtonsContainer);
			
			bottomButtonsContainer.
		}

		private TextField AddTextField(string label, string text, VisualElement parent)
		{
			var field = new TextField(label);
			field.AddToClassList("overrideField");
			field.SetValueWithoutNotify(text);
			parent.Add(field);
			AddCurrentValueLabel(text, parent);
			return field;
		}

		private Toggle AddToggle(string label, bool value, VisualElement parent)
		{
			var field = new Toggle(label);
			field.AddToClassList("overrideField");
			field.SetValueWithoutNotify(value);
			parent.Add(field);
			AddCurrentValueLabel(value.ToString(), parent);
			return field;
		}

		private void AddCurrentValueLabel(string text, VisualElement parent)
		{
			var defaultValueLabel = new Label($"Current value: {text}");
			defaultValueLabel.AddToClassList("defaultValueLabel");
			parent.Add(defaultValueLabel);
		}

		private Button AddButton(string text, VisualElement parent, Action onClick)
		{
			var button = new Button(onClick);
			var label = new Label(text);
			button.Add(label);
			parent.Add(button);
			return button;
		}

		private void LoadEnvironmentData(EnvironmentData data)
		{
			_environmentField.SetValueWithoutNotify(data.Environment);
			_apiUrlField.SetValueWithoutNotify(data.ApiUrl);
			_portalUrlField.SetValueWithoutNotify(data.PortalUrl);
			_beamMongoExpressUrlField.SetValueWithoutNotify(data.DockerRegistryUrl);
			_dockerRegistryUrl.SetValueWithoutNotify(data.DockerRegistryUrl);
			_isUnityVspField.SetValueWithoutNotify(data.IsUnityVsp);
			_sdkVersionField.SetValueWithoutNotify(data.SdkVersion.ToString());
		}

		private void LoadDataFromFile(string path)
		{
			string envText  = File.ReadAllText(path);
			// var rawDict = Json.Deserialize(envText) as ArrayDict;
			var data = JsonSerializable.FromJson<EnvironmentData>(envText);
			LoadEnvironmentData(data);
		}

		private void LoadDevData() => LoadDataFromFile(EnvDevPath);
		private void LoadStagingData() => LoadDataFromFile(EnvStagingPath);
		private void LoadProdData() => LoadDataFromFile(EnvProdPath);
	}
}
