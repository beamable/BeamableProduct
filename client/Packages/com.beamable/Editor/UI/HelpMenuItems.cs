using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class HelpMenuItems
	{
		[MenuItem(
		   BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/" +
		   BeamableConstantsOLD.OPEN + " " +
		   BeamableConstantsOLD.BEAMABLE_MAIN_WEBSITE,
		   priority = BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		private static void OpenBeamableMainWebsite()
		{
			Application.OpenURL(BeamableConstantsOLD.URL_BEAMABLE_MAIN_WEBSITE);
		}

		[MenuItem(
		   BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/" +
		   BeamableConstantsOLD.OPEN + " " +
		   BeamableConstantsOLD.BEAMABLE_DOCS_WEBSITE,
		   priority = BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		private static void OpenBeamableDocsWebsite()
		{
			Application.OpenURL(BeamableConstantsOLD.URL_BEAMABLE_DOCS_WEBSITE);
		}
	}
}
