
namespace Beamable.Api.Open.Commerce
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface ICommerceApiObjectApi
    {
        Promise<GetActiveOffersResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> scope);
        Promise<GetTotalCouponResponse> GetCouponsCount(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<CommonResponse> PutListingsCooldown(string objectId, CooldownModifierRequest gsReq);
        Promise<GetActiveOffersResponse> GetOffersAdmin(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> language, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stores);
        Promise<CommonResponse> PostPurchase(string objectId, PurchaseRequest gsReq);
        Promise<ResultResponse> PutPurchase(string objectId, ReportPurchaseRequest gsReq);
        Promise<ActiveListingResponse> GetListings(string listing, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> store, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time);
        Promise<CommonResponse> DeleteStatus(string objectId, ClearStatusRequest gsReq);
        Promise<CommonResponse> PostCoupons(string objectId, GiveCouponReq gsReq);
        Promise<CommonResponse> PostStatsUpdate(string objectId, StatSubscriptionNotification gsReq);
        Promise<GetActiveOffersResponse> GetOffers(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> language, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stores);
    }
    public class CommerceApiObjectApi : ICommerceApiObjectApi
    {
        private IBeamableRequester _requester;
        public CommerceApiObjectApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<GetActiveOffersResponse> Get(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> scope)
        {
            string gsUrl = "/object/commerce/{objectId}/";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((scope != default(OptionalString)) 
                        && scope.HasValue))
            {
                gsQueries.Add(string.Concat("scope=", scope.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetActiveOffersResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetActiveOffersResponse>);
        }
        public virtual Promise<GetTotalCouponResponse> GetCouponsCount(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/object/commerce/{objectId}/coupons/count";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<GetTotalCouponResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<GetTotalCouponResponse>);
        }
        public virtual Promise<CommonResponse> PutListingsCooldown(string objectId, CooldownModifierRequest gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/listings/cooldown";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<GetActiveOffersResponse> GetOffersAdmin(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> language, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stores)
        {
            string gsUrl = "/object/commerce/{objectId}/offersAdmin";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((language != default(OptionalString)) 
                        && language.HasValue))
            {
                gsQueries.Add(string.Concat("language=", language.ToString()));
            }
            if (((time != default(OptionalString)) 
                        && time.HasValue))
            {
                gsQueries.Add(string.Concat("time=", time.ToString()));
            }
            if (((stores != default(OptionalString)) 
                        && stores.HasValue))
            {
                gsQueries.Add(string.Concat("stores=", stores.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetActiveOffersResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetActiveOffersResponse>);
        }
        public virtual Promise<CommonResponse> PostPurchase(string objectId, PurchaseRequest gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/purchase";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<ResultResponse> PutPurchase(string objectId, ReportPurchaseRequest gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/purchase";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<ResultResponse>(Method.PUT, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<ResultResponse>);
        }
        public virtual Promise<ActiveListingResponse> GetListings(string listing, string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> store, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time)
        {
            string gsUrl = "/object/commerce/{objectId}/listings";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            gsQueries.Add(string.Concat("listing=", listing.ToString()));
            if (((store != default(OptionalString)) 
                        && store.HasValue))
            {
                gsQueries.Add(string.Concat("store=", store.ToString()));
            }
            if (((time != default(OptionalString)) 
                        && time.HasValue))
            {
                gsQueries.Add(string.Concat("time=", time.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<ActiveListingResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<ActiveListingResponse>);
        }
        public virtual Promise<CommonResponse> DeleteStatus(string objectId, ClearStatusRequest gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/status";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.DELETE, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostCoupons(string objectId, GiveCouponReq gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/coupons";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<CommonResponse> PostStatsUpdate(string objectId, StatSubscriptionNotification gsReq)
        {
            string gsUrl = "/object/commerce/{objectId}/stats/update";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            // make the request and return the result
            return _requester.Request<CommonResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<CommonResponse>);
        }
        public virtual Promise<GetActiveOffersResponse> GetOffers(string objectId, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> language, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> time, [System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<string> stores)
        {
            string gsUrl = "/object/commerce/{objectId}/offers";
            gsUrl = gsUrl.Replace("{objectId}", objectId);
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((language != default(OptionalString)) 
                        && language.HasValue))
            {
                gsQueries.Add(string.Concat("language=", language.ToString()));
            }
            if (((time != default(OptionalString)) 
                        && time.HasValue))
            {
                gsQueries.Add(string.Concat("time=", time.ToString()));
            }
            if (((stores != default(OptionalString)) 
                        && stores.HasValue))
            {
                gsQueries.Add(string.Concat("stores=", stores.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetActiveOffersResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetActiveOffersResponse>);
        }
    }
}
