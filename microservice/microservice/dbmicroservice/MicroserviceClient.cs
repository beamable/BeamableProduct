using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;

namespace Beamable.Server;


public class MicroserviceClient
{
    private IDependencyProvider _provider;

    public MicroserviceClient(IDependencyProvider provider)
    {
        _provider = provider;
    }

    public async Task<T> Request<T>(string service, string route, object req)
    {
        var requester = _provider.GetService<MicroserviceRequester>();
        var context = _provider.GetService<MicroserviceRequestContext>();
        
        return await requester.BeamableRequest(new SDKRequesterOptions<T>
        {
            method = Method.POST,
            body = req,
            includeAuthHeader = true,
            uri = $"basic/{context.Cid}.{context.Pid}.micro_{service}/{route}",
            headerInterceptor = headers =>
            {
                if (context.Headers.TryGetValue(Constants.Requester.HEADER_ROUTINGKEY, out var val))
                {
                    headers[Constants.Requester.HEADER_ROUTINGKEY] = val;
                }
                return headers;
            }
        });
    }
}