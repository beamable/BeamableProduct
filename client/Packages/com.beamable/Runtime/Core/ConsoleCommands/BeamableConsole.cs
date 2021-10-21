using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using UnityEngine;

namespace Beamable.ConsoleCommands
{
    public delegate string ConsoleCommandCallback(string[] args);

    public delegate Task<string> ConsoleCommandAsyncCallback(string[] args);

    public delegate void OnConsoleLog(string message);

    public delegate string OnConsoleExecute(string command, string[] args);

    public delegate void OnCommandRegistered(BeamableConsoleCommandAttribute command, ConsoleCommandCallback callback);

    public class BeamableConsole
    {
        public struct ConsoleCommand
        {
            public string OriginTypeName;
            public string OriginMethodName;
            public BeamableConsoleCommandAttribute Command;
            public ConsoleCommandCallback Callback;
        }
        
        public class CommandCache : IReflectionCacheUserSystem
        {
            public List<Type> TypesOfInterest => new List<Type>();
            public List<Type> AttributesOfInterest => new List<Type> {typeof(BeamableConsoleCommandProviderAttribute)};
            
            public readonly Dictionary<string, string> commandOrigin = new Dictionary<string, string>();
            public readonly Dictionary<string, ConsoleCommand> consoleCommandsByName = new Dictionary<string, ConsoleCommand>();

            public void OnTypeCachesLoaded(Dictionary<Type, List<Type>> perBaseTypeCache, Dictionary<Type, List<(Type gameMakerType, Attribute attribute)>> perAttributeTypeCache)
            {
                // Do nothing when the entire caches are loaded.
            }
            
            public void OnTypeOfInterestCacheLoaded(Type typeOfInterest, List<Type> typeOfInterestSubTypes)
            {
                // Do nothing with types of interest since we have none for console commands
            }

            public void OnAttributeOfInterestCacheLoaded(Type attributeOfInterestType, List<(Type gameMakerType, Attribute attribute)> typesWithAttributeOfInterest)
            {
                // Assuming attributeOfInterestType == typeof(BeamableConsoleCommandProviderAttribute)
                foreach (var (gameMakerType, attribute) in typesWithAttributeOfInterest)
                {
                    // This is guaranteed to not be null, since we validate this when we build the Attribute Cache. 
                    var providerConstructor = gameMakerType.GetConstructor(BeamableConsoleCommandProviderAttribute.EmptyTypeArray);
                    var providerInstance = providerConstructor.Invoke(BeamableConsoleCommandProviderAttribute.EmptyObjectArray);

                    // Get instance methods that have the attribute, discard methods missing attributes as we don't really care about them in this case.
                    var instanceMethods = ReflectionCache.GatherInstanceMethodsWithAttributes<BeamableConsoleCommandAttribute>(gameMakerType, out _);
                    
                    // Handle instance methods
                    var instanceCommands = instanceMethods.Select((mapping) =>
                    {
                        var (method, attr) = mapping;

                        return new ConsoleCommand()
                        {
                            OriginTypeName = gameMakerType.Name,
                            OriginMethodName = method.Name,
                            Command = attr,
                            Callback = (ConsoleCommandCallback) Delegate.CreateDelegate(typeof(ConsoleCommandCallback), providerInstance, method, false)
                        };
                    });

                    // Find all invalid instance commands
                    var invalidCommands = instanceCommands.Where(cmd => cmd.Callback == null);

                    // Keep only valid commands
                    instanceCommands = instanceCommands.Where(cmd => cmd.Callback != null);

                    // Get static methods that have the attribute, discard methods missing attributes as we don't really care about them in this case
                    var staticMethods = ReflectionCache.GatherStaticMethodsWithAttributes<BeamableConsoleCommandAttribute>(gameMakerType, out _);
                    var staticCommands = staticMethods.Select(mapping =>
                    {
                        var (method, attr) = mapping;

                        return new ConsoleCommand()
                        {
                            Command = attr,
                            Callback = (ConsoleCommandCallback) Delegate.CreateDelegate(typeof(ConsoleCommandCallback), method, false)
                        };
                    });
                    
                    // Add invalid static method commands to the list of invalid commands
                    invalidCommands = invalidCommands.Union(staticCommands.Where(cmd => cmd.Callback == null));

                    // Keep only the valid commands
                    staticCommands = staticCommands.Where(cmd => cmd.Callback != null);

                    var allCommands = staticCommands.Union(instanceCommands).ToList();
                    var commandNameCollisions = new Dictionary<string, List<ConsoleCommand>>();
                    foreach (var consoleCommand in allCommands)
                    {
                        foreach (var name in consoleCommand.Command.Names)
                        {
                            var lowercaseName = name.ToLower();
                            if (consoleCommandsByName.ContainsKey(lowercaseName))
                            {
                                var collidedName = name;

                                if (!commandNameCollisions.TryGetValue(collidedName, out var collidedCommands))
                                {
                                    // Initialize collided name and add registered command to the list of collided commands.
                                    collidedCommands = new List<ConsoleCommand>();
                                    collidedCommands.Add(consoleCommandsByName[lowercaseName]);
                                    
                                    commandNameCollisions.Add(collidedName, collidedCommands);
                                }
                                
                                // Add current command being evaluated to the list of collided commands. 
                                collidedCommands.Add(consoleCommand);
                            }
                            else
                            {
                                commandOrigin.Add(name, $"Type=[{consoleCommand.OriginTypeName}] method=[#{consoleCommand.OriginMethodName}]");    
                                consoleCommandsByName.Add(lowercaseName, consoleCommand);
                            }
                        }
                    }

                    var invalidSignatureCommandsList = invalidCommands.ToList();
                    if (invalidSignatureCommandsList.Count > 0)
                    {
                        var invalidSignatureMessage = new StringBuilder();
                        invalidSignatureMessage.AppendLine("Console Commands must match the following signature: string CommandMethod(string[] args).");
                        invalidSignatureMessage.AppendLine("Please adjust the methods to match one of the supported signatures.");
                        foreach (var cmd in invalidSignatureCommandsList)
                        {
                            var invalidSignaturePath = $"{cmd.OriginTypeName}.{cmd.OriginMethodName}";
                            invalidSignatureMessage.AppendLine(invalidSignaturePath);
                        }
                        
                        BeamableLogger.LogError(invalidSignatureMessage.ToString());
                    }
                    

                    if (commandNameCollisions.Count > 0)
                    {
                        var commandCollisionMessage = new StringBuilder();
                        commandCollisionMessage.AppendLine($"The following commands failed to register due to a command name collision.");
                        
                        foreach (var commandNameCollision in commandNameCollisions)
                        {
                            var name = commandNameCollision.Key;
                            var collisionPaths = string.Join(", ", commandNameCollision.Value.Select(cmd => $"{cmd.OriginTypeName}.{cmd.OriginMethodName}"));

                            commandCollisionMessage.AppendLine($"{name} => {collisionPaths}");
                        }

                        commandCollisionMessage.AppendLine();
                        commandCollisionMessage.AppendLine("Please give each command unique names!");

                        BeamableLogger.LogError(commandCollisionMessage.ToString());
                    }
                }
            }

        }
        
        
        public event OnConsoleLog OnLog;
        public event OnConsoleExecute OnExecute;
        public event OnCommandRegistered OnCommandRegistered;

