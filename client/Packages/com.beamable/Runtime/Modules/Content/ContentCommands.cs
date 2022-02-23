using Beamable.Console;
using Beamable.ConsoleCommands;
using System;
using System.Text;

namespace Beamable.Modules.Content
{
	[BeamableConsoleCommandProvider]
	public class ContentCommands
	{
		[BeamableConsoleCommand("GET_CONTENT", "Get specific content", "GET_CONTENT <contentId>")]
		public string GetContent(string[] args)
		{
			if (args.Length < 1 || args.Length > 1)
			{
				return "You need to provide a <contentId>.";
			}

			API.Instance.Then(api =>
			{
				var contentID = args[0];
				var result = string.Empty;

				api.ContentService.GetContent(contentID).Then(contentObject =>
				{
					result += contentObject == null
						? $"Content for given id \"{contentID}\" does not exist."
						: $"{contentObject.ToJson()}\n";
				});

				ConsoleFlow.Instance.Log(result);
			});

			return string.Empty;
		}

		[BeamableConsoleCommand("LIST_CONTENT", "List manifest content", "LIST_CONTENT <startIndex(?)> <filter(?)> <namespaceId(?)>")]
		public string ListContent(string[] args)
		{
			const int elementsLimit = 50;
			var filter = String.Empty;
			var namespaceId = String.Empty;
			var startIndex = 0;
			var result = new StringBuilder();

			API.Instance.Then(api =>
			{
				namespaceId = api.ContentService.CurrentDefaultManifestID;
				SetParameters();

				api.ContentService.GetManifest(filter, namespaceId).Then(manifest =>
				{
					result.Append("\nContent list of \"{namespaceId}\" namespace:\n\n");
					if (manifest.entries.Count == 0)
					{
						result.Append("Content list is empty.");
					}

					var amount = Math.Min(startIndex + elementsLimit, manifest.entries.Count);
					for (var index = startIndex; index < amount; index++)
					{
						var content = manifest.entries[index];
						result.AppendFormat("{0} [{1}]\n", content.contentId, content.version);
					}

					ConsoleFlow.Instance.Log(result.ToString());
				});
			});
			return string.Empty;

			void TrySetIndexFromString(string arg)
			{
				if (int.TryParse(arg, out var newStartIndex))
				{
					startIndex = newStartIndex;
				}
				else
				{
					result.AppendFormat("Cannot parse {0} as start index", arg);
				}
			}

			void SetParameters()
			{
				for (var index = 0; index < args.Length; index++)
				{
					var arg = args[index];
					var splitted = arg.Split('=');

					if (splitted.Length == 1)
					{
						switch (index)
						{
							case 0:
								TrySetIndexFromString(arg);
								break;
							case 1:
								filter = arg;
								break;
							case 2:
								namespaceId = arg;
								break;
						}
						continue;
					}

					switch (splitted[0])
					{
						case "startIndex":
							TrySetIndexFromString(splitted[1]);
							break;
						case "filter":
							filter = splitted[1];
							break;
						case "namespaceId":
							namespaceId = splitted[1];
							break;
					}
				}
			}
		}

		[BeamableConsoleCommand("CONTENT_NAMESPACE", "Current content namespace", "CONTENT_NAMESPACE")]
		public string ContentNamespace(string[] args)
		{
			API.Instance.Then(api =>
			{
				var currentNamespace = api.ContentService.CurrentDefaultManifestID;
				ConsoleFlow.Instance.Log(currentNamespace);
			});
			return string.Empty;
		}

		[BeamableConsoleCommand("SET_CONTENT_NAMESPACE", "Set content namespace", "SET_CONTENT_NAMESPACE <namespaceId>")]
		public string SetContentNamespace(string[] args)
		{
			if (args.Length < 1 || args.Length > 1)
			{
				return "You need to provide a <namespaceId>";
			}

			API.Instance.Then(api =>
			{
				var newNamespaceId = args[0];
				var oldNamespaceId = api.ContentService.CurrentDefaultManifestID;

				api.ContentService.SwitchDefaultManifestID(newNamespaceId);

				var result = oldNamespaceId != api.ContentService.CurrentDefaultManifestID ?
					$"Namespace switched from \"{oldNamespaceId}\" to \"{newNamespaceId}\"" :
					$"Can't switch namespace from \"{oldNamespaceId}\" to \"{newNamespaceId}\". Check if given namespace exists.";

				ConsoleFlow.Instance.Log(result);
			});

			return string.Empty;
		}
	}
}
