
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamGetTokenForGuestCommandOutput
	{
		public Beamable.Common.Content.OptionalString accessToken;
		public Beamable.Common.Content.OptionalString challengeToken;
		public long expiresIn;
		public Beamable.Common.Content.OptionalString refreshToken;
		public Beamable.Common.Content.OptionalArrayOfString scopes;
		public string tokenType;
	}
}
