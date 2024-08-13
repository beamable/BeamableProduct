
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamGetTokenDetailsCommandOutput
	{
		public bool wasRefreshToken;
		public Beamable.Common.Content.OptionalLong accountId;
		public long cid;
		public long created;
		public Beamable.Common.Content.OptionalString device;
		public Beamable.Common.Content.OptionalLong expiresMs;
		public Beamable.Common.Content.OptionalLong gamerTag;
		public Beamable.Common.Content.OptionalString pid;
		public Beamable.Common.Content.OptionalString platform;
		public Beamable.Common.Content.OptionalBool revoked;
		public Beamable.Common.Content.OptionalArrayOfString scopes;
		public string token;
		public string type;
	}
}
