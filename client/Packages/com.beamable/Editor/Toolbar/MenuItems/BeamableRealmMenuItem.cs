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
			foreach (var proj in editor.CurrentCustomer.Projects)
			{
				if (proj.Archived) continue;
				if (proj.GamePid != editor.CurrentRealm?.GamePid) continue;

				projects.Add(proj);
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
				
			foreach (var proj in projects)
			{
				var enabled = proj.Pid == editor.CurrentRealm.Pid;
				
				menu.AddItem(new GUIContent(rootDisplay.text + "/" + proj.DisplayName), enabled, () =>
				{
					editor.SwitchRealm(proj);
				});
			}
			
			menu.AddSeparator(rootDisplay.text + "/");
			menu.AddItem(new GUIContent(rootDisplay.text + "/Refresh"), false, () =>
			{
				var _ = editor.EditorAccount.UpdateRealms(editor.Requester);
			});
			
		}

		public override GUIContent RenderLabel(BeamEditorContext beamableApi)
		{
			var realmName = beamableApi?.CurrentRealm?.DisplayName;
			if (string.IsNullOrEmpty(realmName))
			{
				return new GUIContent($"Realm: {realmName}");
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
