using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{

   public class MicroserviceConfigConstants : IConfigurationConstants
   {
      public string GetSourcePath(Type type)
      {
         //
         // TODO: make this work for multiple config types
         //       but for now, there is just the one...

         return "Packages/com.beamable.server/Editor/microserviceConfiguration.asset";

      }
   }

   public class MicroserviceConfiguration : AbsModuleConfigurationObject<MicroserviceConfigConstants>
   {
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
      const string DOCKER_LOCATION = "/usr/local/bin/docker";
#else
      const string DOCKER_LOCATION = "docker";
#endif

      public static MicroserviceConfiguration Instance => Get<MicroserviceConfiguration>();

      public List<MicroserviceConfigurationEntry> Microservices;

      [Tooltip("When you run a microservice in the Editor, the prefix controls the flow of traffic. By default, the prefix is your MAC address. If two developers use the same prefix, their microservices will share traffic. The prefix is ignored for games running outside of the Editor."), Delayed]
      public string CustomContainerPrefix;

      private string _cachedContainerPrefix = null;
      
      [Tooltip("When you build a microservice, any ContentType class will automatically be referenced if this field is set to true. Beamable recommends that you put your ContentTypes into a shared assembly definition instead.")]
      public bool AutoReferenceContent = true;

      [Tooltip("When you build and run microservices, the logs will be color coded if this field is set to true.")]
      public bool ColorLogs = true;

      [Tooltip("Docker Buildkit may speed up and increase performance on your microservice builds. However, it is not fully supported with Beamable microservices, and you may encounter issues using it. ")]
      public bool EnableDockerBuildkit = false;

      public string DockerCommand = DOCKER_LOCATION;

      #if BEAMABLE_NEWMS
      [Tooltip("Microservice Logs are sent to a dedicated logging window. If you enable this field, then service logs will also be sent to the Unity Console.")]
      public bool ForwardContainerLogsToUnityConsole;
      #endif

      public Color LogProcessLabelColor = Color.grey;
      public Color LogStandardOutColor = Color.blue;
      public Color LogStandardErrColor = Color.red;
      public Color LogDebugLabelColor =  new Color(.25f, .5f, 1);
      public Color LogInfoLabelColor =  Color.blue;
      public Color LogErrorLabelColor =  Color.red;
      public Color LogWarningLabelColor =  new Color(1, .6f, .15f);
      public Color LogFatalLabelColor =  Color.red;


      #if UNITY_EDITOR
      public override void OnFreshCopy()
      {
         var isDark = UnityEditor.EditorGUIUtility.isProSkin;

         if (isDark)
         {
            LogProcessLabelColor = Color.white;
            LogStandardOutColor = new Color(.2f, .4f, 1f);
            LogStandardErrColor = new Color(1, .44f, .2f);
         }
         else
         {
            LogProcessLabelColor = Color.grey;
            LogStandardOutColor = new Color(.4f, .4f, 1f);
            LogStandardErrColor = new Color(1, .44f, .4f);
         }
         DockerCommand = DOCKER_LOCATION;
      }
      #endif

      public MicroserviceConfigurationEntry GetEntry(string serviceName)
      {
         var existing = Microservices.FirstOrDefault(s => s.ServiceName == serviceName);
         if (existing == null)
         {
            existing = new MicroserviceConfigurationEntry
            {
               ServiceName = serviceName,
               TemplateId = "small",
               Enabled = true,

               DebugData = new MicroserviceConfigurationDebugEntry
               {
                  Password = "Password!",
                  Username = "root",
                  SshPort = 11100 + Microservices.Count
               }
            };
            Microservices.Add(existing);
         }
         return existing;
      }

      private void OnValidate() {
         if (CustomContainerPrefix != _cachedContainerPrefix) {
            _cachedContainerPrefix = CustomContainerPrefix;
            Config.ConfigDatabase.SetString("containerPrefix", _cachedContainerPrefix, true, true);
            EditorApplication.delayCall += () => // using delayCall to avoid Unity warning about sending messages from OnValidate()
               EditorAPI.Instance.Then(api => api.SaveConfig(
                  api.CidOrAlias, api.Pid, api.Host, api.Cid, CustomContainerPrefix));
         }
      }
   }

   [System.Serializable]
   public class MicroserviceConfigurationEntry
   {
      public string ServiceName;
      [Tooltip("If the service should be running on the cloud, in the current realm.")]
      public bool Enabled;
      public string TemplateId;

      [Tooltip("When the container is built, inject the following string into the built docker file.")]
      public string CustomDockerFileStrings;

      [Tooltip("When building locally, should the service be build with debugging tools? If false, you cannot attach breakpoints.")]
      public bool IncludeDebugTools;


      public MicroserviceConfigurationDebugEntry DebugData;
   }

   [System.Serializable]
   public class MicroserviceConfigurationDebugEntry
   {
      public string Username = "beamable";
      [Tooltip("The SSH password to use to connect a debugger. This is only supported for local development. SSH is completely disabled on cloud services.")]
      public string Password = "beamable";
      public int SshPort = -1;
   }
}