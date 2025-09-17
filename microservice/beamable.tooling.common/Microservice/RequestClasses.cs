namespace Beamable.Server;

[Serializable]
public class MicroserviceNonceResponse
{
    public string nonce;
}

[Serializable]
public class MicroserviceAuthRequest
{
    public string cid, pid, signature;
}

[Serializable]
public class MicroserviceAuthRequestWithToken
{
    public string cid, pid, token;
}

[Serializable]
public class MicroserviceAuthResponse
{
    public string result;
    public MicroserviceAuthResponse(){}
}

public class MicroserviceEventProviderRequest
{
    public string type = "event";
    public string[] evtWhitelist;
    public string name; // We can remove this field after the platform no longer needs it. Maybe mid August 2022?
}

public class MicroserviceProviderResponse
{

}