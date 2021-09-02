namespace Beamable.Common.Api.Social
{
   public interface ISocialApi : IHasBeamableRequester
   {
      Promise<SocialList> Get();
      Promise<EmptyResponse> ImportFriends(SocialThirdParty source, string token);
      Promise<FriendStatus> BlockPlayer(long gamerTag);
      Promise<FriendStatus> UnblockPlayer(long gamerTag);
      Promise<EmptyResponse> RemoveFriend(long gamerTag);
      Promise<SocialList> RefreshSocialList();
   }
}
