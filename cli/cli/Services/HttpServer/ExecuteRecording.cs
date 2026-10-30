using Beamable.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace cli.Services.HttpServer;

/// <summary>One /execute exchange as a golden fixture: the raw request body and each event's JSON, in order.</summary>
[Serializable]
public class ExecuteFixture
{
	public string name;
	public string body;
	public List<JObject> events = new List<JObject>();
}

/// <summary>Collects an /execute exchange for `server serve --record-to`.</summary>
public class ExecuteRecording
{
	private readonly object _lock = new object();
	private readonly ExecuteFixture _fixture = new ExecuteFixture();

	public void SetRequest(string body)
	{
		_fixture.body = body;
	}

	public void Add(string eventJson)
	{
		lock (_lock)
		{
			_fixture.events.Add(JObject.Parse(eventJson));
		}
	}

	public void Save(string folder)
	{
		try
		{
			Directory.CreateDirectory(folder);
			string command;
			try
			{
				command = JsonConvert.DeserializeObject<ServerRequest>(_fixture.body ?? "")?.commandLine;
			}
			catch (JsonException)
			{
				command = null;
			}
			command = string.IsNullOrWhiteSpace(command) ? "invalid-request" : command;
			var slug = Regex.Replace(command.Trim(), "[^A-Za-z0-9]+", "-").Trim('-');
			_fixture.name = slug.Length > 60 ? slug.Substring(0, 60) : slug;
			string json;
			lock (_lock)
			{
				json = JsonConvert.SerializeObject(_fixture, Formatting.Indented);
			}
			var path = Path.Combine(folder, $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{_fixture.name}.json");
			File.WriteAllText(path, json);
			Log.Information($"Recorded /execute exchange to {path}");
		}
		catch (Exception ex)
		{
			// A recording is a debugging aid; it must never fail the command.
			Log.Error($"Could not save /execute recording. message=[{ex.Message}]");
		}
	}
}
