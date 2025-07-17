using System.Net.Http.Headers;
using System.Text;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using Newtonsoft.Json;

namespace Beamable.Tooling.Common;

/// <summary>
/// The <see cref="ISignedRequesterConfig"/> configures an instance of the <see cref="ISignedRequester"/>
///
/// </summary>
public interface ISignedRequesterConfig
{
    /// <summary>
    /// The api path for all requests, usually "https://api.beamable.com"
    /// </summary>
    public string Host { get; }
    
    /// <summary>
    /// Signed requests require a realm secret. This delegate must return the realm secret
    /// that will be used by the requester.
    /// </summary>
    public Func<Promise<string>> RealmSecretGenerator { get; }
    
    /// <summary>
    /// Always "1".
    /// </summary>
    public string ApiVersion { get; }
}

public class DefaultSignedRequesterConfig : ISignedRequesterConfig
{
    public string Host { get; set; }
    public Func<Promise<string>> RealmSecretGenerator { get; set; }
    public string ApiVersion => "1";
}

public class SignedRequestAccessTokenShim : IAccessToken
{
    public string Token => null;
    public string RefreshToken => null;
    public DateTime ExpiresAt => DateTime.MaxValue;
    public long ExpiresIn => long.MaxValue;
    public DateTime IssuedAt => DateTime.Now;
    public string Cid { get; set; }
    public string Pid { get; set; }
}

public class HttpSignedRequester : ISignedRequester
{
    private const string PLAYER_ID_NONE = "";
    
    private ISignedRequesterConfig _config;
    private HttpClient _client;
    private string _secret;
    private string _playerId;
    private IUserContext _userContext;

    public IAccessToken AccessToken { get; private set; }
    public string Cid { get; private set; }
    public string Pid { get; private set; }
    
    public HttpSignedRequester(ISignedRequesterConfig config, IRealmInfo realmInfo, IUserContext userContext)
    {
        _userContext = userContext;
        _config = config;
        AccessToken = new SignedRequestAccessTokenShim
        {
            Cid = realmInfo.CustomerID,
            Pid = realmInfo.ProjectName
        };
        Cid = realmInfo.CustomerID;
        Pid = realmInfo.ProjectName;
    }
    
    public virtual string EscapeURL(string url)
    {
        return System.Web.HttpUtility.UrlEncode(url);
    }


    public virtual void SetPlayerId(string playerId)
    {
        switch (playerId)
        {
            case PLAYER_ID_NONE:
            case "0":
                _playerId = PLAYER_ID_NONE;
                break;
            default:
                _playerId = playerId;
                break;
        }
    }

    public virtual Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
        bool useCache = false)
    {
        var opts = new SDKRequesterOptions<T>
        {
            method = method,
            uri = uri,
            body = body,
            includeAuthHeader = includeAuthHeader,
            useCache = useCache,
            parser = parser,
            useConnectivityPreCheck = false,
            disableScopeHeaders = false
        };
        return BeamableRequest(opts);
    }

    public virtual IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
    {
        // this request makes no sense for a signed requester... 
        return this;
    }

    public virtual async Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
    {

        if (_client == null)
        {
            _client = new HttpClient();
        }

        if (string.IsNullOrEmpty(_secret))
        {
            _secret = await _config.RealmSecretGenerator();
        }

        string bodyContent = SignedRequesterHelper.GetBodyToSign(
            req.method, 
            req.body,
            body => JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance));
        
        var uri = _config.Host + req.uri;

        var signature = SignedRequesterHelper.CalculateSignature(Pid, 
            _secret, 
            req.uri, 
            body: bodyContent, 
            version: _config.ApiVersion);

        var httpMethod = HttpMethod.Get;
        switch (req.method)
        {
            case Method.PUT:
                httpMethod = HttpMethod.Put;
                break;
            case Method.POST:
                httpMethod = HttpMethod.Post;
                break;
            case Method.DELETE:
                httpMethod = HttpMethod.Delete;
                break;
        }

        var request = new HttpRequestMessage(httpMethod, uri);
        request.Headers.Add(Constants.Requester.HEADER_SCOPE, $"{Cid}.{Pid}");
        request.Headers.Add(Constants.Requester.HEADER_SIGNATURE, signature);

        if (req.includeAuthHeader)
        {
            string id = null;
            switch (_playerId)
            {
                case PLAYER_ID_NONE:
                    // do not include the header, because the option has been
                    //  explicitly configured to ignore
                    break;
                
                case null:
                    // use whatever is in the userContext
                    try
                    {
                        id = _userContext.UserId.ToString();
                    }
                    catch (ArgumentException)
                    {
                        // there is no default userId, so don't do anything.
                        id = null;
                    }
                    break;
                
                default:
                    // use whatever was configured. 
                    id = _playerId;
                    break;
                    
            }

            if (!string.IsNullOrEmpty(id))
            {
                request.Headers.Add(Constants.Requester.HEADER_GAMERTAG, id);
            }
            
        }
        
        if (req.headerInterceptor != null)
        {
            var headers = request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault());
            headers = req.headerInterceptor(headers);
            
            foreach (var kvp in headers)
            {
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }
        
        if (bodyContent != null)
            request.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        var rawResponse = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var parser = req.parser;
            if (parser == null)
            {
                parser = (json) => JsonConvert.DeserializeObject<T>(json, UnitySerializationSettings.Instance);
            }

            var result = parser(rawResponse);
            return result;
        }
        
        var ex = new RequesterException("signed-requester", req.method.ToReadableString(), uri,
            (int)response.StatusCode, rawResponse);
        throw ex;
    }

    public void SetRealmInfo(string cid, string pid, string secret)
    {
	    Cid = cid;
	    Pid = pid;
	    _secret = secret;
    }
}
