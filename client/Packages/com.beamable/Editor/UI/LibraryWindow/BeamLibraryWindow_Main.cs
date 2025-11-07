using Beamable.Common.Util;
using Beamable.Editor.Util;
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
				                          Application.OpenURL(DocsPageHelper.GetUnityDocsPageUrl("unity/samples/lightbeam/", EditorConstants.UNITY_CURRENT_DOCS_VERSION));
			                          });
		}
	}
}
