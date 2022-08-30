
namespace Beamable.Api.Open.Commerce
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public interface ICommerceApiBasicApi
    {
        Promise<ResultResponse> PostCatalogLegacy(SaveCatalogRequest gsReq);
        Promise<GetCatalogResponse> GetCatalog([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> version);
        Promise<GetSKUsResponse> GetSkus([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> version, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader);
        Promise<ResultResponse> PostSkus(SaveSKUsRequest gsReq);
    }
    public class CommerceApiBasicApi : ICommerceApiBasicApi
    {
        private IBeamableRequester _requester;
        public CommerceApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<ResultResponse> PostCatalogLegacy(SaveCatalogRequest gsReq)
        {
            string gsUrl = "/basic/commerce/catalog/legacy";
            // make the request and return the result
            return _requester.Request<ResultResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<ResultResponse>);
        }
        public virtual Promise<GetCatalogResponse> GetCatalog([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> version)
        {
            string gsUrl = "/basic/commerce/catalog";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((version != default(OptionalLong)) 
                        && version.HasValue))
            {
                gsQueries.Add(string.Concat("version=", version.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetCatalogResponse>(Method.GET, gsUrl, default(object), true, Beamable.Serialization.JsonSerializable.FromJson<GetCatalogResponse>);
        }
        public virtual Promise<GetSKUsResponse> GetSkus([System.Runtime.InteropServices.DefaultParameterValueAttribute(null)] [System.Runtime.InteropServices.OptionalAttribute()] Beamable.Common.Content.Optional<long> version, [System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/commerce/skus";
            string gsQuery = "?";
            System.Collections.Generic.List<string> gsQueries = new System.Collections.Generic.List<string>();
            if (((version != default(OptionalLong)) 
                        && version.HasValue))
            {
                gsQueries.Add(string.Concat("version=", version.ToString()));
            }
            gsQuery = string.Concat(gsQuery, string.Join("&", gsQueries));
            gsUrl = string.Concat(gsUrl, gsQuery);
            // make the request and return the result
            return _requester.Request<GetSKUsResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<GetSKUsResponse>);
        }
        public virtual Promise<ResultResponse> PostSkus(SaveSKUsRequest gsReq)
        {
            string gsUrl = "/basic/commerce/skus";
            // make the request and return the result
            return _requester.Request<ResultResponse>(Method.POST, gsUrl, Beamable.Serialization.JsonSerializable.ToJson(gsReq), true, Beamable.Serialization.JsonSerializable.FromJson<ResultResponse>);
        }
    }
}
