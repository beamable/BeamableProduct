using Beamable.Editor.Util;
using System;
using UnityEditor;
using UnityEngine;

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
			                          }, 
			                          onClickedRefresh: () =>
			                          {
				                          library.Reload();
			                          },
			                          onClickedHelp: () =>
			                          {
				                          Application.OpenURL("https://docs.beamable.com/v2.0.0/docs/lightbeam-code-overview");
			                          });
		}
	}
}
