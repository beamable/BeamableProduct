using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using ZLogger;

namespace tests.Web;

/// <summary>
/// Builds OpenAPI documents the way a Beamable microservice does: <see cref="ServiceDocGenerator"/> runs over a
/// real <see cref="Microservice"/> type (as <c>--generate-oapi</c> does after a build), the result is serialized
/// to JSON (the <c>beam_openApi.json</c> file) and read back with <see cref="OpenApiStringReader"/>, exactly as the
/// CLI loads it into <c>HttpMicroserviceLocalProtocol.OpenApiDoc</c>.
/// </summary>
public static class WebClientTestDocuments
{
	public static OpenApiDocument MatchServiceDocument() => Generate<WebClientTestMatchService>();

	public static OpenApiDocument InventoryServiceDocument() => Generate<WebClientTestInventoryService>();

	public static OpenApiDocument PrimitiveServiceDocument() => Generate<WebClientTestPrimitiveService>();

	private static OpenApiDocument Generate<TMicroservice>() where TMicroservice : Microservice
	{
		InitializeLogging();
		var builder = new DependencyBuilder();
		builder.AddSingleton<BeamStandardTelemetryAttributeProvider>();
		builder.AddSingleton<SingletonDependencyList<ITelemetryAttributeProvider>>();
		builder.AddSingleton<IMicroserviceArgs>(new MicroserviceArgs());

		var generated = new ServiceDocGenerator().Generate<TMicroservice>(builder.Build());
		var json = generated.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		var document = new OpenApiStringReader().Read(json, out var diagnostic);
		Assert.That(diagnostic.Errors, Is.Empty, "the generated OpenAPI document should be valid");
		return document;
	}

	/// <summary>The CLI's <c>Log</c> needs a logger; the CLI sets one up at startup, tests do it here.</summary>
	public static void InitializeLogging()
	{
		BeamableZLoggerProvider.Provider ??= new BeamableZLoggerProvider();
		BeamableZLoggerProvider.LogContext.Value ??= LoggerFactory
			.Create(builder => builder.AddZLoggerConsole())
			.CreateLogger(nameof(WebClientTestDocuments));
	}
}

[Serializable]
public class WebClientTestMatchPlayer
{
	public long playerId;
	public int score;
}

[Serializable]
public class WebClientTestMatchView
{
	public string matchId;
	public long createdAt;
	public long? endedAt;
	public List<long> roundScores;
	public List<WebClientTestMatchPlayer> players;
}

[Serializable]
public class WebClientTestInventoryItem
{
	public string itemId;
	public int quantity;
}

/// <summary>
/// One DTO (<see cref="WebClientTestMatchView"/>) reachable from several callables, as a request parameter, a
/// response, and an array response.
/// </summary>
[Microservice("MatchService")]
public class WebClientTestMatchService : Microservice
{
	[ClientCallable]
	public WebClientTestMatchView GetMatch(string matchId) => null;

	[ClientCallable]
	public WebClientTestMatchView JoinMatch(string matchId, long playerId) => null;

	[ClientCallable]
	public WebClientTestMatchView[] ListMatches() => null;

	[ClientCallable]
	public void UpdateMatch(WebClientTestMatchView match)
	{
	}

	/// <summary>A nullable parameter is marked nullable in the request schema.</summary>
	[ClientCallable]
	public WebClientTestMatchView[] ListMatchesSince(long? since) => null;

	[ServerCallable]
	public long CountMatches() => 0;
}

[Microservice("InventoryService")]
public class WebClientTestInventoryService : Microservice
{
	[ClientCallable]
	public WebClientTestInventoryItem GetItem(string itemId) => null;
}

/// <summary>
/// A service whose callables take no parameters and return primitives, so it contributes no client types.
/// </summary>
[Microservice("PrimitiveService")]
public class WebClientTestPrimitiveService : Microservice
{
	[ClientCallable]
	public int GetCount() => 0;
}
