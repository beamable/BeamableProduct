// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 


namespace Beamable.Api.Autogenerated.Party
{
    using Beamable.Api.Autogenerated.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    using Beamable.Common.Dependencies;
    
    public partial interface IBeamPartyApi
    {
        /// <summary>
        /// Create a party for the current player.
        /// 
        /// POST call to `/api/parties` endpoint.
        /// </summary>
        /// <param name="gsReq">The <see cref="CreateParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        Promise<Party> PostApiParties(CreateParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Updates party state.
        /// 
        /// PUT call to `/api/parties/{id}/metadata` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="UpdateParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        Promise<Party> PutMetadata(System.Guid id, UpdateParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Return the status of a party.
        /// 
        /// GET call to `/api/parties/{id}` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        Promise<Party> Get(System.Guid id, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Join a party
        /// 
        /// PUT call to `/api/parties/{id}` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        Promise<Party> Put(System.Guid id, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Promote a party member to leader.
        /// 
        /// PUT call to `/api/parties/{id}/promote` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="PromoteNewLeader"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        Promise<Party> PutPromote(System.Guid id, PromoteNewLeader gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Invite a player to a party
        /// 
        /// POST call to `/api/parties/{id}/invite` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="InviteToParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesInvitePostPartyResponse"/></returns>
        Promise<ApiPartiesInvitePostPartyResponse> PostInvite(System.Guid id, InviteToParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Cancel party invitation.
        /// 
        /// DELETE call to `/api/parties/{id}/invite` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="CancelInviteToParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesInviteDeletePartyResponse"/></returns>
        Promise<ApiPartiesInviteDeletePartyResponse> DeleteInvite(System.Guid id, CancelInviteToParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        /// <summary>
        /// Remove the requested player from the party. The leader is able to remove anyone. Others may
        ///only remove themselves without error.
        /// 
        /// DELETE call to `/api/parties/{id}/members` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="LeaveParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesMembersDeletePartyResponse"/></returns>
        Promise<ApiPartiesMembersDeletePartyResponse> DeleteMembers(System.Guid id, LeaveParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
    }
    public partial class BeamPartyApi : IBeamPartyApi
    {
        /// <summary>
        /// Create a party for the current player.
        /// 
        /// POST call to `/api/parties` endpoint.
        /// </summary>
        /// <param name="gsReq">The <see cref="CreateParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        public virtual Promise<Party> PostApiParties(CreateParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties";
            // make the request and return the result
            return _requester.Request<Party>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<Party>);
        }
        /// <summary>
        /// Updates party state.
        /// 
        /// PUT call to `/api/parties/{id}/metadata` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="UpdateParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        public virtual Promise<Party> PutMetadata(System.Guid id, UpdateParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}/metadata";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<Party>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<Party>);
        }
        /// <summary>
        /// Return the status of a party.
        /// 
        /// GET call to `/api/parties/{id}` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        public virtual Promise<Party> Get(System.Guid id, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<Party>(Method.GET, gsUrl, default(object), includeAuthHeader, this.Serialize<Party>);
        }
        /// <summary>
        /// Join a party
        /// 
        /// PUT call to `/api/parties/{id}` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        public virtual Promise<Party> Put(System.Guid id, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<Party>(Method.PUT, gsUrl, default(object), includeAuthHeader, this.Serialize<Party>);
        }
        /// <summary>
        /// Promote a party member to leader.
        /// 
        /// PUT call to `/api/parties/{id}/promote` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="PromoteNewLeader"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="Party"/></returns>
        public virtual Promise<Party> PutPromote(System.Guid id, PromoteNewLeader gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}/promote";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<Party>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<Party>);
        }
        /// <summary>
        /// Invite a player to a party
        /// 
        /// POST call to `/api/parties/{id}/invite` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="InviteToParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesInvitePostPartyResponse"/></returns>
        public virtual Promise<ApiPartiesInvitePostPartyResponse> PostInvite(System.Guid id, InviteToParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}/invite";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<ApiPartiesInvitePostPartyResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<ApiPartiesInvitePostPartyResponse>);
        }
        /// <summary>
        /// Cancel party invitation.
        /// 
        /// DELETE call to `/api/parties/{id}/invite` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="CancelInviteToParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesInviteDeletePartyResponse"/></returns>
        public virtual Promise<ApiPartiesInviteDeletePartyResponse> DeleteInvite(System.Guid id, CancelInviteToParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}/invite";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<ApiPartiesInviteDeletePartyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<ApiPartiesInviteDeletePartyResponse>);
        }
        /// <summary>
        /// Remove the requested player from the party. The leader is able to remove anyone. Others may
        ///only remove themselves without error.
        /// 
        /// DELETE call to `/api/parties/{id}/members` endpoint.
        /// </summary>
        /// <param name="id">Id of the party</param>
        /// <param name="gsReq">The <see cref="LeaveParty"/> instance to use for the request</param>
        /// <param name="includeAuthHeader">By default, every request will include an authorization header so that the request acts on behalf of the current user. When the includeAuthHeader argument is false, the request will not include the authorization header for the current user.</param>
        /// <returns>A promise containing the <see cref="ApiPartiesMembersDeletePartyResponse"/></returns>
        public virtual Promise<ApiPartiesMembersDeletePartyResponse> DeleteMembers(System.Guid id, LeaveParty gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/api/parties/{id}/members";
            gsUrl = gsUrl.Replace("{id}", _requester.EscapeURL(id.ToString()));
            // make the request and return the result
            return _requester.Request<ApiPartiesMembersDeletePartyResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, this.Serialize<ApiPartiesMembersDeletePartyResponse>);
        }
    }
}
