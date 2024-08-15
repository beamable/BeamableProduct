using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	struct CliWindowToolAction
	{
		public Action onClick;
		public string name;
	}
	
	public partial class BeamCliWindow
	{
		
		void DrawTools(params CliWindowToolAction[] toolActions)
		{
			GUILayout.BeginHorizontal();
			{
				for (var i = 0; i < toolActions.Length; i++)
				{
					var tool = toolActions[i];
					if (GUILayout.Button(tool.name, EditorStyles.miniButton))
					{
						// delay the action so that if an exception occurs in callback, GUI events will still be closed.
						delayedActions.Add(() =>
						{
							tool.onClick();
						});
					}
				}
			}
			GUILayout.EndHorizontal();
		}
	}
	
	
}
