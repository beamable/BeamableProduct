using System;
using System.Collections.Generic;
using System.IO;
using Beamable.Config;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beamable.Server.Editor
{
    [InitializeOnLoad]
    public static class MicroserviceEditor
    {
        public const int portCounter = 3000;

        public static string commandoutputfile = "";
        public static bool isVerboseOutput = false;
        public static bool wasCompilerError = true;
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
   public static string dockerlocation = "/usr/local/bin/docker";
#else
        public static string dockerlocation = "docker";
#endif

        private const string MENU_TOGGLE_AUTORUN =
            BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_MICROSERVICES + "/Auto Run Local Microservices";

        private const int MENU_TOGGLE_PRIORITY = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3;

        public const string CONFIG_AUTO_RUN = "auto_run_local_microservices";
        private const string TEMPLATE_MICROSERVICE_DIRECTORY = "Packages/com.beamable.server/Template";
        private const string DESTINATION_MICROSERVICE_DIRECTORY = "Assets/Beamable/Microservices";

        private const string TEMPLATE_STORAGE_OBJECT_DIRECTORY = "Packages/com.beamable.server/Template/StorageObject";
        private const string DESTINATION_STORAGE_OBJECT_DIRECTORY = "Assets/Beamable/StorageObjects";

        private static Dictionary<ServiceType, ServiceCreateInfo> _serviceCreateInfos =
            new Dictionary<ServiceType, ServiceCreateInfo>
            {
                {
                    ServiceType.MicroService,
                    new ServiceCreateInfo(ServiceType.MicroService, DESTINATION_MICROSERVICE_DIRECTORY, TEMPLATE_MICROSERVICE_DIRECTORY)
                },
                {
                    ServiceType.StorageObject,
                    new ServiceCreateInfo(ServiceType.StorageObject, DESTINATION_STORAGE_OBJECT_DIRECTORY, TEMPLATE_STORAGE_OBJECT_DIRECTORY)
                }
            };

        static MicroserviceEditor()
        {
            /// Delaying until first editor tick so that the menu
            /// will be populated before setting check state, and
            /// re-apply correct action
            EditorApplication.delayCall += () =>
            {
                var enabled = false;
                if (ConfigDatabase.HasKey(CONFIG_AUTO_RUN))
                    enabled = ConfigDatabase.GetBool(CONFIG_AUTO_RUN, false);
                else
                    enabled = EditorPrefs.GetBool(CONFIG_AUTO_RUN, false);

                setAutoRun(enabled);
            };
        }

        private static void setAutoRun(bool value)
        {
            Menu.SetChecked(MENU_TOGGLE_AUTORUN, value);
            if (ConfigDatabase.HasKey(CONFIG_AUTO_RUN)) ConfigDatabase.SetBool(CONFIG_AUTO_RUN, value);

            EditorPrefs.SetBool(CONFIG_AUTO_RUN, value);
        }

        [MenuItem(MENU_TOGGLE_AUTORUN, priority = MENU_TOGGLE_PRIORITY)]
        public static void AutoRunLocalMicroservicesToggle()
        {
            var enabled = EditorPrefs.GetBool(CONFIG_AUTO_RUN, false);
            setAutoRun(!enabled);
        }

        #region NewMicroService

        public static void CreateNewMicroservice(string microserviceName)
        {
            CreateNewServiceFile(ServiceType.MicroService, microserviceName);

//             var rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
//             var servicePath = Path.Combine(rootPath, DESTINATION_MICROSERVICE_DIRECTORY, microserviceName);
//             //string serviceEditorPath = Path.Combine(servicePath, "Editor");
//             var serviceHiddenPath = Path.Combine(servicePath, "~");
//             var serviceDockerPath = Path.Combine(servicePath, "dbmicroservice");
//             var templateHiddenPath = Path.Combine(rootPath, TEMPLATE_MICROSERVICE_DIRECTORY, "~");
//
//             var templateDirectory =
//                 new DirectoryInfo(Path.Combine(rootPath, TEMPLATE_MICROSERVICE_DIRECTORY, "dbmicroservice"));
//
//             var msDirectory = Directory.CreateDirectory(servicePath);
//             var msHiddenDirectory = Directory.CreateDirectory(serviceHiddenPath);
// //         DirectoryInfo msEditorDirectory = Directory.CreateDirectory(serviceEditorPath);
//             var templateHiddenDirectory = new DirectoryInfo(templateHiddenPath);


            //Editor file
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(rootPath, TemplateDirectory, "Editor/XXXXEditor.cs"),
//            msEditorDirectory.FullName + $"/{microserviceName}Editor.cs");
//
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(rootPath, TemplateDirectory, "Editor/Unity.Beamable.Editor.UserMicroService.XXXX.asmdef"),
//            msEditorDirectory.FullName + $"/Unity.Beamable.Editor.UserMicroService.{microserviceName}.asmdef");


            // ACTUAL SOURCE.
            // CreateNewFileWithMicroserviceInfo(microserviceName,
            //     Path.Combine(rootPath, TEMPLATE_MICROSERVICE_DIRECTORY,
            //         "Unity.Beamable.Runtime.UserMicroService.XXXX.asmdef"),
            //     msDirectory.FullName + $"/Unity.Beamable.Runtime.UserMicroService.{microserviceName}.asmdef");
            //
            // CreateNewFileWithMicroserviceInfo(microserviceName,
            //     Path.Combine(rootPath, TEMPLATE_MICROSERVICE_DIRECTORY, "Microservice.cs"),
            //     msDirectory.FullName + $"/{microserviceName}.cs");

            // HIDDEN SOURCE.
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(templateHiddenPath, "Dockerfile"),
//            msHiddenDirectory.FullName + $"/Dockerfile");
//
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(templateHiddenPath, "Program.cs"),
//            msHiddenDirectory.FullName + $"/Program.cs");
//
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(templateHiddenPath, "xxxx.csproj"),
//            msHiddenDirectory.FullName + $"/{microserviceName.ToLower()}.csproj");
//
//         CreateNewFileWithMicroserviceInfo(microserviceName,
//            Path.Combine(templateHiddenPath, "xxxx.sln"),
//            msHiddenDirectory.FullName + $"/{microserviceName.ToLower()}.sln");

            // copy the dbmicroservice folder into the new microservice (it needs to be there to build the docker container atleast for now.
            // CopyAllHelper(microserviceName, templateHiddenDirectory, msHiddenDirectory);
            // AssetDatabase.Refresh();
        }

        // public static void CreateNewFileWithMicroserviceInfo(string microserviceName, string sourcefile,
        //     string targetfile)
        // {
        //     // if files get really big this will have to change
        //     // MyMicroservice = XXXX | mymicroservice = xxxx (lower case) ||  "" = //ZZZZ  ie things to remove
        //     var text = File.ReadAllText(sourcefile);
        //     text = text.Replace("XXXX", microserviceName);
        //     text = text.Replace("//ZZZZ", "");
        //     text = text.Replace("xxxx", microserviceName.ToLower());
        //     File.WriteAllText(targetfile, text);
        // }

//      public static void CopyAllHelper(string microserviceName, DirectoryInfo source, DirectoryInfo target)
//      {
//         Directory.CreateDirectory(target.FullName);
//
//         // Copy each file into the new directory.
//         foreach (FileInfo fi in source.GetFiles())
//         {
//            // Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
//            //fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
//            var targetFile = Path.Combine(target.FullName, fi.Name);
//            targetFile = targetFile.Replace("XXXX", microserviceName);
//            targetFile = targetFile.Replace("xxxx", microserviceName.ToLower());
//            CreateNewFileWithMicroserviceInfo(microserviceName, fi.FullName, targetFile);
//         }
//
//         // Copy each subdirectory using recursion.
//         foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
//         {
//            DirectoryInfo nextTargetSubDir =
//               target.CreateSubdirectory(diSourceSubDir.Name);
//
//            if (new[] {"~", "obj", "bin"}.Contains(diSourceSubDir.Name) || diSourceSubDir.Name.StartsWith("."))
//            {
//               continue;
//            }
//
//            CopyAllHelper(microserviceName, diSourceSubDir, nextTargetSubDir);
//         }
//      }

        #endregion

        #region TEMPORARY_ONLY_FOR_TESTS

        [MenuItem("TESTING/Create Storage Object")]
        public static void CreateNewStorageObject()
        {
            Debug.LogWarning("=== Using temp method to create new Storage Object ===");
            var randomName = $"StorageObject_{Random.Range(100000000, 999999999)}";
            CreateNewServiceFile(ServiceType.StorageObject, randomName);
        }

        #endregion
        

        public static void CreateNewServiceFile(ServiceType serviceType, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return;
            }
            
            var serviceCreateInfo = _serviceCreateInfos[serviceType];
            var rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            var servicePath = Path.Combine(rootPath, serviceCreateInfo.DestinationDirectoryPath, serviceName);
            var destinationDirectory = Directory.CreateDirectory(servicePath);

            SetupServiceFileInfo(serviceName,
                Path.Combine(rootPath, serviceCreateInfo.TemplateDirectoryPath, $"Unity.Beamable.Runtime.User{serviceCreateInfo.ServiceTypeName}.XXXX.asmdef"),
                destinationDirectory.FullName + $"/Unity.Beamable.Runtime.User{serviceCreateInfo.ServiceTypeName}.{serviceName}.asmdef");

            SetupServiceFileInfo(serviceName,
                Path.Combine(rootPath, serviceCreateInfo.TemplateDirectoryPath, $"{serviceCreateInfo.ServiceTypeName}.cs"),
                destinationDirectory.FullName + $"/{serviceName}.cs");

            AssetDatabase.Refresh();
        }
        
        private static void SetupServiceFileInfo(string serviceName, string sourceFile, string targetFile)
        {
            var text = File.ReadAllText(sourceFile);
            text = text.Replace("XXXX", serviceName);
            text = text.Replace("//ZZZZ", "");
            text = text.Replace("xxxx", serviceName.ToLower());
            File.WriteAllText(targetFile, text);
        }

        private class ServiceCreateInfo
        {
            public ServiceType ServiceType { get; }
            public string ServiceTypeName { get; }
            public string DestinationDirectoryPath { get; }
            public string TemplateDirectoryPath { get; }

            public ServiceCreateInfo(ServiceType serviceType, string destinationDirectoryPath, string templateDirectoryPath)
            {
                ServiceType = serviceType;
                ServiceTypeName = Enum.GetName(typeof(ServiceType), serviceType);
                DestinationDirectoryPath = destinationDirectoryPath;
                TemplateDirectoryPath = templateDirectoryPath;
            }
        }

        public enum ServiceType
        {
            MicroService,
            StorageObject
        }

        #region Docker

//      public static void BuildDockerImage(string imagename, string path)
//      {
//         ClearLog();
//         string microservicepath = Application.dataPath + path;
//         Debug.Log($"Building Microservice docker image '{imagename}' in '{microservicepath}'.");
//         //  docker build -t microservicedb .
//         bool success = Command(imagename+"container", $"{dockerlocation} build -t {imagename} \"{microservicepath}\"");
//         if (success)
//            Debug.Log($"Building Microservice {imagename} docker image...  SUCCESS");
//         else
//         {
//            string text = "";
//#if UNITY_EDITOR
//         text = File.ReadAllText(commandoutputfile);
//#endif
//            // find the error in the docker file VortexMicroservice.cs(65,9): error CS1585: Member modifier 'public' must precede the member type and name [/src/vortexmicroservice.csproj]
//            string pattern = ".*error.*";
//            Regex r = new Regex(pattern);
//            foreach (Match m in r.Matches(text))
//            {
//               LogErrorsConnectedToCodeLines(m.Value, path);
//            }
//
//            if (isVerboseOutput)
//               Debug.LogError(
//                  $"Microservice {imagename} failed to build.  Did you break the server code? Are you adding client methods that can not be run on the server?\n {text}");
//         }
//      }

//      public static void LogErrorsConnectedToCodeLines(string errormessage, string path)
//      {
//         MethodBase mUnityLog;
//
//         // ex. VortexMicroservice.cs(65,9): error CS1585: Member modifier 'public'
//         int nameEnd = errormessage.IndexOf("(");
//         string fileName = errormessage.Substring(0, nameEnd);
//         string file = "Assets" + path + "/" + fileName;
//         int comma = errormessage.IndexOf(",");
//         int line = Int32.Parse(errormessage.Substring(nameEnd + 1,
//            comma - (nameEnd + 1))); //The line number you already know.
//
//         StringBuilder
//            message = new StringBuilder(); //Using StringBuilder in case you want to add more content to the log.
//         message.Append(errormessage);
//
//         // per unity support we have to find this call to do this properly
//         mUnityLog = typeof(UnityEngine.Debug).GetMethod("LogPlayerBuildError",
//            BindingFlags.NonPublic | BindingFlags.Static);
//         mUnityLog.Invoke(null, new object[] {message.ToString(), file, line, 0});
//         wasCompilerError = true;
//      }

//      [UnityEditor.Callbacks.DidReloadScripts(-1)]
//      public static void ClearLog()
//      {
//         if (wasCompilerError)
//         {
//            wasCompilerError = false;
//            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
//            var type = assembly.GetType("UnityEditor.LogEntries");
//            var method = type.GetMethod("Clear");
//            method.Invoke(new object(), null);
//         }
//      }

//      public static void RunMicroservice(string containername, string imagename)
//      {
//         string host = ConfigDatabase.GetString("socket");
//         string cid = ConfigDatabase.GetString("cid");
//         string pid = ConfigDatabase.GetString("pid");
//         string secret = ConfigDatabase.GetString("secret");
//
//         string command = $"{dockerlocation} run --rm " +
//                          $"-p {portCounter}:80 " +
//                          $"--env CID={cid} " +
//                          $"--env PID={pid} " +
//                          $"--env SECRET=\"{secret}\" " +
//                          $"--env HOST=\"{host}\" " +
//                          $"--name {containername} {imagename}";
//
//         Debug.Log($"Run Microservice {containername}");
//         Debug.Log(command);
//         Command(containername, command, false);
//         portCounter++; // this is pretty hacky
//
//      }
//
//      public static void StopMicroservice(string containername)
//      {
//         Debug.Log($"Stop Microservice {containername}");
//         Command(containername, $"{dockerlocation} stop {containername}");
//
//         // this needs more thought
//         portCounter--;
//         if (portCounter < 3000)
//            portCounter = 3000;
//
//      }

        #endregion

//
//      #region commandline
//
//      static bool Command(string service, string command, bool block = true)
//      {
//         using (var p = new Process())
//         {
//            commandoutputfile = $"./Logs/{service}.txt";
//#if UNITY_EDITOR && !UNITY_EDITOR_WIN
//            p.StartInfo.FileName = "sh";
//            p.StartInfo.Arguments = "-c '" + command + " > " + commandoutputfile + "'";
//#else
//         p.StartInfo.FileName = "cmd.exe";
//         p.StartInfo.Arguments = "/C " + command + " > " + commandoutputfile + "'";
//#endif
//            // Configure the process using the StartInfo properties.
//            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
//            p.EnableRaisingEvents = true;
//            p.StartInfo.RedirectStandardInput = true;
//            p.StartInfo.RedirectStandardOutput = true;
//            p.StartInfo.RedirectStandardError = true;
//            p.StartInfo.CreateNoWindow = true;
//            p.StartInfo.UseShellExecute = false;
//
////         p.StartInfo.RedirectStandardInput = true;
////         p.StartInfo.RedirectStandardOutput = true;
////         p.StartInfo.RedirectStandardError = true;
////         p.StartInfo.CreateNoWindow = true;
////         p.StartInfo.UseShellExecute = false;
//
//
//
//            StringBuilder output = new StringBuilder();
//            StringBuilder error = new StringBuilder();
//
//            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
//            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
//            {
//               p.OutputDataReceived += (sender, e) =>
//               {
//                  if (e.Data == null)
//                  {
//                     outputWaitHandle.Set();
//                  }
//                  else
//                  {
//                     output.AppendLine(e.Data);
//                  }
//               };
//               p.ErrorDataReceived += (sender, e) =>
//               {
//                  if (e.Data == null)
//                  {
//                     errorWaitHandle.Set();
//                  }
//                  else
//                  {
//                     error.AppendLine(e.Data);
//                  }
//               };
//
//
//               UnityEngine.Debug.Log(p.StartInfo + " " + p.StartInfo.Arguments);
//
//               p.Start();
//
//               if (!block)
//                  return true;
//               p.BeginOutputReadLine();
//               p.BeginErrorReadLine();
//
//               p.StandardInput.Close();
//
//               int timeout = 2000000;
//
//               if (p.WaitForExit(timeout) &&
//                   outputWaitHandle.WaitOne(timeout) &&
//                   errorWaitHandle.WaitOne(timeout))
//               {
//                  if (!string.IsNullOrEmpty(error.ToString()))
//                  {
//                     if (isVerboseOutput || !block)
//                        UnityEngine.Debug.LogError("Microservice::ERROR " + error.ToString());
//                     return false;
//                  }
//
//                  UnityEngine.Debug.Log(output.ToString());
//               }
//               else
//               {
//                  UnityEngine.Debug.LogError("microservice process timed out...");
//                  UnityEngine.Debug.LogError(output);
//                  UnityEngine.Debug.LogError(error);
//               }
//            }
//
//            return true;
//         }
//      }
//
//      #endregion
    }
}