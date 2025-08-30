using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text.Json;
using Beamable.Common;
using Beamable.Server;
using Hocon;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace cli.BackendCommands;

public static class BackendMongoUtil
{
    private static BackendMongo _mongoSingleton;

    public static BackendMongo GetMongo(BackendToolList list)
    {
        if (_mongoSingleton != null) return _mongoSingleton;
        var dbFlake = list.tools.FirstOrDefault(d => d.name == "dbflake");
        if (dbFlake == null)
            throw new CliException(
                "To connect to mongo, the cli is trying to find connection strings from dbflakes .conf files, but no dbflake was found.");
        var conf = HoconConfigurationFactory.FromFile(Path.Combine(dbFlake.projectPath, "src", "main", "resources",
            "server.conf"));
        var userName = conf.GetString("mongodb.default.username");
        var password = conf.GetString("mongodb.default.password");
        
        var host = conf.GetString("mongodb.master.host");
        var port = conf.GetString("mongodb.master.port");
        _mongoSingleton = Connect(userName, password, host, port);
        return _mongoSingleton;
    }
    
    public static async Task RunWatching<TDocument>(
        this IMongoCollection<TDocument> collection, 
        CancellationToken ct,
        Action<ChangeStreamDocument<TDocument>> changeHandler)
    {
        try
        {
            using var changeStream = await collection.WatchAsync(cancellationToken: ct);
            while (await changeStream.MoveNextAsync(ct))
            {
                foreach (var change in changeStream.Current)
                {
                    changeHandler(change);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // its okay to let a cancellation happen.
        }
        catch (Exception ex)
        {
            Log.Fatal("Unknown error happened during mongo watch. " + ex.GetType().Name);
        }
    }
    
    public static BackendMongo Connect(string mongoUser, string mongoPassword, string host, string port)
    {
        var connStr = $"mongodb://{mongoUser}:{WebUtility.UrlEncode(mongoPassword)}@{host}:{port}/?directConnection=true";
        var clientSettings = MongoClientSettings.FromConnectionString(connStr);
        clientSettings.ConnectTimeout = TimeSpan.FromMilliseconds(250);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(250); // how long driver waits to find a suitable server
        // clientSettings.SocketTimeout = TimeSpan.FromMilliseconds(250);          // how long operations wait on socket reads/writes
        var client = new MongoClient(clientSettings);
        return new BackendMongo
        {
            Client = client
        };
    }

    public static IMongoCollection<BackendTopologyEntry> GetTopologyCollection(this BackendMongo mongo)
    {
        var db = mongo.Client.GetDatabase(mongo.WizardMasterDatabaseName);
        return db.GetCollection<BackendTopologyEntry>(mongo.ServiceTopologyCollectionName);
    }

    public static async Task<bool> IsAvailable(this BackendMongo mongo)
    {
        try
        {
            
            var db = mongo.Client.GetDatabase("admin");
            var command = new BsonDocument("ping", 1);
            await db.RunCommandAsync<BsonDocument>(command); // lightweight check
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public static async Task<List<BackendTopologyEntry>> GetTopologies(this BackendMongo mongo)
    {
        if (!await mongo.IsAvailable())
        {
            return new List<BackendTopologyEntry>();
        }
        var coll = GetTopologyCollection(mongo);
        var cursor = await coll.FindAsync(FilterDefinition<BackendTopologyEntry>.Empty);
        var entries =  await cursor.ToListAsync();

        var entryCache = new Dictionary<string, BackendTopologyDebugInfo>();
        
        var httpClient = new HttpClient();
        foreach (var entry in entries)
        {
            await Ping(entry);
        }
        
        async Task Ping(BackendTopologyEntry entry)
        {
           
            var host = $"http://{entry.binding.host}:{entry.binding.port}";

            lock (entryCache)
            {
                if (entryCache.TryGetValue(host, out entry.debugInfo))
                {
                    entry.respondedToDebugEndpoint = true;
                    return;
                }
            }
            
            var req = new HttpRequestMessage(HttpMethod.Get, host);
            
            req.Headers.Add("x-local-dbg", "1");
            try
            {

                var res = await httpClient.SendAsync(req);
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    entry.debugInfo = new BackendTopologyDebugInfo
                    {
                        processId = -1,
                    };
                    return;
                }

                var dbgJson = await res.Content.ReadAsStringAsync();
                entry.debugInfo = JsonSerializer.Deserialize<BackendTopologyDebugInfo>(dbgJson,
                    new JsonSerializerOptions
                    {
                        IncludeFields = true
                    });
                entry.respondedToDebugEndpoint = true;

                lock (entryCache)
                {
                    entryCache[host] = entry.debugInfo;
                }
            }
            catch (Exception ex)
            {
                entry.debugInfo = new BackendTopologyDebugInfo
                {
                    processId = -1,
                };
                return;
            }
        }
        
        return entries;
    }
}

public class BackendTopologyDebugInfo
{
    public int processId;
    public string stdOutRedirection;
    public string stdErrRedirection;
}

public class BackendTopologyEntry
{
    public ObjectId id;
    public string service;
    public string instance;
    public BackendTopologyBindingEntry binding;

    public BackendTopologyDebugInfo debugInfo;
    public bool respondedToDebugEndpoint;
    
    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; }

    public string ServiceName => service.Split('.')[0];
    public string ServiceType => service.Split('.')[1];

    public int ProcessId => debugInfo?.processId ?? -1;
    public string StandardOutRedirection => debugInfo?.stdOutRedirection;
    public string StandardErrRedirection => debugInfo?.stdErrRedirection;
}

public class BackendTopologyBindingEntry
{
    public string host;
    public int port;
}

public class BackendMongo
{
    public MongoClient Client { get; set; }
    public string WizardMasterDatabaseName => "wizard_master"; // TODO pull from env?
    public string ServiceTopologyCollectionName => "service_topology";
}