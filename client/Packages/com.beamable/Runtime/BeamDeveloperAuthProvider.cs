namespace Beamable
{
	public interface IBeamDeveloperAuthProvider
	{
		string AccessToken { get; }
		string RefreshToken { get; }
	}
	public class BeamDeveloperAuthProvider : IBeamDeveloperAuthProvider
	{
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
	}
}
