// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

namespace Beamable.Common.Api
{
	public interface IUserContext
	{
		/// <summary>
		/// The current gamertag of this context
		/// </summary>
		long UserId { get; }
	}

	public class SimpleUserContext : IUserContext
	{
		public long UserId { get; }

		public SimpleUserContext(long userId)
		{
			UserId = userId;
		}
	}
}
