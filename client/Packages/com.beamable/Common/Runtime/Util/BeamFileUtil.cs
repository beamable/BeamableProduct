using System;
using System.IO;

namespace Beamable.Common.Util
{
	public partial class BeamUtil
	{
		/// <summary>
		/// replaces all invalid file name characters with a dash
		/// </summary>
		public static string SanitizeStringForPath(string str)
		{
			if (str == null) return null;
			if (str == String.Empty) return String.Empty;
			
			var invalidChars = Path.GetInvalidFileNameChars();
			foreach (var invalidChar in invalidChars)
			{
				str = str.Replace(invalidChar, '-');
			}

			return str;
		}
	}
}
