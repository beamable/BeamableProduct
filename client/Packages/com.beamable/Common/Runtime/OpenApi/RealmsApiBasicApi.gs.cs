
namespace Beamable.Api.Open.Realms
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface IRealmsApiBasicApi
    {
        Promise<CommonResponse> PostProjectBeamable(CreateProjectRequest gsReq);
        Promise<AliasAvailableResponse> GetCustomerAliasAvailable(string alias, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<ProjectView> GetProject([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<CommonResponse> PostProject(CreateProjectRequest gsReq);
        Promise<CommonResponse> PutProject(UnarchiveProjectRequest gsReq);
        Promise<CommonResponse> DeleteProject(ArchiveProjectRequest gsReq);
        Promise<GetGameResponse> GetGames();
        Promise<RealmConfigResponse> GetConfig();
        Promise<CommonResponse> PutConfig(RealmConfigSaveRequest gsReq);
        Promise<CommonResponse> PutProjectRename(RenameProjectRequest gsReq);
        Promise<ServicePlansResponse> GetPlans();
        Promise<CommonResponse> PostPlans(CreatePlanRequest gsReq);
        Promise<CustomerViewResponse> GetCustomer();
        Promise<NewCustomerResponse> PostCustomer(NewCustomerRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<LaunchMessageListResponse> GetLaunchMessage();
        Promise<CommonResponse> PostLaunchMessage(CreateLaunchMessageRequest gsReq);
        Promise<CommonResponse> DeleteLaunchMessage(RemoveLaunchMessageRequest gsReq);
        Promise<EmptyResponse> GetIsCustomer([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<CustomerResponse> GetAdminCustomer();
        Promise<GetGameResponse> GetGame(string rootPID);
        Promise<CommonResponse> PostGame(NewGameRequest gsReq);
        Promise<CommonResponse> PutGame(UpdateGameHierarchyRequest gsReq);
        Promise<PromoteRealmResponseOld> GetProjectPromote(string sourcePid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> promotions, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> contentManifestIds);
        Promise<PromoteRealmResponseOld> PostProjectPromote(PromoteRealmRequest gsReq);
        Promise<CustomersResponse> GetCustomers([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<PromoteRealmResponse> GetPromotion(string sourcePid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> promotions, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> contentManifestIds);
        Promise<PromoteRealmResponse> PostPromotion(PromoteRealmRequest gsReq);
    }
    public class RealmsApiBasicApi : IRealmsApiBasicApi
    {
        private IBeamableRequester _requester;
        public RealmsApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<CommonResponse> PostProjectBeamable(CreateProjectRequest gsReq)
        {
            string gsUrl = "/basic/realms/project/beamable";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<AliasAvailableResponse> GetCustomerAliasAvailable(string alias, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/realms/customer/alias/available";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("alias=", alias.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<AliasAvailableResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<AliasAvailableResponse>);
        }
        public virtual Promise<ProjectView> GetProject([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/realms/project";
            // make the request and return the result
            return _requester.Request<ProjectView>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<ProjectView>);
        }
        public virtual Promise<CommonResponse> PostProject(CreateProjectRequest gsReq)
        {
            string gsUrl = "/basic/realms/project";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutProject(UnarchiveProjectRequest gsReq)
        {
            string gsUrl = "/basic/realms/project";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> DeleteProject(ArchiveProjectRequest gsReq)
        {
            string gsUrl = "/basic/realms/project";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<GetGameResponse> GetGames()
        {
            string gsUrl = "/basic/realms/games";
            // make the request and return the result
            return _requester.Request<GetGameResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetGameResponse>);
        }
        public virtual Promise<RealmConfigResponse> GetConfig()
        {
            string gsUrl = "/basic/realms/config";
            // make the request and return the result
            return _requester.Request<RealmConfigResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<RealmConfigResponse>);
        }
        public virtual Promise<CommonResponse> PutConfig(RealmConfigSaveRequest gsReq)
        {
            string gsUrl = "/basic/realms/config";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutProjectRename(RenameProjectRequest gsReq)
        {
            string gsUrl = "/basic/realms/project/rename";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<ServicePlansResponse> GetPlans()
        {
            string gsUrl = "/basic/realms/plans";
            // make the request and return the result
            return _requester.Request<ServicePlansResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ServicePlansResponse>);
        }
        public virtual Promise<CommonResponse> PostPlans(CreatePlanRequest gsReq)
        {
            string gsUrl = "/basic/realms/plans";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CustomerViewResponse> GetCustomer()
        {
            string gsUrl = "/basic/realms/customer";
            // make the request and return the result
            return _requester.Request<CustomerViewResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CustomerViewResponse>);
        }
        public virtual Promise<NewCustomerResponse> PostCustomer(NewCustomerRequest gsReq, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/realms/customer";
            // make the request and return the result
            return _requester.Request<NewCustomerResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<NewCustomerResponse>);
        }
        public virtual Promise<LaunchMessageListResponse> GetLaunchMessage()
        {
            string gsUrl = "/basic/realms/launch-message";
            // make the request and return the result
            return _requester.Request<LaunchMessageListResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<LaunchMessageListResponse>);
        }
        public virtual Promise<CommonResponse> PostLaunchMessage(CreateLaunchMessageRequest gsReq)
        {
            string gsUrl = "/basic/realms/launch-message";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> DeleteLaunchMessage(RemoveLaunchMessageRequest gsReq)
        {
            string gsUrl = "/basic/realms/launch-message";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<EmptyResponse> GetIsCustomer([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/realms/is-customer";
            // make the request and return the result
            return _requester.Request<EmptyResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<EmptyResponse>);
        }
        public virtual Promise<CustomerResponse> GetAdminCustomer()
        {
            string gsUrl = "/basic/realms/admin/customer";
            // make the request and return the result
            return _requester.Request<CustomerResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<CustomerResponse>);
        }
        public virtual Promise<GetGameResponse> GetGame(string rootPID)
        {
            string gsUrl = "/basic/realms/game";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("rootPID=", rootPID.ToString()));
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetGameResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetGameResponse>);
        }
        public virtual Promise<CommonResponse> PostGame(NewGameRequest gsReq)
        {
            string gsUrl = "/basic/realms/game";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PutGame(UpdateGameHierarchyRequest gsReq)
        {
            string gsUrl = "/basic/realms/game";
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<PromoteRealmResponseOld> GetProjectPromote(string sourcePid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> promotions, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> contentManifestIds)
        {
            string gsUrl = "/basic/realms/project/promote";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("sourcePid=", sourcePid.ToString()));
            if (((promotions != default(OptionalStringArray)) 
                        && promotions.HasValue))
            {
                gsQueries.Add(string.Concat("promotions=", promotions.ToString()));
            }
            if (((contentManifestIds != default(OptionalStringArray)) 
                        && contentManifestIds.HasValue))
            {
                gsQueries.Add(string.Concat("contentManifestIds=", contentManifestIds.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<PromoteRealmResponseOld>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<PromoteRealmResponseOld>);
        }
        public virtual Promise<PromoteRealmResponseOld> PostProjectPromote(PromoteRealmRequest gsReq)
        {
            string gsUrl = "/basic/realms/project/promote";
            // make the request and return the result
            return _requester.Request<PromoteRealmResponseOld>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<PromoteRealmResponseOld>);
        }
        public virtual Promise<CustomersResponse> GetCustomers([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/realms/customers";
            // make the request and return the result
            return _requester.Request<CustomersResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<CustomersResponse>);
        }
        public virtual Promise<PromoteRealmResponse> GetPromotion(string sourcePid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> promotions, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string[]> contentManifestIds)
        {
            string gsUrl = "/basic/realms/promotion";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("sourcePid=", sourcePid.ToString()));
            if (((promotions != default(OptionalStringArray)) 
                        && promotions.HasValue))
            {
                gsQueries.Add(string.Concat("promotions=", promotions.ToString()));
            }
            if (((contentManifestIds != default(OptionalStringArray)) 
                        && contentManifestIds.HasValue))
            {
                gsQueries.Add(string.Concat("contentManifestIds=", contentManifestIds.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<PromoteRealmResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<PromoteRealmResponse>);
        }
        public virtual Promise<PromoteRealmResponse> PostPromotion(PromoteRealmRequest gsReq)
        {
            string gsUrl = "/basic/realms/promotion";
            // make the request and return the result
            return _requester.Request<PromoteRealmResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<PromoteRealmResponse>);
        }
    }
}
