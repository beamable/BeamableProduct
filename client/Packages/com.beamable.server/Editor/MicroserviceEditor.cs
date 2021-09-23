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

        public static void CreateNewMicroservice(string microserviceName)
        {
            CreateNewServiceFile(ServiceType.MicroService, microserviceName);
        }

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
                DestinationDirectoryPath = destinationDirectoryPath;
                TemplateDirectoryPath = templateDirectoryPath;
                
                switch (serviceType)
                {
                    case ServiceType.MicroService: ServiceTypeName = "MicroService"; break;
                    case ServiceType.StorageObject: ServiceTypeName = "StorageObject"; break;
                }
            }
        }

        public enum ServiceType
        {
            MicroService,
            StorageObject
        }
    }
}