using Beamable.Config;
using Beamable.Editor.UI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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
		public const string TEMPLATE_DIRECTORY = "Packages/com.beamable.server/Template";
		private const string TEMPLATE_MICROSERVICE_DIRECTORY = TEMPLATE_DIRECTORY;
		private const string DESTINATION_MICROSERVICE_DIRECTORY = "Assets/Beamable/Microservices";

		private const string TEMPLATE_STORAGE_OBJECT_DIRECTORY = TEMPLATE_DIRECTORY + "/StorageObject";
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

		public static bool IsInitialized { get; private set; }
		
		static MicroserviceEditor()
		{
			/// Delaying until first editor tick so that the menu
			/// will be populated before setting check state, and
			/// re-apply correct action
			EditorApplication.delayCall += Initialize;
			void Initialize()
			{
				try
				{
					BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				}
				catch(InvalidOperationException)
				{
					EditorApplication.delayCall += Initialize;
					return;
				}

				var enabled = false;
				if (ConfigDatabase.HasKey(CONFIG_AUTO_RUN))
					enabled = ConfigDatabase.GetBool(CONFIG_AUTO_RUN, false);
				else
					enabled = EditorPrefs.GetBool(CONFIG_AUTO_RUN, false);

				setAutoRun(enabled);

				IsInitialized = true;
			}
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

		public static void CreateNewMicroservice(string microserviceName, List<ServiceModelBase> additionalReferences = null)
		{
			CreateNewServiceFile(ServiceType.MicroService, microserviceName, additionalReferences);
		}


		public static void CreateNewServiceFile(ServiceType serviceType, string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			AssetDatabase.StartAssetEditing();
			try
			{
				if (string.IsNullOrWhiteSpace(serviceName))
				{
					return;
				}

				var serviceCreateInfo = _serviceCreateInfos[serviceType];
				var rootPath = Directory.GetParent(Application.dataPath).FullName;
				var relativeDestPath = Path.Combine(serviceCreateInfo.DestinationDirectoryPath, serviceName);
				var absoluteDestPath = Path.Combine(rootPath, relativeDestPath);
				var destinationDirectory = Directory.CreateDirectory(absoluteDestPath);

				var scriptTemplatePath = Path.Combine(rootPath, serviceCreateInfo.TemplateDirectoryPath,
													  serviceCreateInfo.TemplateFileName);

				Debug.Assert(File.Exists(scriptTemplatePath));

				// create the asmdef by hand.
				var asmName = serviceType == ServiceType.MicroService
					? $"Beamable.Microservice.{serviceName}"
					: $"Beamable.Storage.{serviceName}";

				var asmPath = relativeDestPath +
						  $"/{asmName}.asmdef";

				var references = new List<string>
				{
					"Unity.Beamable.Runtime.Common",
					"Unity.Beamable.Server.Runtime",
					"Unity.Beamable.Server.Runtime.Shared",
					"Unity.Beamable",
					"Beamable.SmallerJSON"
				};
				if (MicroserviceConfiguration.Instance.AutoBuildCommonAssembly)
				{
					references.Add(CommonAreaService.GetCommonAsmDefName());
				}


				if (additionalReferences != null && additionalReferences.Count != 0)
				{
					foreach (var additionalReference in additionalReferences)
					{
						// For creating Microservice
						if (additionalReference is MongoStorageModel mongoStorageModel)
						{
							var info = AssemblyDefinitionHelper.ConvertToInfo(mongoStorageModel.Descriptor);
							references.Add(info.Name);
						}
					}
				}

				SetupServiceFileInfo(serviceName, scriptTemplatePath,
									 destinationDirectory.FullName + $"/{serviceName}.cs");
				AssemblyDefinitionHelper.CreateAssetDefinitionAssetOnDisk(
					asmPath,
					new AssemblyDefinitionInfo
					{
						Name = asmName,
						DllReferences =
							serviceType == ServiceType.StorageObject
								? AssemblyDefinitionHelper.MongoLibraries
								: new string[] { },
						IncludePlatforms = new[] { "Editor" },
						References = references.ToArray()
					});

				CommonAreaService.EnsureCommon();

				if (!string.IsNullOrWhiteSpace(asmName) && additionalReferences != null && additionalReferences.Count != 0)
				{
					foreach (var additionalReference in additionalReferences)
					{
						// For creating StorageObject
						if (additionalReference is MicroserviceModel microserviceModel)
						{
							AssemblyDefinitionHelper.AddAndRemoveReferences(microserviceModel.ServiceDescriptor, new List<string> { asmName }, null);
						}
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

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

			public string ServiceTypeName
			{
				get
				{
					switch (ServiceType)
					{
						case ServiceType.MicroService: return "MicroService";
						case ServiceType.StorageObject: return "StorageObject";
					}
					return string.Empty;
				}
			}

			public string DestinationDirectoryPath { get; }
			public string TemplateDirectoryPath { get; }

			public string TemplateFileName
			{
				get
				{
					switch (ServiceType)
					{
						case ServiceType.MicroService: return "Microservice.cs";
						case ServiceType.StorageObject: return "StorageObject.cs";
					}

					return string.Empty;
				}
			}

			public ServiceCreateInfo(ServiceType serviceType, string destinationDirectoryPath, string templateDirectoryPath)
			{
				ServiceType = serviceType;
				DestinationDirectoryPath = destinationDirectoryPath;
				TemplateDirectoryPath = templateDirectoryPath;
			}
		}
	}

	public enum ServiceType
	{
		MicroService,
		StorageObject
	}
}
