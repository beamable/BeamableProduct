using Beamable.Common.Api.Realms;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "SelectRealmMenuItem", menuName = "Beamable/Toolbar/Menu Items/Realm Picker", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamableRealmMenuItem : BeamableToolbarMenuItem
	{
	
		public override void ContextualizeMenu(BeamEditorContext editor, GenericMenu menu)
		{
			var rootDisplay = RenderLabel(editor);

			var projects = new List<RealmView>();
			
			if (editor.CurrentCustomer?.Projects != null)
			{
				foreach (var proj in editor.CurrentCustomer.Projects)
				{
					if (proj.Archived) continue;
					if (proj.GamePid != editor.CurrentRealm?.GamePid) continue;

					projects.Add(proj);
				}
			}

			projects.Sort((a, b) =>
			{
				var depthCompare = a.Depth.CompareTo(b.Depth);
				if (depthCompare != 0 && (a.Depth <= 1 || b.Depth <= 1))
				{
					return depthCompare;
				}
				
				return String.Compare(a.ProjectName, b.ProjectName, StringComparison.Ordinal);
			});
			var buildPid = editor.ServiceScope.GetService<ConfigDefaultsService>().Pid;
			var buildName = buildPid.ToString();
			var sameEditorAndBuildPids = buildPid == editor.CurrentRealm?.Pid;
			foreach (var proj in projects)
			{
				var enabled = proj.Pid == editor.CurrentRealm.Pid;
				var display = !sameEditorAndBuildPids && buildPid == proj.Pid ? $"{proj.ProjectName} [build]" : proj.ProjectName;
				if (buildPid == proj.Pid)
				{
					buildName = proj.ProjectName;
				}
				menu.AddItem(new GUIContent(rootDisplay.text + "/" + display), enabled, () =>
				{
					editor.SwitchRealm(proj);
				});
			}

			if (projects.Count > 0)
			{
				menu.AddSeparator(rootDisplay.text + "/");
				menu.AddItem(new GUIContent(rootDisplay.text + "/Refresh"), false, () =>
				{
					var _ = editor.EditorAccount.UpdateRealms(editor.Requester);
				});
				if(!sameEditorAndBuildPids)
				{
					menu.AddDisabledItem(new GUIContent($"{rootDisplay.text}/Editor is on realm {editor.CurrentRealm?.DisplayName}, but the build will use the {buildName}."));
					menu.AddDisabledItem(new GUIContent($"{rootDisplay.text}/Calling `Save` would update the config-defaults file which is used in builds."));
					menu.AddItem(new GUIContent(rootDisplay.text + "/Save to config-defaults"), false, editor.WriteConfig);
				}
			}
		}

		public override GUIContent RenderLabel(BeamEditorContext beamableApi)
		{
			var realmName = beamableApi?.CurrentRealm?.DisplayName;
			if (string.IsNullOrEmpty(realmName))
			{
				return null;
			}
			else
			{
				return new GUIContent("Select Realm");
			}
		}

		public override void OnItemClicked(BeamEditorContext beamableApi)
		{
			
		}
	}
}
