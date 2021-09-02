using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
        public event OnConsoleLog OnLog;
        public event OnConsoleExecute OnExecute;
        public event OnCommandRegistered OnCommandRegistered;

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
