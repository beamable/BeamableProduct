#if UNITY_2022_1_OR_NEWER
using Beamable.Editor.Assistant;
using Beamable.Editor.UI.Common;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Directories;

namespace Beamable.Editor.ToolbarExtender
{
	public class BeamableVersionButton : BeamableBasicVisualElement
	{
		private VisualElement _icon;

		public BeamableVersionButton() :
			base(
				$"{BEAMABLE_PACKAGE_EDITOR_ASSISTANT}/{nameof(BeamableVersionButton)}/{nameof(BeamableVersionButton)}.uss",
				false)
		{
		}

		public override void Init()
		{
			base.Init();

			RefreshIcon();
			Root.Add(_icon);

			VisualElement label = new TextElement {name = "label", text = GetVersion()};
			Root.Add(label);

			Root.RegisterCallback<MouseDownEvent>(ButtonClicked);
			EditorApplication.update += RefreshIcon;
		}

		private void RefreshIcon()
		{
			_icon = new VisualElement
			{
				name = "icon", style = {backgroundImage = new StyleBackground(GetSprite())}
			};
		}

		protected override void OnDestroy()
		{
			EditorApplication.update -= RefreshIcon;
			Root.UnregisterCallback<MouseDownEvent>(ButtonClicked);
		}

		private void ButtonClicked(MouseDownEvent evt)
		{
			BeamEditorContext editorAPI = BeamEditorContext.Default;

			var menuItemsSearchInFolders = BeamEditor.CoreConfiguration.BeamableAssistantMenuItemsPath
			                                         .Where(Directory.Exists).ToArray();
			var menuItemsGuids = BeamableAssetDatabase.FindAssets<BeamableAssistantMenuItem>(menuItemsSearchInFolders);

			var assistantMenuItems = menuItemsGuids
			                         .Select(guid => AssetDatabase.LoadAssetAtPath<BeamableAssistantMenuItem>(
				                                 AssetDatabase.GUIDToAssetPath(guid))).ToList();
			assistantMenuItems.Sort((mi1, mi2) =>
			{
				var orderComp = mi1.Order.CompareTo(mi2.Order);
				var labelComp = string.Compare(mi1.RenderLabel(editorAPI).text, mi2.RenderLabel(editorAPI).text,
				                               StringComparison.Ordinal);

				return orderComp == 0 ? labelComp : orderComp;
			});

			var menu = new GenericMenu();

			assistantMenuItems
				.ForEach(item =>
				{
					menu.AddItem(item.RenderLabel(editorAPI), false,
					             data => item.OnItemClicked((BeamEditorContext)data), editorAPI);
				});

			menu.ShowAsContext();
		}

		private string GetVersion()
		{
			var version = BeamableEnvironment.SdkVersion;
			var versionStr = $"Beamable {version}";
			if (version.IsReleaseCandidate)
			{
				versionStr = $"Beamable {version.Major}.{version.Minor}.{version.Patch} RC{version.RC}";
			}

			if (version.IsNightly)
			{
				versionStr = $"BeamDev {version.NightlyTime}";
			}

			return versionStr;
		}

		public Sprite GetSprite()
		{
			var noHintsTexture =
				AssetDatabase.LoadAssetAtPath<Sprite>(
					"Packages/com.beamable/Editor/UI/BeamableAssistant/Icons/info.png");
			var hintsTexture =
				AssetDatabase.LoadAssetAtPath<Sprite>(
					"Packages/com.beamable/Editor/UI/BeamableAssistant/Icons/info hit.png");
			var validationTexture =
				AssetDatabase.LoadAssetAtPath<Sprite>(
					"Packages/com.beamable/Editor/UI/BeamableAssistant/Icons/info valu.png");

			var btnTexture = noHintsTexture;

			BeamHintNotificationManager notificationManager = null;
			BeamEditor.GetBeamHintSystem(ref notificationManager);
			if (notificationManager != null && notificationManager.PendingHintNotifications.Any())
				btnTexture = hintsTexture;

			if (notificationManager != null && notificationManager.PendingValidationNotifications.Any())
				btnTexture = validationTexture;

			return btnTexture;
		}
	}
}
#endif
