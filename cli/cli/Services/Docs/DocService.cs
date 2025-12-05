using System.Collections;
using cli.Docs;
using cli.Unreal;
using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json;
using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Beamable.Server;

namespace cli.Services;

public class DocService
{
	private const string KEY_ABOUT = "{{About}}";
	private const string KEY_USAGE_NOTE = "{{Usage}}";
	private const string KEY_USAGE = "{{UsageSyntax}}";
	private const string KEY_ARGS = "{{ArgsSyntax}}";
	private const string KEY_OPTS = "{{OptionsSyntax}}";
	private const string KEY_PARENT = "{{ParentSection}}";
	private const string KEY_CHILDREN = "{{ChildrenSection}}";
	private const string KEY_TITLE = "TITLE";
	private const string KEY_DESC = "{{DESC}}";
	// private const string KEY_PARAMS = "{{PARAMS}}";

	private const string DocTemplate = @$"
{KEY_USAGE_NOTE}
{KEY_USAGE}

## About
{KEY_ABOUT}

{KEY_ARGS}

## Options
{KEY_OPTS}

{KEY_PARENT}

{KEY_CHILDREN}
";
	public HttpClient _client;
	public bool isAuthorized;

	public DocService()
	{
		_client = new HttpClient();
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
	}

	public async Task UploadGuides(GenerateDocsCommandArgs args)
	{
		var guides = GetGuides();

		var guidePublishes = new List<Task>();
		foreach (var guide in guides)
		{
			guidePublishes.Add(PublishDoc(new ReadmePostDocumentRequest
			{
				title = guide.title,
				order = guide.order,
				slug = guide.slug,
				excerpt = guide.excerpt,
				categorySlug = args.categorySlug,
				parentDocSlug = args.guideParentSlug,
				body = guide.content,
			}));
		}

		await Task.WhenAll(guidePublishes);
	}
	public List<DocGuideDescriptor> GetGuides()
	{
		var output = new List<DocGuideDescriptor>();
		var asm = Assembly.GetExecutingAssembly();
		var allResources = asm.GetManifestResourceNames();
		string prefix = $"cli.Docs.Guides";
		foreach (var resource in allResources)
		{
			if (!resource.StartsWith(prefix)) continue;

			using Stream stream = asm.GetManifestResourceStream(resource);
			using StreamReader reader = new StreamReader(stream);
			string pattern = @"cli\.Docs\.Guides\.(\d+)_(.+)\.md";
			var match = Regex.Match(resource, pattern);
			var sb = new StringBuilder();
			string excerpt = null;
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (excerpt == null)
				{
					excerpt = line;
				}
				else
				{
					sb.AppendLine(line);
				}
			}

			var content = sb.ToString();
			var order = match.Groups[1].Value;
			var title = match.Groups[2].Value;

			output.Add(new DocGuideDescriptor
			{
				order = int.Parse(order),
				title = title.Replace("-", " "),
				content = content,
				slug = "cli-guide-" + title.ToLower(),
				excerpt = excerpt
			});
		}

