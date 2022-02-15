using UnityEngine;

namespace Beamable.Common.Constants
{
	public static partial class BeamableConstants
	{
		public static partial class Features
		{
			public static partial class Buss
			{
				public static partial class ThemeManager
				{
					public static readonly Vector2 THEME_MANAGER_WINDOW_SIZE = new Vector2(500, 300);
					public static readonly Vector2 ADD_STYLE_WINDOW_SIZE = new Vector2(520, 620);
				
					public const string ADD_STYLE_WINDOW_HEADER = "Add style window";

					public const string DELETE_STYLE_HEADER = "Delete style";
					public const string DELETE_STYLE_MESSAGE = "Are You sure You want to delete this style?";

					public const string CLEAR_ALL_PROPERTIES_HEADER = "Clear all properties";
					public const string CLEAR_ALL_PROPERTIES_MESSAGE = "Are You sure You want to clear all properties?";

					public const string NO_BUSS_STYLE_SHEET_AVAILABLE =
						"There should be created at least one Buss Style Sheet and it should be referenced by Buss Element present at scene";
				}
			}
		}
	}
}

// using static Beamable.Common.Constants.BeamableConstants.Features.Services;
