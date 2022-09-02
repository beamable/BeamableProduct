
namespace Beamable.Api.Open.Social
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class SocialApiBasicApi
    {
        private IBeamableRequester _requester;
        public SocialApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<Social> GetMy()
        {
            string gsUrl = "/basic/social/my";
            // make the request and return the result
            return _requester.Request<Social>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<Social>);
        }
        public virtual Promise<EmptyResponse> PostFriendsInvite(SendFriendRequest gsReq)
        {
            string gsUrl = "/basic/social/friends/invite";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> DeleteFriendsInvite(SendFriendRequest gsReq)
        {
            string gsUrl = "/basic/social/friends/invite";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> DeleteFriends(PlayerIdRequest gsReq)
        {
            string gsUrl = "/basic/social/friends";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<EmptyResponse> PostFriendsImport(ImportFriendsRequest gsReq)
        {
            string gsUrl = "/basic/social/friends/import";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<CommonResponse> PostFriendsMake(MakeFriendshipRequest gsReq)
        {
            string gsUrl = "/basic/social/friends/make";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<GetSocialStatusesResponse> Get(string[] playerIds)
        {
            string gsUrl = "/basic/social/";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("playerIds=", playerIds.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetSocialStatusesResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetSocialStatusesResponse>);
        }
        public virtual Promise<FriendshipStatus> PostBlocked(PlayerIdRequest gsReq)
        {
            string gsUrl = "/basic/social/blocked";
            // make the request and return the result
            return _requester.Request<FriendshipStatus>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<FriendshipStatus>);
        }
        public virtual Promise<FriendshipStatus> DeleteBlocked(PlayerIdRequest gsReq)
        {
            string gsUrl = "/basic/social/blocked";
            // make the request and return the result
            return _requester.Request<FriendshipStatus>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<FriendshipStatus>);
        }
    }
}
