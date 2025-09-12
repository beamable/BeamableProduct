namespace Beamable.Server;

public class MicroserviceNonceResponse
{
    public string nonce;
}

public class MicroserviceAuthRequest
{
    public string cid, pid, signature;
}

public class MicroserviceAuthResponse
{
    public string result;
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