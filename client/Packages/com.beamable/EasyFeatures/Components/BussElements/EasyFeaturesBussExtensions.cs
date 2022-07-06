using Beamable.UI.Buss;
using System.Collections.Generic;

namespace EasyFeatures.Components
{
	public static class EasyFeaturesBussExtensions
	{
		public static void SetButtonPrimary(this BussElement element)
		{
			element.UpdateClasses(new List<string> {"button", "primary"});
		}
		
		public static void SetButtonDisabled(this BussElement element)
		{
			element.UpdateClasses(new List<string> {"button", "disable"});
		}

		public static void SetSelected(this BussElement element, bool value)
		{
			if (value)
			{
				element.AddClass("selected");
			}
			else
			{
				element.RemoveClass("selected");
			}
		}
	}
}
