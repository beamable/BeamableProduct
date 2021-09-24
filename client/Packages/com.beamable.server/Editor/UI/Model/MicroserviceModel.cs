
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.Environment;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Model
{
    [System.Serializable]
    public class MicroserviceModel
    {
        public MicroserviceDescriptor Descriptor;
        // public MicroserviceStateMachine StateMachine;
        public MicroserviceBuilder Builder;
        public LogMessageStore Logs;
        public ServiceReference RemoteReference;
        public ServiceStatus RemoteStatus;
        public MicroserviceConfigurationEntry Config;
        public bool AreLogsAttached { get; private set; }= true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnSelectionChanged?.Invoke(value);
            }
        }

        public bool IsBuilding => Builder?.IsBuilding ?? false;
        public bool IsRunning => Builder?.IsRunning ?? false;
        public bool SameImageOnRemoteAndLocally => string.Equals(Builder?.LastBuildImageId, RemoteReference?.imageId);

        public Action<ServiceReference> OnRemoteReferenceEnriched;
        public Action<ServiceStatus> OnRemoteStatusEnriched;
        public Action OnLogsDetached;
        public Action OnLogsAttached;
        public Action<bool> OnLogsAttachmentChanged;
        public Action<bool> OnSelectionChanged;
        public Action OnSortChanged;

        public event Action<Task> OnBuildAndRestart;
        public event Action<Task> OnBuildAndStart;
        public event Action<Task> OnBuild;
        public event Action<Task> OnStart; // TODO: Currently it exposes us a moment of starting process and not exact moment when MS has started. Maybe rename it in future and add a proper one?? Luke
        public event Action<Task> OnStop;
        public event Action<Promise<Unit>> OnDockerLoginRequired;
        public Action<float, long, long> OnDeployProgress;

        private bool _isSelected;

        public Task BuildAndRestart()
        {
            var task = Builder.TryToBuildAndRestart(IncludeDebugTools);
            OnBuildAndRestart?.Invoke(task);
            return task;
        }

        public Task BuildAndStart()
        {
            var task = Builder.TryToBuildAndStart(IncludeDebugTools);
            OnBuildAndStart?.Invoke(task);
            return task;
        }

        public Task Build()
        {
            var task = Builder.TryToBuild(IncludeDebugTools);
            OnBuild?.Invoke(task);
            return task;
        }

        public Task Start()
        {
            var task = Builder.TryToStart();
            OnStart?.Invoke(task);
            return task;
        }

        public Task Stop()
        {
            var task = Builder.TryToStop();
            OnStop?.Invoke(task);
            return task;
        }

        public void OpenLocalDocs()
        {
            EditorAPI.Instance.Then(de =>
            {
                //http://localhost:10001/1323424830305280/games/DE_1323424830305283/realms/DE_1323424830305283/microservices/DeploymentTest/docs/remote/?
                var url =
                    $"{BeamableEnvironment.PortalUrl}/{de.CidOrAlias}/games/{de.ProductionRealm.Pid}/realms/{de.Pid}/microservices/{Descriptor.Name}/docs/{MicroserviceIndividualization.Prefix}/?";
                Application.OpenURL(url);
            });
        }

        public bool IncludeDebugTools
        {
            get => Config.IncludeDebugTools;
            set
            {
                Config.IncludeDebugTools = value;
                EditorUtility.SetDirty(MicroserviceConfiguration.Instance);
            }
        }

        public void DetachLogs()
        {
            if (!AreLogsAttached) return;

            AreLogsAttached = false;
            OnLogsDetached?.Invoke();
            OnLogsAttachmentChanged?.Invoke(false);
        }

        public void AttachLogs()
        {
            if (AreLogsAttached) return;
            AreLogsAttached = true;
            OnLogsAttached?.Invoke();
            OnLogsAttachmentChanged?.Invoke(true);
        }

        public void EnrichWithRemoteReference(ServiceReference remoteReference)
        {
            RemoteReference = remoteReference;
            OnRemoteReferenceEnriched?.Invoke(remoteReference);
        }

        public void EnrichWithStatus(ServiceStatus status)
        {
            RemoteStatus = status;
            OnRemoteStatusEnriched?.Invoke(status);
        }

        public void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
        {
            var existsOnRemote = RemoteStatus?.serviceName?.Length > 0;
            var hasImageSuffix = Builder.HasBuildDirectory ? string.Empty : " (Build first)";
            var localCategory = IsRunning ? "Local" : "Local (not running)";
            var remoteCategory = existsOnRemote ? "Cloud" : "Cloud (not deployed)";
            var debugToolsSuffix = IncludeDebugTools ? string.Empty : " (Debug tools disabled)";
            evt.menu.BeamableAppendAction($"Reveal build directory{hasImageSuffix}", pos =>
            {
                var full = Path.GetFullPath(Descriptor.BuildPath);
                EditorUtility.RevealInFinder(full);
            }, Builder.HasBuildDirectory);

            evt.menu.BeamableAppendAction($"Run Snyk Tests{hasImageSuffix}", pos => RunSnykTests(), Builder.HasImage);

            evt.menu.BeamableAppendAction($"{localCategory}/Open in CLI", pos => OpenInCli(), IsRunning);
            evt.menu.BeamableAppendAction($"{localCategory}/View Documentation", pos => OpenLocalDocs(), IsRunning);

            evt.menu.BeamableAppendAction($"{remoteCategory}/View Documentation", pos => {OpenOnRemote("docs/remote/");}, existsOnRemote);
            evt.menu.BeamableAppendAction($"{remoteCategory}/View Metrics", pos => {OpenOnRemote("metrics");}, existsOnRemote);
            evt.menu.BeamableAppendAction($"{remoteCategory}/View Logs", pos => {OpenOnRemote("logs");}, existsOnRemote);
            evt.menu.BeamableAppendAction($"Visual Studio Code/Copy Debug Configuration{debugToolsSuffix}", pos => { CopyVSCodeDebugTool(); }, IncludeDebugTools);
            if (MicroserviceConfiguration.Instance.Microservices.Count > 1) {
                evt.menu.BeamableAppendAction($"Order/Move Up", pos => {
                    MicroserviceConfiguration.Instance.MoveMicroserviceIndex(Name, -1);
                    OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetMicroserviceIndex(Name) > 0);
                evt.menu.BeamableAppendAction($"Order/Move Down", pos => {
                    MicroserviceConfiguration.Instance.MoveMicroserviceIndex(Name, 1);
                    OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetMicroserviceIndex(Name) < MicroserviceConfiguration.Instance.Microservices.Count - 1);
            }

            if (!AreLogsAttached)
            {
                evt.menu.BeamableAppendAction($"Reattach Logs", pos => AttachLogs());
            }
        }

        private void RunSnykTests()
        {
            var snykCommand = new SnykTestCommand(Descriptor);
            Debug.Log($"Starting Snyk Tests for {Descriptor.Name}. Please hold.");
            snykCommand.Start(null).Then(res =>
            {
                if (res.RequiresLogin)
                {
                    var onLogin = new Promise<Unit>();
                    onLogin.Then(_ => RunSnykTests()).Error(_ =>
                    {
                        Debug.Log("Cannot run Snyk Tests without being logged into DockerHub");
                    });
                    OnDockerLoginRequired?.Invoke(onLogin);

                }
                else
                {
                    Debug.Log(res.Output);
                    var date = DateTime.UtcNow.ToFileTimeUtc().ToString();
                    var filePath =
                        $"{Directory.GetParent(Application.dataPath)}{Path.DirectorySeparatorChar}{Descriptor.Name}-snyk-results-{date}.txt";

                    File.WriteAllText(filePath, res.Output);
                    EditorUtility.OpenWithDefaultApp(filePath);
                }
            });
        }

        private void CopyVSCodeDebugTool()
        {

            EditorGUIUtility.systemCopyBuffer =
$@"{{
     ""name"": ""Attach {Descriptor.Name}"",
     ""type"": ""coreclr"",
     ""request"": ""attach"",
     ""processId"": ""${{command:pickRemoteProcess}}"",
     ""pipeTransport"": {{
        ""pipeProgram"": ""docker"",
        ""pipeArgs"": [ ""exec"", ""-i"", ""{Descriptor.ContainerName}"" ],
        ""debuggerPath"": ""/vsdbg/vsdbg"",
        ""pipeCwd"": ""${{workspaceRoot}}"",
        ""quoteArgs"": false
     }},
     ""sourceFileMap"": {{
        ""/subsrc"": ""{Path.GetFullPath(Descriptor.BuildPath)}/""
     }}
  }}";
        }

        private void OpenOnRemote(string relativePath)
        {
            EditorAPI.Instance.Then(api =>
            {
                var path =
                    $"{BeamableEnvironment.PortalUrl}/{api.CidOrAlias}/" +
                    $"games/{api.ProductionRealm.Pid}/realms/{api.Pid}/" +
                    $"microservices/{Descriptor.Name}/{relativePath}?refresh_token={api.Token.RefreshToken}";
                Application.OpenURL(path);
            });
        }

        private void OpenInCli()
        {
            System.Diagnostics.Process GetProcess(string command)
            {
                var baseProcess = new System.Diagnostics.Process();
#if UNITY_EDITOR_WIN
                baseProcess.StartInfo.FileName = "cmd.exe";
                baseProcess.StartInfo.Arguments = $"/C {command}";
#else
                baseProcess.StartInfo.FileName = "sh";
                baseProcess.StartInfo.Arguments = $"-c \"{command}\"";
#endif
                return baseProcess;
            }
            var baseCommand =
                $"{MicroserviceConfiguration.Instance.DockerCommand} container exec -it {Descriptor.ContainerName} sh";
#if UNITY_EDITOR_WIN
            var process = GetProcess(baseCommand);
            process.Start();
#else
            var tmpPath = Path.Combine(FileUtil.GetUniqueTempPathInProject(), "..");

            tmpPath = Path.Combine(tmpPath, $"{Descriptor.ContainerName}_cli_shell");
            tmpPath = Path.GetFullPath(tmpPath);
            if(File.Exists(tmpPath))
                FileUtil.DeleteFileOrDirectory(tmpPath);

            using (var file = new StreamWriter(tmpPath, false, Encoding.UTF8)) {
                file.WriteLine("#!/bin/sh");
                file.WriteLine(baseCommand);
            }
            using (var process = GetProcess($"chmod +x {tmpPath}"))
            {
                process.Start();
            }
            using (var process = GetProcess($"open {tmpPath}"))
            {
                process.Start();
            }
#endif
        }

        // Chris took these out because they weren't being used yet, and were throwing warnings on package builds.
        // public event Action OnRenameRequested;
        // public event Action<MicroserviceModel> OnEnriched;
        // private string _name = "";

        public string Name
        {
            set
            {
                throw new NotImplementedException("Cannot rename services yet.");
                // if (string.Equals(_name, value)) return;
                // if (string.IsNullOrWhiteSpace(value)) throw new Exception("Name cannot be empty.");
                // var oldName = _name;
                // try
                // {
                //     _name = value;
                //     OnRenamed?.Invoke(this);
                // }
                // catch (Exception)
                // {
                //     _name = oldName; // clean up the name
                //     throw;
                // }
            }
            get { return Descriptor.Name; }
        }

    }


}
