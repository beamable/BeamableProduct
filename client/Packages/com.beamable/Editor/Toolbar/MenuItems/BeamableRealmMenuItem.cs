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

			
			if (editor.BeamCli?.latestRealms != null)
			{
				foreach (var proj in editor.BeamCli.latestRealms)
				{
					if (proj.Archived) continue;
					if (proj.GamePid != editor.BeamCli?.ProductionRealm.Pid) continue;

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
			var sameEditorAndBuildPids = buildPid == editor.BeamCli?.Pid;
			foreach (var proj in projects)
			{
				var enabled = proj.Pid == editor.BeamCli.Pid;
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
					throw new NotImplementedException();
					// var _ = editor.EditorAccount.UpdateRealms(editor.Requester);
				});
				if(!sameEditorAndBuildPids)
				{
					menu.AddItem(new GUIContent(rootDisplay.text + "/Save to config-defaults"), false, editor.WriteConfig);
				}
			}
		}

		public override GUIContent RenderLabel(BeamEditorContext beamableApi)
		{
			var realmName = beamableApi?.BeamCli?.CurrentRealm.DisplayName;
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
