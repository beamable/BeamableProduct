using System.CommandLine;
using System.Net;
using System.Text.Json;
using Beamable.Server;
using cli.Services;

namespace cli.DeploymentCommands;

public class CheckRegistryCommandArgs : CommandArgs
{
    public string service;
}

public class CheckRegistryCommandResults
{
    public string dockerReposityName;
    public List<string> availableImageIds = new List<string>();
}

public class DockerRegistryImageTags
{
    public string name;
    public List<string> tags = new List<string>();
}

public class CheckRegistryCommand : AtomicCommand<CheckRegistryCommandArgs, CheckRegistryCommandResults>
{
    public override bool IsForInternalUse => true;

    public CheckRegistryCommand() : base("registry", "Find all docker image tags that exist for the given beamoId")
    {
    }

    public override void Configure()
    {
        AddArgument(new Argument<string>("service-id", "The beamo id for the service to get images for"), (args, i) => args.service = i);
    }

    public override async Task<CheckRegistryCommandResults> GetResult(CheckRegistryCommandArgs args)
    {
        var uri = await args.BeamoService.GetDockerImageRegistryUri();
        var realm = await args.RealmsApi.GetRealm();
        var imageName = $"{realm.Cid}_{realm.GamePid}_{args.service}";
        var imageNameHash = ServiceUploadUtil.GetHash(imageName).Substring(0, 30);
        Log.Verbose($"image=[{imageName}] hash=[{imageNameHash}]");

        
        /*
         * The registry API is kind of a pain to understand :(
         *  The docs: https://docker-docs.uclv.cu/registry/spec/api/
         *
         * Our docker registry is available through an NGINX proxy that handles
         * Beamable auth.
         *
         * Some other useful endpoints might look like,
         *  var allRepositories = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/v2/_catalog"));
         *  var ping = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/v2/"));
         *  var manifestForImageTag = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/v2/{imageNameHash}/manifests/{imageId}"));
         */
        
        var client = new HttpClient()
        {
            BaseAddress = new Uri(uri),
            Timeout = Timeout.InfiniteTimeSpan,
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        
        client.DefaultRequestHeaders.Add("x-ks-clientid", realm.Cid);
        client.DefaultRequestHeaders.Add("x-ks-projectid", realm.Pid);
        client.DefaultRequestHeaders.Add("x-ks-token", args.AppContext.Token.Token);
        
        var availableTags = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/v2/{imageNameHash}/tags/list"));
        availableTags.EnsureSuccessStatusCode();
        var availableTagsResponse = await availableTags.Content.ReadAsStringAsync();
        var imageTags = JsonSerializer.Deserialize<DockerRegistryImageTags>(availableTagsResponse, new JsonSerializerOptions
        {
            IncludeFields = true
        });
        
        
        return new CheckRegistryCommandResults
        {
            dockerReposityName = imageTags.name,
            availableImageIds = imageTags.tags
        };
    }

    public class DockerRegistryCatalogResponse
    {
        // {
        //     "name" : "e7e51c0576d189e9846bb91d7269d7",
        //     "tags" : [ "b8c455961431", "f41e489ff45d" ]
        // }
        public List<string> repositories = new List<string>();
    }


}