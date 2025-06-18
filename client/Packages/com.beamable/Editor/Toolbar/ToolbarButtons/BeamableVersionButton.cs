#if UNITY_2022_1_OR_NEWER
using Beamable.Editor.UI.Common;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Directories;

namespace Beamable.Editor.ToolbarExtender
{
	public class BeamableVersionButton : BeamableBasicVisualElement
	{
		private VisualElement _icon;
		private VisualElement _badge;
		private TextElement _label;

		public BeamableVersionButton() :
			base(
				$"{BEAMABLE_PACKAGE_EDITOR_TOOLBAR}/{nameof(BeamableVersionButton)}/{nameof(BeamableVersionButton)}.uss",
				false)
		{
		}

		public override void Init()
		{
			base.Init();

			_icon = new VisualElement {name = "icon", style = {backgroundImage = new StyleBackground(GetSprite())}};
			Root.Add(_icon);

			_label = new TextElement {name = "label", text = GetVersion()};
			Root.Add(_label);

			_badge = new VisualElement {name = "badge"};
			Root.Add(_badge);
			RefreshIcon();

			Root.RegisterCallback<MouseDownEvent>(ButtonClicked);
			EditorApplication.update += RefreshIcon;
		}

		private void RefreshIcon()
		{
			BeamEditorContext ctx = BeamEditorContext.Default;

			_label.text = ctx.BeamCli.CurrentRealm?.DisplayName ?? "<no realm>";
			_badge?.ClearClassList();
			if (ctx.BeamCli.CurrentRealm?.IsProduction ?? false)
			{
				_badge?.AddToClassList("production");
			}
			if (ctx.BeamCli.CurrentRealm?.IsStaging ?? false)
			{
				_badge?.AddToClassList("staging");
			}
		}

		protected override void OnDestroy()
		{
			EditorApplication.update -= RefreshIcon;
			Root.UnregisterCallback<MouseDownEvent>(ButtonClicked);
		}

		private class MenuItemInfo
		{
			public MenuItem menuItem;
			public MethodBase method;
			public string path;
		}

		private void ButtonClicked(MouseDownEvent evt)
		{
			BeamEditorContext editorAPI = BeamEditorContext.Default;

			var menuItemsSearchInFolders = BeamEditor.CoreConfiguration.BeamableMenuItemsPath
			                                         .Where(Directory.Exists).ToArray();
			var menuItemsGuids = BeamableAssetDatabase.FindAssets<BeamableToolbarMenuItem>(menuItemsSearchInFolders);

			var assistantMenuItems = menuItemsGuids
			                         .Select(guid => AssetDatabase.LoadAssetAtPath<BeamableToolbarMenuItem>(
				                                 AssetDatabase.GUIDToAssetPath(guid))).ToList();
			assistantMenuItems.Sort((mi1, mi2) =>
			{
				var orderComp = mi1.Order.CompareTo(mi2.Order);
				var mi1Label = mi1.RenderLabel(editorAPI);
				var mi2Label = mi2.RenderLabel(editorAPI);
				var labelComp = 0;
				if (mi1Label != null && mi2Label != null)
				{
					labelComp = string.Compare(mi1Label.text, mi2Label.text,
					                           StringComparison.Ordinal);
				}

				return orderComp == 0 ? labelComp : orderComp;
			});

			var menu = new GenericMenu();
		
			
			assistantMenuItems.ForEach(item => item?.ContextualizeMenu(editorAPI, menu));
			assistantMenuItems
				.ForEach(item =>
				{
					var label = item.RenderLabel(editorAPI);
					if (label == null) return;
					menu.AddItem(label, false,
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
					"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small.png");

			return noHintsTexture;
		}
	}
}
#endif
