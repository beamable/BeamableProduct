namespace Beamable.Common.Util
{
	public class DocsPageHelper
	{
		private const string BaseUrl = "https://help.beamable.com/CLI-";
		private const string CliVersion = "6.1";

		public static string GetCliFullUrl(string subpage)
		{
			return $"{BaseUrl}{CliVersion}/{subpage}";
		}
		
	}
}