		return output;
	}

	public string GetLink(string category, BeamCommandDescriptor desc)
	{
		var slug = desc.GetSlug();
		return $"[{slug}](doc:{category}-{slug})";
	}

	public string GetUsage(BeamCommandDescriptor desc)
	{
		var sb = new StringBuilder();
		sb.Append("```shell\n");
		sb.Append(desc.executionPath);
		foreach (var arg in desc.command.Arguments)
		{
			if (arg.HasDefaultValue)
			{
				sb.Append($" [<{arg.HelpName ?? arg.Name}>]");
			}
			else
			{
				sb.Append($" <{arg.HelpName ?? arg.Name}>");
			}
		}

		sb.Append(" [options]");
		sb.Append("\n```");
		return sb.ToString();
	}

	public string GetParentSection(string category, BeamCommandDescriptor command)
	{
		if (command.parent == null || command.parent.GetSlug() == "beam")
		{
			return "";
		}

		var link = $"[{command.parent.GetSlug(" ")}](./{command.parent.GetName()}.md)";

		if (command.children.Count > 0)
		{
			link = $"[{command.parent.GetSlug(" ")}](../{command.parent.GetName()}.md)";
		}
		
		return @$"### Parent Command
{link}";
	}

	public string GetChildrenSection(string category, BeamCommandDescriptor command)
	{
		if (command.command.Subcommands.Count == 0)
		{
			return "";
		}

		var sb = new StringBuilder();
		sb.Append("### Sub Commands\n");
		foreach (var sub in command.children)
		{
			if (sub.children.Count > 0)
			{
				sb.AppendLine($"- [{sub.GetName()}]({sub.GetName()}/{sub.GetName()}.md)");

			}
			else
			{
				sb.AppendLine($"- [{sub.GetName()}](./{sub.GetName()}.md)");

			}
			// sb.Append($"{GetLink(category, sub)}\n");
		}
		return sb.ToString();
	}

	public string GetOptDescription(BeamCommandDescriptor command, CompiledDocDescriptor desc)
	{

		var argSb = new StringBuilder();
		var optionList = new List<Option>();
		var parent = command.parent;
		optionList.AddRange(command.command.Options);
		while (parent != null)
		{
			optionList.AddRange(parent.command.Options);
			parent = parent.parent;
		}

		for (var i = 0; i < optionList.Count; i++)
		{
			var opt = optionList[i];
			if (opt.IsHidden) continue;
			argSb.Append("|--");
			argSb.Append(opt.ArgumentHelpName ?? opt.Name);
			argSb.Append("|");
			if (opt.ValueType.IsGenericType)
			{
				if (opt.ValueType.IsAssignableTo(typeof(IEnumerable)))
				{
					argSb.Append("Set[" + opt.ValueType.GetGenericArguments()[0].Name + "]");
				}
				else
				{
					argSb.Append("unknown");
				}
			}
			else
			{
				argSb.Append(opt.ValueType.Name);
			}
			argSb.Append("|");
			var argDesc = opt.Description.ReplaceLineEndings("<br>");
			if (desc.TryGetMarkdown($"{{{{Opt_{opt.Name}}}}}", out var extra))
			{
				extra = extra.Replace("\n", " ")
					.Replace("\n\r", " ")
					.Replace("\r\n", " ");
				argDesc += $". {extra}";
			}

			argSb.Append(argDesc);
			argSb.Append("|");
			argSb.Append("\n");
		}
		var argSection = @$"
|Name|Type|Description|
|-|-|-|
{argSb}
";

		return argSection;
	}

	public string GetArgDescription(BeamCommandDescriptor command, CompiledDocDescriptor desc)
	{

		if (command.command.Arguments.Count == 0)
		{
			return "";
		}
		var argSb = new StringBuilder();
		for (var i = 0; i < command.command.Arguments.Count; i++)
		{
			var arg = command.command.Arguments[i];
			argSb.Append("|");
			argSb.Append(arg.HelpName ?? arg.Name);
			argSb.Append("|");
			argSb.Append(arg.ValueType.Name);
			argSb.Append("|");
			var argDesc = arg.Description;
			if (desc.TryGetMarkdown($"{{{{Arg_{arg.Name}}}}}", out var extra))
			{
				extra = extra.Replace("\n", " ")
					.Replace("\n\r", " ")
					.Replace("\r\n", " ");
				argDesc += $". {extra}";
			}

			argSb.Append(argDesc);
			argSb.Append("|");
			argSb.Append("\n");
		}
		var argSection = @$"## Arguments 
|Name|Type|Description|
|-|-|-|
{argSb}
";

		return argSection;
	}

	public string Render(BeamCommandDescriptor commandDesc)
	{
		return Render("_", commandDesc, (KEY_TITLE, commandDesc.executionPath.Substring("beam".Length).Trim()),
			(KEY_DESC, commandDesc.command.Description));
	}
	public string Render(string docCategory, BeamCommandDescriptor command, params (string, string)[] extraVars)
	{
		var desc = Parse(command);
		var variables = extraVars.ToList();

		var rendered = DocTemplate;


		void Fill(string key, string value)
		{
			rendered = rendered.Replace(key, value);
		}

		void FillRequired(string key)
		{
			rendered = rendered.Replace(key, desc.GetRequiredMarkdown(key));
		}

		void FillOptional(string key)
		{
			if (!desc.TryGetMarkdown(key, out var content))
			{
				content = "";
			}

			rendered = rendered.Replace(key, content);
		}


		FillRequired(KEY_ABOUT);
		FillOptional(KEY_USAGE_NOTE);
		Fill(KEY_USAGE, GetUsage(command));

		Fill(KEY_ARGS, GetArgDescription(command, desc));
		Fill(KEY_OPTS, GetOptDescription(command, desc));
		Fill(KEY_PARENT, GetParentSection(docCategory, command));
		Fill(KEY_CHILDREN, GetChildrenSection(docCategory, command));

		foreach (var variable in variables)
		{
			var loweredForm = variable.Item1.ToLower();
			var normalForm = variable.Item1.ToLower().Capitalize();
			var upperForm = variable.Item1.ToUpper();

			rendered = rendered.Replace($"{{{{{loweredForm}}}}}", variable.Item2);
			rendered = rendered.Replace($"{{{{{normalForm}}}}}", variable.Item2);
			rendered = rendered.Replace($"{{{{{upperForm}}}}}", variable.Item2);
		}

		return rendered;
	}

	public bool GetDocsFile(BeamCommandDescriptor descriptor, out string content)
	{
		content = null;
		string resourceName = $"cli.Docs.Commands.{descriptor.GetSlug().ToLower()}.md";

		Assembly assembly = Assembly.GetExecutingAssembly();
		using Stream stream = assembly.GetManifestResourceStream(resourceName);
		if (stream == null)
		{
			return false;
		}
		using StreamReader reader = new StreamReader(stream);

		content = reader.ReadToEnd();
		return true;
	}

	public CompiledDocDescriptor Parse(BeamCommandDescriptor command)
	{
		if (!GetDocsFile(command, out var file))
		{
			return new CompiledDocDescriptor
			{
				titleToMarkdown = new Dictionary<string, string>
				{
					["About"] = command.command.Description
				}
			};
		}
		var doc = Markdown.Parse(file);

		var titleToMarkdown = new Dictionary<string, string>();
		HeadingBlock latestHeader = null;
		foreach (var block in doc)
		{
			if (block is HeadingBlock heading && heading.Level == 1)
			{
				if (latestHeader != null)
				{
					var title = latestHeader.GetTitle(file);
					var content = file.Substring(latestHeader.Span.End + 1, (block.Span.Start - latestHeader.Span.End) - 1);
					titleToMarkdown.Add(title, content);
				}

				latestHeader = heading;
			}
		}
		if (latestHeader != null)
		{
			var title = latestHeader.GetTitle(file);
			var content = file.Substring(latestHeader.Span.End + 1);
			titleToMarkdown.Add(title, content);
		}

		// clean leading and trailing white space
		foreach (var key in titleToMarkdown.Keys)
		{
			titleToMarkdown[key] = titleToMarkdown[key].Trim();
		}

		return new CompiledDocDescriptor
		{
			titleToMarkdown = titleToMarkdown
		};
	}


	public async Task DeleteDoc(DocDesc desc)
	{
		var res = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"https://dash.readme.com/api/v1/docs/{desc.slug}"));
		if (!res.IsSuccessStatusCode)
		{
			Log.Error($"failed to delete {desc.slug} status=[{res.StatusCode}] {await res.Content.ReadAsStringAsync()}");
		}
	}

	public async Task<bool> DoesDocExist(ReadmePostDocumentRequest desc)
	{

		var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://dash.readme.com/api/v1/docs/{desc.slug}"));
		return response.StatusCode == HttpStatusCode.OK;
	}

	public async Task PublishDoc(ReadmePostDocumentRequest desc)
	{
		if (!isAuthorized)
		{
			return;
		}
		var exists = await DoesDocExist(desc);

		var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
		var json = JsonConvert.SerializeObject(desc, settings);
		var req = new StringContent(json, Encoding.UTF8, "application/json");

		var method = exists ? HttpMethod.Put : HttpMethod.Post;
		var endpoint = exists
			? $"https://dash.readme.com/api/v1/docs/{desc.slug}"
			: "https://dash.readme.com/api/v1/docs";
		var msg = new HttpRequestMessage(method, endpoint);
		msg.Content = req;

		var response = await _client.SendAsync(msg);
		if (!response.IsSuccessStatusCode)
		{
			Log.Error(
				$"Failed to submit new doc! {desc.slug} status=[{response.StatusCode}] reason=[{response.ReasonPhrase}] raw=[{await response.Content.ReadAsStringAsync()}]");
		}

	}

	public DocDesc GenerateDocFile(BeamCommandDescriptor commandDesc, GenerateDocsCommandArgs args)
	{
		var rendered = Render(args.categorySlug, commandDesc,
			(KEY_TITLE, commandDesc.executionPath.Substring("beam".Length).Trim()),
			(KEY_DESC, commandDesc.command.Description)
		);

		var path = commandDesc.executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string slug = "beam";
		if (path.Length > 1)
		{
			slug = string.Join("-", path.Skip(1));
		}

		var doc = new DocDesc
		{
			excerpt = commandDesc.command.Description,
			title = commandDesc.ExecutionPathAsCapitalizedStringWithoutBeam(" "),
			markdownContent = rendered,
			slug = $"{args.categorySlug}-{slug}",
			categorySlug = args.categorySlug, // TODO: pull from option
			parentSlug = args.commandParentSlug
		};

		return doc;
	}

	public void SetReadmeAuth(string argsReadmeApiKey, string readmeVersion)
	{
		isAuthorized = !string.IsNullOrEmpty(argsReadmeApiKey);
		_client.DefaultRequestHeaders.Add("Authorization", $"Basic {argsReadmeApiKey}");
		_client.DefaultRequestHeaders.Add("x-readme-version", readmeVersion);
	}
}

public class CompiledDocDescriptor
{
	public Dictionary<string, string> titleToMarkdown = new Dictionary<string, string>();

	public string GetRequiredMarkdown(string key)
	{
		if (!TryGetMarkdown(key, out var markdown))
		{
			throw new CliException($"Cannot find required key=[{key}] for docs.");
		}

		return markdown;
	}

	public bool TryGetMarkdown(string key, out string markdown)
	{
		foreach (var kvp in titleToMarkdown)
		{
			var candidate = kvp.Key.ToLower();
			candidate = $"{{{{{candidate.ToLower()}}}}}";
			if (candidate == key.ToLower())
			{
				markdown = kvp.Value;
				return true;
			}
		}

		markdown = null;
		return false;
	}

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
	public int? order;
}
public class DocGuideDescriptor
{
	public int order;
	public string title;
	public string slug;
	public string excerpt;
	public string content;
}

public static class MarkdownExtensions
{
	public static string GetContent(this Block block, string raw)
	{
		return raw.Substring(block.Span.Start, block.Span.Length);
	}

	public static string GetTitle(this HeadingBlock title, string raw)
	{
		return title.GetContent(raw).Replace(title.HeaderChar.ToString(), "").Trim();
	}
}
