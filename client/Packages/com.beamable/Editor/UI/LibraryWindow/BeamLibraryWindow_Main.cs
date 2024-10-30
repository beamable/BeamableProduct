using Beamable.Editor.Util;
using System;
using UnityEditor;

namespace Beamable.Editor.Library
{
	public partial class BeamLibraryWindow
	{
		void DrawMain()
		{
			DrawHeader();
			DrawSampleSection();
		}

		void DrawHeader()
		{
			BeamGUI.DrawHeaderSection(this, ActiveContext,
			                          drawTopBarGui: () =>
			                          {
				                          // there aren't really any options that make sense yet.
			                          }, 
			                          drawLowBarGui: () =>
			                          {
				                          EditorGUILayout.Space(4, false);
				                          BeamGUI.LayoutRealmDropdown(this, ActiveContext);
				                          EditorGUILayout.Space(4, false);
			                          }, 
			                          onClickedRefresh: () =>
			                          {
				                          library.Reload();
			                          },
			                          onClickedHelp: () =>
			                          {
				                          throw new NotImplementedException("go to docs");
			                          });
		}
	}
}
