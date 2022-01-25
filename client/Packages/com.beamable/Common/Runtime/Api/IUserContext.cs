namespace Beamable.Common.Api
{
   public interface IUserContext
   {
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