        public static CommandCache ReflectionCommandCache = new CommandCache();

        private static BeamableConsole commandInstance;
        private bool asyncCommandInProcess = false;
        public string scriptCommandReturn = String.Empty;

        private Dictionary<string, string> _commandOrigin = new Dictionary<string, string>();

        public static bool AsyncCommandInProcess
        {
            get => commandInstance.asyncCommandInProcess;
            set => commandInstance.asyncCommandInProcess = value;
        }

        public BeamableConsole()
        {
            commandInstance = this;
        }

        public void LoadCommands()
        {
            var emptyTypeArray = new Type[] { };
            var emptyObjectArray = new object[] { };

            _commandOrigin = new Dictionary<string, string>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.GetCustomAttribute<BeamableConsoleCommandProviderAttribute>(false) == null)
                    {
                        continue;
                    }

                    var instance = ResolveInstance(type);
                    if (instance == null)
                    {
                        continue;
                    }

                    var instanceMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var staticMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    ProcessMethods(instanceMethods, (method) => (ConsoleCommandCallback) Delegate.CreateDelegate(typeof(ConsoleCommandCallback), instance, method, false));
                    ProcessMethods(staticMethods, (method) => (ConsoleCommandCallback) Delegate.CreateDelegate(typeof(ConsoleCommandCallback), method, false));
                }
            }

            object ResolveInstance(Type type)
            {
                if (type == null)
                {
                    Debug.LogError($"Cannot resolve null type");
                    return null;
                }
                var emptyConstructor = type.GetConstructor(emptyTypeArray);
                if (emptyConstructor == null)
                {
                    Debug.LogError($"Console Command Provider must have an empty constructor. type=[{type.Name}]");
                    return null;
                }
                return emptyConstructor.Invoke(emptyObjectArray);
            }

            void ProcessMethods(IEnumerable<MethodInfo> methods,
                Func<MethodInfo, ConsoleCommandCallback> callbackCreator)
            {
                foreach (var method in methods)
                {

                    var attribute = method.GetCustomAttribute<BeamableConsoleCommandAttribute>();
                    if (attribute == null)
                    {
                        continue;
                    }

                    var callback = callbackCreator(method);
                    if (callback == null)
                    {
                       Debug.LogError(
                          $"Console Command must accept a string[], and return a string. type=[{method.DeclaringType.Name}] method=[{method.Name}]");
                       continue;
                    }

                    try
                    {
                        foreach (var name in attribute.Names)
                        {
                            _commandOrigin.Add(name, $"Type=[{method.DeclaringType.Name}] method=[#{method.Name}]");
                        }
                        OnCommandRegistered?.Invoke(attribute, callback);
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogError($"Command failed to register due to argument exception. Perhaps the command has already been registered. command=[{string.Join(",", attribute.Names)}]");
                    }
                }
            }
        }

        public void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        public void LogFormat(string line, params object[] args)
        {
            Log(string.Format (line, args));
        }

        public string Help(params string[] args)
        {
            return Execute("help", args);
        }

        public string Execute(string command, params string[] args)
        {
            return OnExecute?.Invoke(command, args);
        }

        public string Origin(string command)
        {
            var key = command.ToUpper();
            if (_commandOrigin.ContainsKey(key))
            {
                return _commandOrigin[key];
            }

            return $"{command} not found in BeamableCommandAttribute registrations";
        }
    }
}
