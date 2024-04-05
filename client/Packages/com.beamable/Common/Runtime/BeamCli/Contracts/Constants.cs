namespace Beamable.Common.BeamCli.Contracts
{
	public static class CliConstants
	{
		/// <summary>
		/// A magical constant that is used in the 'beam project generate-props` call to signify that the
		/// $(SolutionDir) should be set to the same place as the Directory.Build.Props file.
		/// </summary>
		public const string GENERATE_PROPS_SLN_NEXT_TO_PROPS = "DIR.PROPS";
	}
}
