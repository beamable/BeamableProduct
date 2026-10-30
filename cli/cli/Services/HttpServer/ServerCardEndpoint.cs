using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.HttpServer;

/// <summary>
/// Standard pre-connection discovery for the MCP endpoint: an MCP Server Card (SEP-2127, extension
/// <c>io.modelcontextprotocol/server-card</c>) at <c>&lt;mcp-url&gt;/server-card</c>, and an AI Catalog at
/// <c>/.well-known/ai-catalog.json</c> that points to it. Both are public by design: they say what the server is
/// and how to connect, never the token, the workspace or the project.
/// </summary>
public static class ServerCardEndpoint
{
	public const string CARD_ROUTE = "mcp/server-card";
	public const string CATALOG_ROUTE = ".well-known/ai-catalog.json";
	public const string CARD_MEDIA_TYPE = "application/mcp-server-card+json";
	public const string CATALOG_MEDIA_TYPE = "application/ai-catalog+json";
	public const string CARD_SCHEMA = "https://static.modelcontextprotocol.io/schemas/v1/server-card.schema.json";
	public const string CARD_NAME = "com.beamable/beam-cli";
	public const string CATALOG_IDENTIFIER = "urn:air:beamable.com:mcp:beam-cli";

	public static bool Owns(string routePath) => routePath is CARD_ROUTE or CATALOG_ROUTE;

	public static object BuildCard(string serverUri, string version) => new Dictionary<string, object>
	{
		["$schema"] = CARD_SCHEMA,
		["name"] = CARD_NAME,
		["title"] = "Beamable CLI",
		["version"] = version,
		["description"] = "Runs Beamable CLI commands (content, microservices, deployments, realms) for the project " +
		                  "this local server was started in. Served by `beam server serve --enable-mcp` on this machine only.",
		["websiteUrl"] = "https://help.beamable.com/CLI-Latest/",
		["remotes"] = new[]
		{
			new Dictionary<string, object>
			{
				["type"] = "streamable-http",
				["url"] = serverUri.TrimEnd('/') + McpHttpEndpoint.PATH,
				["supportedProtocolVersions"] = McpHttpEndpoint.SupportedProtocolVersions,
				["headers"] = new[]
				{
					new Dictionary<string, object>
					{
						["name"] = "Authorization",
						["description"] = "The server's session token. `beam mcp token` prints the header for the running server.",
						["isRequired"] = true,
						["isSecret"] = true,
						["value"] = "Bearer {token}",
						["variables"] = new Dictionary<string, object>
						{
							["token"] = new Dictionary<string, object>
							{
								["description"] = "Session token from .beamable/temp/servers/<pid>.json",
								["isRequired"] = true,
								["isSecret"] = true
							}
						}
					}
				}
			}
		}
	};

	public static object BuildCatalog(string serverUri) => new Dictionary<string, object>
	{
		["specVersion"] = "1.0",
		["entries"] = new[]
		{
			new Dictionary<string, object>
			{
				["identifier"] = CATALOG_IDENTIFIER,
				["type"] = CARD_MEDIA_TYPE,
				["url"] = serverUri.TrimEnd('/') + "/" + CARD_ROUTE
			}
		}
	};

	/// <summary>Answers a discovery request: CORS for any origin, ETag revalidation, an hour of caching.</summary>
	public static async Task Handle(HttpListenerRequest req, HttpListenerResponse resp, string routePath, string version)
	{
		var serverUri = $"http://127.0.0.1:{req.LocalEndPoint.Port}/";
		resp.Headers["Access-Control-Allow-Origin"] = "*";
		resp.Headers["Access-Control-Allow-Methods"] = "GET";
		resp.Headers["Access-Control-Allow-Headers"] = "Content-Type, If-None-Match";
		resp.Headers["Access-Control-Expose-Headers"] = "ETag";

		if (req.HttpMethod == "OPTIONS")
		{
			resp.StatusCode = 204;
			return;
		}

		var isCard = routePath == CARD_ROUTE;
		var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
			isCard ? BuildCard(serverUri, version) : BuildCatalog(serverUri), Formatting.Indented));
		var etag = "\"" + Convert.ToHexString(SHA256.HashData(body))[..16].ToLowerInvariant() + "\"";
		resp.Headers["ETag"] = etag;
		resp.Headers["Cache-Control"] = "public, max-age=3600";

		if (req.Headers["If-None-Match"] == etag)
		{
			resp.StatusCode = 304;
			return;
		}

		resp.StatusCode = 200;
		resp.ContentType = (isCard ? CARD_MEDIA_TYPE : CATALOG_MEDIA_TYPE) + "; charset=utf-8";
		if (req.HttpMethod != "HEAD")
		{
			await resp.OutputStream.WriteAsync(body);
		}
	}
}
