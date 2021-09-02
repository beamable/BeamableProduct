

   using System;
   using System.Collections.Generic;
   using System.Linq;
   using Beamable.Server;
   using Beamable.Server.Editor;
   using Beamable.Server.Editor.ManagerClient;
   using Beamable.Server.Editor.DockerCommands;
   using Beamable.Server.Editor.UI;
   using Beamable.Server.Editor.Uploader;
   using Beamable.Config;
   using Beamable.Platform.SDK;
   using Beamable.Editor;
   using Beamable.Editor.Environment;
   using UnityEditor;
   using UnityEngine;

   namespace Beamable.Server.Editor
   {
      public class DebugWindow : CommandRunnerWindow
      {

          #if !BEAMABLE_NEWMS
         [MenuItem(
           BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
           BeamableConstants.OPEN + " " +
           BeamableConstants.MICROSERVICES_MANAGER,
           priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2)]
         #endif
         public static void Init()
         {
            var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

            var checkCommand = new CheckDockerCommand();
            DebugWindow wnd = GetWindow<DebugWindow>("Microservices Manager", true, inspector);
            checkCommand.Start(wnd).Then(installed =>
            {
               wnd.initialized = true;
               wnd.Show();
            });
         }

         private bool initialized;


         //private List<MicroserviceDescriptor> _descriptors = new List<MicroserviceDescriptor>();

         private Vector2 _scrollPosition;
         private Dictionary<MicroserviceDescriptor, bool> _debuggerInfoOpen =
            new Dictionary<MicroserviceDescriptor, bool>();

         private void OnGUI()
         {

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (GUILayout.Button("PRINT STATUS"))
            {
               EditorAPI.Instance.Then(de =>
               {
                  de.GetMicroserviceManager().GetStatus().Then(status =>
                  {
                     Debug.Log($"----- status ----- isCurrent=[{status.isCurrent}]");
                     foreach (var service in status.services)
                     {
                        Debug.Log(
                           $"service=[{service.serviceName}] running=[{service.running}] imageId=[{service.imageId}] isCurrent=[{service.isCurrent}]");
                     }
                  });
               });
            }

            if (GUILayout.Button("GET MANIFEST"))
            {
               EditorAPI.Instance.Then(de =>
               {
                  de.GetMicroserviceManager().GetCurrentManifest().Then(
                     manifest => { Debug.Log("manifest " + manifest); });
               });

            }

            if (GUILayout.Button("GET ALL MANIFEST"))
            {
               EditorAPI.Instance.Then(de =>
               {
                  de.GetMicroserviceManager().GetManifests().Then(res =>
                  {
                     res.ForEach(s => Debug.Log(string.Join(",", s.manifest.Select(m => m.serviceName))));
                  });
               });
            }

            if (GUILayout.Button("WRITE EMPTY MANIFEST"))
            {
               EditorAPI.Instance.Then(de =>
               {
                  var service = de.GetMicroserviceManager();
                  service.Deploy(new ServiceManifest());
               });
            }

            if (GUILayout.Button("WRITE ACTUAL MANIFEST"))
            {
               var wnd = DeployWindow.ShowDeployPopup();
            }


            if (GUILayout.Button("REFRESH") || Microservices.Descriptors == null)
            {
               //Microservices.Descriptors = Microservices.ListMicroservices().ToList();
            }

#if BEAMABLE_DEVELOPER
            if (GUILayout.Button("BUILD BEAMSERVICE"))
            {
               var command = new BuildBeamServiceCommand();
               command.Start();
            }
#endif
            var style = new GUIStyle(EditorStyles.label);
            style.wordWrap = true;

            // If we know docker is installed; continue. If we find that Docker isn't installed, show something...
            if (DockerCommand.DockerNotInstalled)
            {
               EditorGUILayout.SelectableLabel("Docker is not installed. https://docs.docker.com/get-docker/", style);
               if (GUILayout.Button("I've installed Docker"))
               {

                  initialized = false;
                  var check = new CheckDockerCommand();
                  check.Start(this).Then(installed =>
                  {
                     initialized = true;
                     if (!installed)
                     {
                        Debug.LogError("Docker was not detected. Make sure it is available on your System Path.");
                     }
                  });
               }

               EditorGUILayout.EndScrollView();
               return;
            }

            if (initialized)
            {
               foreach (var service in Microservices.Descriptors)
               {

                  GUILayout.Label(
                     service.ImageName + " // Assets/" + service.SourcePath.Substring(Application.dataPath.Length),
                     style);

                  var machine = Microservices.GetServiceStateMachine(service);


                  GUI.enabled = true;

//            if (GUILayout.Button("Upload container"))
//            {
//               var uploader = new ContainerUploadHarness(this);
//               //await uploader.UploadContainer(service);
//            }

                  if (GUILayout.Button("Open Deployed Swagger Docs"))
                  {
                     EditorAPI.Instance.Then(de =>
                     {
                        //http://localhost:10001/1323424830305280/games/DE_1323424830305283/realms/DE_1323424830305283/microservices/DeploymentTest/docs/remote/?
                        var url =
                           $"{BeamableEnvironment.PortalUrl}/{de.CidOrAlias}/games/{de.ProductionRealm.Pid}/realms/{de.Pid}/microservices/{service.Name}/docs/remote/?";
                        Application.OpenURL(url);
                     });
                  }

                  if (GUILayout.Button("Open Local Swagger Docs"))
                  {
                     EditorAPI.Instance.Then(de =>
                     {
                        //http://localhost:10001/1323424830305280/games/DE_1323424830305283/realms/DE_1323424830305283/microservices/DeploymentTest/docs/remote/?
                        var url =
                           $"{BeamableEnvironment.PortalUrl}/{de.CidOrAlias}/games/{de.ProductionRealm.Pid}/realms/{de.Pid}/microservices/{service.Name}/docs/{MicroserviceIndividualization.Prefix}/?";
                        Application.OpenURL(url);
                     });
                  }

                  if (GUILayout.Button("Force Client Rebuild"))
                  {
                     Microservices.GenerateClientSourceCode(service);
                  }

                  GUI.enabled = machine.CurrentState == MicroserviceState.IDLE;
                  machine.IncludeDebugTools = GUILayout.Toggle(machine.IncludeDebugTools, "Include Debugging Tools");
                  if (GUILayout.Button("Build"))
                  {
                     machine.MoveNext(MicroserviceCommand.BUILD);
                  }

                  GUI.enabled = machine.CurrentState == MicroserviceState.IDLE;
                  machine.UseDebug = GUILayout.Toggle(machine.UseDebug, "Use Debug");
                  if (GUILayout.Button("Start"))
                  {
                     machine.MoveNext(MicroserviceCommand.START);
                  }

                  GUI.enabled = machine.CurrentState == MicroserviceState.RUNNING;

                  bool foldoutOpen;
                  _debuggerInfoOpen.TryGetValue(service, out foldoutOpen);
                  var debuggerFoldout = EditorGUILayout.Foldout(foldoutOpen, "Debugger Connection Details");
                  _debuggerInfoOpen[service] = debuggerFoldout;
                  if (debuggerFoldout)
                  {
                     var config = MicroserviceConfiguration.Instance.GetEntry(service.Name);
                     EditorGUI.indentLevel++;
                     EditorGUILayout.LabelField("Use SSH to connect a debugger to the container.");
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.PrefixLabel("host");

                     EditorGUILayout.SelectableLabel("0.0.0.0", GUILayout.Height(EditorGUIUtility.singleLineHeight));
                     EditorGUILayout.EndHorizontal();
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.PrefixLabel("user");

                     EditorGUILayout.SelectableLabel(config.DebugData.Username,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                     EditorGUILayout.EndHorizontal();
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.PrefixLabel("password");

                     EditorGUILayout.SelectableLabel(config.DebugData.Password,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                     EditorGUILayout.EndHorizontal();
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.PrefixLabel("port");

                     EditorGUILayout.SelectableLabel("" + config.DebugData.SshPort,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                     EditorGUILayout.EndHorizontal();

                     EditorGUILayout.Space();
                     EditorGUILayout.LabelField(
                        "Open a VSCode window to the microservice root, and add this debug configuration.");
                     GUILayout.BeginHorizontal();
                     GUILayout.FlexibleSpace();
                     if (GUILayout.Button("Copy VSCode Configuration", GUILayout.Width(200)))
                     {
                        EditorGUIUtility.systemCopyBuffer = $@"{{
                     ""name"": ""Attach {service.Name}"",
                     ""type"": ""coreclr"",
                     ""request"": ""attach"",
                     ""processId"": ""${{command:pickRemoteProcess}}"",
                     ""pipeTransport"": {{
                        ""pipeProgram"": ""docker"",
                        ""pipeArgs"": [ ""exec"", ""-i"", ""{service.ContainerName}"" ],
                        ""debuggerPath"": ""/vsdbg/vsdbg"",
                        ""pipeCwd"": ""${{workspaceRoot}}"",
                        ""quoteArgs"": false
                     }},
                     ""sourceFileMap"": {{
                        ""/subsrc/"": ""${{workspaceRoot}}/""
                     }}
                  }}";
                     }

                     GUILayout.EndHorizontal();

                     /*
                      *         {
                  "name": "Attach DeploymentTest",
                  "type": "coreclr",
                  "request": "attach",
                  "processId": "${command:pickRemoteProcess}",
                  "pipeTransport": {
                      "pipeProgram": "docker",
                      "pipeArgs": [ "exec", "-i", "DeploymentTest_container" ],
                      "debuggerPath": "/vsdbg/vsdbg",
                      "pipeCwd": "${workspaceRoot}",
                      "quoteArgs": false
                  },
                  "sourceFileMap": {
                      "/subsrc/": "${workspaceRoot}/"
                  }
              }
                      */
                     EditorGUI.indentLevel--;
                  }


                  if (GUILayout.Button("Stop"))
                  {
                     machine.MoveNext(MicroserviceCommand.STOP);
                  }


                  GUI.enabled = true;

               }
            }

            EditorGUILayout.EndScrollView();
         }
      }
   }