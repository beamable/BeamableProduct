// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

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
