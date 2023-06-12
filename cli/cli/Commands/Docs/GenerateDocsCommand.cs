using Beamable.Common.Dependencies;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace cli.Docs;

public class GenerateDocsCommandArgs : CommandArgs
{
	public string categorySlug;
	public string readmeApiKey;
}


public class GenerateDocsCommand : AppCommand<GenerateDocsCommandArgs>
{
	private const string KEY_TITLE = "{{TITLE}}";
	private const string KEY_DESC = "{{DESC}}";
	private const string KEY_PARAMS = "{{PARAMS}}";

	public HttpClient _client;
	
	public GenerateDocsCommand() : base("docs", "Generate CLI documentation")
	{
		_client = new HttpClient();
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
	}

	public override void Configure()
	{
		AddOption(new Option<string>("category", () => "cli", "The category slug to use"), (args, i) => args.categorySlug = i);
		AddOption(new Option<string>("api", "The api key to use to push to Readme"), (args, i) => args.readmeApiKey = i);
	}

	public override async Task Handle(GenerateDocsCommandArgs args)
	{
		// build a markdown file for each command...
		var generatorContext = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();

		_client.DefaultRequestHeaders.Add("Authorization", "Basic cmRtZV94bjhzOWhiOGY4NDY3NGZmNWEzOGM1MjU3MTcwNWRhYjQ0ZTI5YzU3ODQxY2EzOWIyYTY3MDE5OGQ1N2M5ZDg5MjZmMGZjOg==");

		foreach (var command in generatorContext.Commands)
		{
			if (command == generatorContext.Root) continue;
			
			// if (command.command is not ConfigCommand) continue;
			var doc = GenerateDocFile(args.DependencyProvider, command, args);
			// var json = JsonConvert.SerializeObject(doc);
			Log.Information(doc.markdownContent);
			await PublishDoc(doc);

		}
		
		// upload docs to readme...
		
		// return Task.CompletedTask;
	}

	async Task DeleteDoc(DocDesc desc)
	{
		var res = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"https://dash.readme.com/api/v1/docs/{desc.slug}"));
		if (!res.IsSuccessStatusCode)
		{
			Log.Error($"failed to delete {desc.slug} status=[{res.StatusCode}] {await res.Content.ReadAsStringAsync()}");
		}
	}
	async Task<bool> DoesDocExist(DocDesc desc)
	{
		
		var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://dash.readme.com/api/v1/docs/{desc.slug}"));
		return response.StatusCode == HttpStatusCode.OK;
	}

	async Task PublishDoc(DocDesc desc)
	{
		var exists = await DoesDocExist(desc);

			var reqObject = new ReadmePostDocumentRequest
			{
				slug = desc.slug,
				body = desc.markdownContent,
				title = desc.title,
				excerpt = desc.excerpt,
				categorySlug = desc.categorySlug,
				parentDocSlug = desc.parentSlug
			};
			var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
			var json = JsonConvert.SerializeObject(reqObject, settings);

			var req = new StringContent(json, Encoding.UTF8, "application/json");
			// Log.Information(json);

			var method = exists ? HttpMethod.Put : HttpMethod.Post;
			var endpoint = exists ? $"https://dash.readme.com/api/v1/docs/{desc.slug}" : "https://dash.readme.com/api/v1/docs";
			var msg = new HttpRequestMessage(method, endpoint);
			msg.Content = req;
			
			// response = await _client.SendM($"https://dash.readme.com/api/v1/docs", req);

			var response = await _client.SendAsync(msg);
			if (!response.IsSuccessStatusCode)
			{
				Log.Error($"Failed to submit new doc! {desc.slug} status=[{response.StatusCode}] reason=[{response.ReasonPhrase}] raw=[{await response.Content.ReadAsStringAsync()}]");
			}
		
	}

	DocDesc GenerateDocFile(IDependencyProvider provider, BeamCommandDescriptor commandDesc, GenerateDocsCommandArgs args)
	{
		var commandName = commandDesc.executionPath;

		var content = @$"#{KEY_TITLE}

### Description
{KEY_DESC}

";

		// TODO: USAGE.
		// TODO: Parent command.
		// TODO: Children commands.

		// var ctx = provider.GetService<HelpContext>();
		// // ctx.
		// // ctx.HelpBuilder.
		// var help = new HelpBuilder(LocalizationResources.Instance);
		// // help.
		// HelpBuilder.Default.CommandUsageSection()
		
		
		var rendered = content
				.Replace(KEY_TITLE, commandDesc.executionPath.Substring("beam".Length).Trim())
				.Replace(KEY_DESC, commandDesc.command.Description)
				.Replace(KEY_PARAMS, commandDesc.command.Arguments.ToList().ToString())
			;

		var path = commandDesc.executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string parentSlug = null;
		string slug = "beam";
		if (path.Length > 1)
		{
			slug = string.Join("-", path.Skip(1));
			// parentSlug = string.Join("-", path.Skip(1).Take(path.Length - 2));
			// parentSlug = string.IsNullOrEmpty(parentSlug) ? "cli-beam" : $"{args.categorySlug}-{parentSlug}";
		}

		var doc = new DocDesc
		{
			excerpt = commandDesc.command.Description,
			title = commandDesc.ExecutionPathAsCapitalizedStringWithoutBeam(" "),
			markdownContent = rendered,
			slug = $"{args.categorySlug}-{slug}",
			categorySlug = args.categorySlug, // TODO: pull from option
			parentSlug = "cli-commands"
		};

		return doc;
	}

	public class DocDesc
	{
		public string title;
		public string excerpt;
		public string markdownContent;
		public string slug;
		public string categorySlug;
		public string parentSlug;
	}

	public class ReadmePostDocumentRequest
	{
		public string title;
		public string slug;
		public string type = "basic";
		public string excerpt;
		public string body;
		public bool hidden = false;
		public string categorySlug;
		public string parentDocSlug;
	}
}
