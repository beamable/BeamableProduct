// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System;
using System.IO;

namespace Beamable.Common.Util
{
	public partial class BeamUtil
	{
		private static readonly char[] InvalidFileChars = Path.GetInvalidFileNameChars();

		/// <summary>
		/// replaces all invalid file name characters with a dash
		/// </summary>
		public static string SanitizeStringForPath(string str)
		{
			if (string.IsNullOrEmpty(str)) return str;

			foreach (var invalidChar in InvalidFileChars)
			{
				str = str.Replace(invalidChar, '-');
			}

			return str;
		}
	}
}
