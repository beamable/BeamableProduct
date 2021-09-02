using System;
using Beamable;
using Beamable.Console;
using Beamable.ConsoleCommands;

namespace Modules.Content
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
        
        [BeamableConsoleCommand("LIST_CONTENT", "List manifest content", "LIST_CONTENT <filter(?)> <namespaceId(?)>")]
        public string ListContent(string[] args)
        {
            var filter = String.Empty;
            var namespaceId = String.Empty;
            var result = string.Empty;
            
            API.Instance.Then(api =>
            {
                namespaceId = api.ContentService.CurrentDefaultManifestID;
                SetParameters();

                api.ContentService.GetManifest(filter, namespaceId).Then(manifest =>
                {
                    result += $"\nContent list of \"{namespaceId}\" namespace:\n\n";
                    if (manifest.entries.Count == 0)
                    {
                        result += $"Content list is empty.";
                    }

                    foreach (var content in manifest.entries)
                    {
                        result += $"{content.contentId} [{content.version}]\n";
                    }

                    ConsoleFlow.Instance.Log(result);
                });
            });
            return string.Empty;

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
                                filter = arg;
                                break;
                            case 1:
                                namespaceId = arg;
                                break;
                        }
                        continue;
                    }
                    
                    switch (splitted[0])
                    {
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