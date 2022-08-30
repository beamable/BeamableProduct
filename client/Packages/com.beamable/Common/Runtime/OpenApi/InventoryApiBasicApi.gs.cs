
namespace Beamable.Api.Open.Inventory
{
    using Beamable.Api.Open.Models;
    using Beamable.Common.Content;
    using Beamable.Common;
    using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
    using Method = Beamable.Common.Api.Method;
    
    public class InventoryApiBasicApi
    {
        private IBeamableRequester _requester;
        public InventoryApiBasicApi(IBeamableRequester requester)
        {
            this._requester = requester;
        }
        public virtual Promise<ItemContentResponse> GetItems([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/inventory/items";
            // make the request and return the result
            return _requester.Request<ItemContentResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<ItemContentResponse>);
        }
        public virtual Promise<CurrencyContentResponse> GetCurrency([System.Runtime.InteropServices.DefaultParameterValueAttribute(true)] [System.Runtime.InteropServices.OptionalAttribute()] bool includeAuthHeader)
        {
            string gsUrl = "/basic/inventory/currency";
            // make the request and return the result
            return _requester.Request<CurrencyContentResponse>(Method.GET, gsUrl, default(object), includeAuthHeader, Beamable.Serialization.JsonSerializable.FromJson<CurrencyContentResponse>);
        }
    }
}
