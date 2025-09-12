using System.Threading.Tasks;
using Beamable.Server;

namespace microservice.dbmicroservice;

public class UsageTest
{
    class X
    {
        
    }
    public static async Task Main()
    {
        var conf = new BeamServiceConfig();
        conf.IncludeRoutes<X>();

        await MicroserviceStartupUtil.Begin(conf);
    }
}