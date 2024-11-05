using Beamable.Common;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class MigrationPlan
	{
		public bool NeedsMigration => services.Count > 0;
		public List<MigrationService> services = new List<MigrationService>();
		public List<MigrationStorage> storages = new List<MigrationStorage>();
	}

	public class ActiveMigration
	{
		public List<ActiveServiceMigration> services = new List<ActiveServiceMigration>();
		// public List<ActiveServiceMigration> storages = new List<ActiveServiceMigration>();
		public bool isComplete;
		public class ActiveServiceMigration
		{
			public string name;
			public bool isComplete;
			public List<string> stepNames;
			public List<float> stepRatios;

			public float TotalRatio => stepRatios.Sum() / stepRatios.Count;
		}
	}

	[Serializable]
	public class MigrationStorage
	{
		public bool isFoldOut;
		public string beamoId;
		public StorageObjectDescriptor legacyDescriptor;
		public MigrationCopyStep copyStep = null;
		public ProjectNewStorageArgs newStorageArgs = null;
		public List<string> stepNames = new List<string>();


	}

	[Serializable]
	public class MigrationService
	{
		public bool isFoldOut;

		public string beamoId;
		public MicroserviceDescriptor legacyDescriptor;
		public List<string> assemblyReferences = new List<string>();
		public List<string> storageDependencies = new List<string>();

		public ProjectNewServiceArgs newServiceArgs = null;
		public MigrationCopyStep copyStep = null;
		public UnityUpdateReferencesArgs unityAssemblyDefinitionArgs = null;

		public List<string> stepNames = new List<string>();
	}

	public class MigrationCopyStep
	{
		public string absoluteSourceFolder;
		public string absoluteDestinationFolder;
		public string includeNamespace;

		public string RelativeSourceFolder => FileUtil.GetProjectRelativePath(absoluteSourceFolder);
		public string RelativeDestFolder => FileUtil.GetProjectRelativePath(absoluteDestinationFolder);
	}

	public static class UsamMigrator
	{

		static void CreateNoticeFile(string beamoId, string noun, MigrationCopyStep copyStep)
		{
			var noticeFilePath = Path.Combine(copyStep.absoluteSourceFolder,
											  $"{beamoId}_migration_notice.txt");
			var relativeDest =
				FileUtil.GetProjectRelativePath(copyStep.absoluteDestinationFolder);

			Directory.CreateDirectory(copyStep.absoluteSourceFolder);
			File.WriteAllText(noticeFilePath, $"The {noun}, {beamoId}, was migrated " +
											  $"on {DateTimeOffset.Now:f}. " +
											  $"\n\n" +
											  $"The {noun} has been moved outside of Unity, to {relativeDest}. " +
											  $"\n\n" +
											  $"Please delete this file. ");
		}


		public static ActiveMigration Migrate(MigrationPlan plan, UsamService usam, BeamCommands cli, BeamableDispatcher dispatcher)
		{
			var migration = new ActiveMigration();
			var serviceToActive = new Dictionary<MigrationService, ActiveMigration.ActiveServiceMigration>();
			var storageToActive = new Dictionary<MigrationStorage, ActiveMigration.ActiveServiceMigration>();

			foreach (var storage in plan.storages)
			{
				var active = new ActiveMigration.ActiveServiceMigration
				{
					name = storage.beamoId,
					stepNames = storage.stepNames,
					stepRatios = storage.stepNames.Select(_ => 0f).ToList(),
				};
				storageToActive.Add(storage, active);
				migration.services.Add(active);
			}

			foreach (var service in plan.services)
			{
				var active = new ActiveMigration.ActiveServiceMigration
				{
					name = service.beamoId,
					stepNames = service.stepNames,
					stepRatios = service.stepNames.Select(_ => 0f).ToList(),
				};
				serviceToActive.Add(service, active);
				migration.services.Add(active);
			}



			dispatcher.Run("migration", Run());

			IEnumerator Run()
			{
				AssetDatabase.DisallowAutoRefresh();
				try
				{
					// the migrations need to happen in sequence, because
					//  the CLI connection is shared; and concurrent edits to
					//  the manifest can cause concurrentModificationExceptions within
					//  the MsBuild library

					// migrate storages first, because services depend on these
					foreach (var service in plan.storages)
					{
						var active = storageToActive[service];

						{
							// create storage
							var createCommand = cli.ProjectNewStorage(service.newStorageArgs);
							var expectedCreateLogs = 5;
							createCommand.OnLog(cb =>
							{
								var isInfo =
									cb.data.logLevel.StartsWith("i", StringComparison.InvariantCultureIgnoreCase);
								if (!isInfo) return;
								active.stepRatios[0] = Math.Clamp(1f / expectedCreateLogs--, 0f, 1f);
							});
							var createPromise = createCommand.Run();

							createPromise.Then(_ =>
							{
								active.stepRatios[0] = 1f;
							});
							yield return createPromise.ToYielder();
						}

						{
							// copy files
							var files = Directory.EnumerateFiles(service.copyStep.absoluteSourceFolder,
																 searchPattern: "*",
																 searchOption: SearchOption.AllDirectories)
												 .Where(file =>
												 {
													 // it is POSSIBLE that someone has custom file types that their service needs, 
													 //  and since we are going to DELETE all these files after the migration, we
													 //  should as nice as possible, and only NOT copy stuff that we feel really
													 //  confident about. 
													 var isMetaFile = file.EndsWith(".meta");
													 var isAssemblyDef = file.EndsWith(".asmdef");
													 return !isMetaFile && !isAssemblyDef;
												 })


												 .ToList();
							var steps = (float)(1 +
												files
													.Count); // extra step to delete what-sit
							for (var i = 0; i < files.Count; i++)
							{
								var file = files[i];
								var fileName = Path.GetFileName(file);
								var newFilePath = Path.Combine(service.copyStep.absoluteDestinationFolder, fileName);
								if (File.Exists(newFilePath))
								{
									File.Delete(newFilePath);
								}

								File.Copy(file, newFilePath);
								yield return null;
								active.stepRatios[1] = (i + 1) / steps;
							}

							var newExtensionsFile = Path.Combine(service.copyStep.absoluteDestinationFolder, "StorageExtensions.cs");
							if (File.Exists(newExtensionsFile))
							{
								File.Delete(newExtensionsFile); // Delete this file because in old storages the extensions class was already inside the storage main file
							}

							active.stepRatios[1] = 1f;

						}

						{
							// delete code!
							Directory.Delete(service.copyStep.absoluteSourceFolder, true);
							CreateNoticeFile(service.beamoId, "storage object", service.copyStep);
							active.stepRatios[2] = 1f;
						}

						active.isComplete = true;

					}


					foreach (var service in plan.services)
					{
						var active = serviceToActive[service];

						{
							// create service
							var createCommand = cli.ProjectNewService(service.newServiceArgs);
							var expectedCreateLogs = 5;
							createCommand.OnLog(cb =>
							{
								var isInfo =
									cb.data.logLevel.StartsWith("i", StringComparison.InvariantCultureIgnoreCase);
								if (!isInfo) return;
								active.stepRatios[0] = Math.Clamp(1f / expectedCreateLogs--, 0f, 1f);
							});
							var createPromise = createCommand.Run();

							createPromise.Then(_ =>
							{
								active.stepRatios[0] = 1f;
							});
							yield return createPromise.ToYielder();
						}

						{
							// copy files
							var files = Directory.EnumerateFiles(service.copyStep.absoluteSourceFolder,
																 searchPattern: "*",
																 searchOption: SearchOption.AllDirectories)
												 .Where(file =>
												 {
													 // it is POSSIBLE that someone has custom file types that their service needs, 
													 //  and since we are going to DELETE all these files after the migration, we
													 //  should as nice as possible, and only NOT copy stuff that we feel really
													 //  confident about. 
													 var isMetaFile = file.EndsWith(".meta");
													 var isAssemblyDef = file.EndsWith(".asmdef");
													 return !isMetaFile && !isAssemblyDef;
												 })


												 .ToList();
							var steps = (float)(2 +
												files
													.Count); // extra step for the Program.cs write, and a second step for the partial class patch
							for (var i = 0; i < files.Count; i++)
							{
								var file = files[i];
								var fileName = Path.GetFileName(file);
								var newFilePath = Path.Combine(service.copyStep.absoluteDestinationFolder, fileName);
								if (File.Exists(newFilePath))
								{
									File.Delete(newFilePath);
								}

								File.Copy(file, newFilePath);
								yield return null;
								active.stepRatios[1] = (i + 1) / steps;
							}

							{
								// modify the Program.cs file to include the right namespace (because its different in 1.x vs 2.x)
								var programFilePath =
									Path.Combine(service.copyStep.absoluteDestinationFolder, "Program.cs");
								var content = File.ReadAllText(programFilePath);

								content = content.Replace($"namespace Beamable.{service.beamoId}",
														  "namespace Beamable.Microservices");

								File.WriteAllText(programFilePath, content);
								active.stepRatios[1] = (steps - 1) / steps;

							}

							{
								// modify the main class file to be partial; in 2.x, there is a source generator that requires it to be partial
								var mainClassFilePath =
									Path.Combine(service.copyStep.absoluteDestinationFolder, $"{service.beamoId}.cs");
								var content = File.ReadAllText(mainClassFilePath);
								content = content.Replace($"public class {service.beamoId}",
														  $"public partial class {service.beamoId}");
								File.WriteAllText(mainClassFilePath, content);
							}

							active.stepRatios[1] = 1f;
						}


						{
							// add references
							var addRefCommand = cli.UnityUpdateReferences(service.unityAssemblyDefinitionArgs);
							yield return addRefCommand.Run().ToYielder();
							active.stepRatios[2] = 1f;
						}

						{
							// delete code!
							Directory.Delete(service.copyStep.absoluteSourceFolder, true);
							CreateNoticeFile(service.beamoId, "service", service.copyStep);
							active.stepRatios[3] = 1f;
						}

						active.isComplete = true;
					}

					yield return usam.WaitReload().ToYielder();

					migration.isComplete = true;
				}
				finally
				{
					AssetDatabase.AllowAutoRefresh();
				}
			}


			return migration;
		}

		static string GetOutputFolder(IDescriptor descriptor)
		{
			string outputFolder = UsamService.SERVICES_FOLDER;
			{ // depending on the origin of this service, it may go to different output folders
				var sourcePath = descriptor.SourcePath;
				var folders = sourcePath.Split(Path.DirectorySeparatorChar).SkipWhile(x => x == ".");
				var firstFolder = folders.FirstOrDefault() ?? "";

				if (firstFolder.StartsWith("Packages"))
				{

					if (descriptor.IsSourceCodeAvailableLocally())
					{
						outputFolder = Path.Combine(Path.GetDirectoryName(sourcePath), "BeamableServices~");
					}
					else
					{
						Debug.LogError("Uh oh this is not a valid migration pathway");
						// TODO:
					}
				}
			}
			return outputFolder;
		}

		public static MigrationPlan CreatePlan(MicroserviceReflectionCache.Registry registry)
		{
			var plan = new MigrationPlan();

			var services = registry.Descriptors;
			var storages = registry.StorageDescriptors;
			var storageAssets = storages.Select(x => x.ConvertToAsset()).ToList();

			foreach (var storage in storages)
			{
				var migration = new MigrationStorage
				{
					beamoId = storage.Name,
					legacyDescriptor = storage
				};

				var outputFolder = GetOutputFolder(storage);

				{ // create the storage
					migration.newStorageArgs = new ProjectNewStorageArgs
					{
						name = new ServiceName(storage.Name),
						serviceDirectory = outputFolder,
						sln = UsamService.SERVICES_SLN_PATH,
					};
					migration.stepNames.Add("Create Storage");

				}

				{ // copy the code
					migration.copyStep = new MigrationCopyStep()
					{
						includeNamespace = storage.Type.Namespace,
						absoluteDestinationFolder =
							Path.GetFullPath(Path.Combine(outputFolder, storage.Name)),
						absoluteSourceFolder = Path.GetFullPath(storage.SourcePath)
					};
					migration.stepNames.Add("Copy Code");
				}

				{ // delete old code
					migration.stepNames.Add("Remove old code");
				}

				plan.storages.Add(migration);
			}

			foreach (var service in services)
			{
				var migration = new MigrationService
				{
					beamoId = service.Name,
					legacyDescriptor = service
				};

				{ // storage refs
					var serviceInfo = service.ConvertToInfo();
					var serviceDependencies = new List<string>();
					for (var i = 0; i < storageAssets.Count; i++)
					{
						var storageObjectAssemblyDefinitionsAsset = storageAssets[i];
						var storage = storages[i];
						if (serviceInfo.References.Contains(
								storageObjectAssemblyDefinitionsAsset.ConvertToInfo().Name))
						{
							serviceDependencies.Add(storage.Name);
						}
					}

					migration.storageDependencies = serviceDependencies;
				}

				var outputFolder = GetOutputFolder(service);

				{ // first, create the service template. 
					migration.newServiceArgs = new ProjectNewServiceArgs
					{
						name = new ServiceName(service.Name),
						serviceDirectory = outputFolder,
						sln = UsamService.SERVICES_SLN_PATH,
						linkTo = migration.storageDependencies.ToArray()
					};
					migration.stepNames.Add("Create Service");
				}

				{ // then, copy the code
					migration.copyStep = new MigrationCopyStep()
					{
						includeNamespace = service.Type.Namespace,
						absoluteDestinationFolder =
							Path.GetFullPath(Path.Combine(outputFolder, service.Name)),
						absoluteSourceFolder = Path.GetFullPath(service.SourcePath)
					};
					migration.stepNames.Add("Copy Code");
				}

				{ // then, add refs

					{ // assembly def refs
						var assemblies = GetAssemblyDefinitionAssets(service);
						var pathsList = new List<string>();
						var namesList = new List<string>();
						foreach (AssemblyDefinitionAsset asmdef in assemblies)
						{
							// skip things that are storages...
							if (storageAssets.Any(asset => asset.name == asmdef.name)) continue;

							namesList.Add(asmdef.name);
							var pathFromRootFolder = CsharpProjectUtil.GenerateCsharpProjectFilename(asmdef.name);
							var pathToService =
								Path.Combine(outputFolder, service.Name, $"{service.Name}.csproj");
							pathsList.Add(PackageUtil.GetRelativePath(pathToService, pathFromRootFolder));
						}

						migration.unityAssemblyDefinitionArgs = new UnityUpdateReferencesArgs
						{
							service = service.Name,
							names = namesList.ToArray(),
							paths = pathsList.ToArray()
						};
					}



					migration.stepNames.Add("Add References");
				}

				{ // TODO: handle federations...

				}

				{ // delete old code
					migration.stepNames.Add("Remove old code");
				}
				plan.services.Add(migration);

			}

			return plan;
		}

		public static List<AssemblyDefinitionAsset> GetAssemblyDefinitionAssets(MicroserviceDescriptor descriptor)
		{
			List<AssemblyDefinitionAsset> assets = new List<AssemblyDefinitionAsset>();
			List<string> mandatoryReferences = new List<string>() { "Unity.Beamable.Customer.Common" }; // Add the customer common asmdef even if it's not being used

			var dependencies = descriptor.Type.Assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();
			dependencies.AddRange(mandatoryReferences);
			foreach (var name in dependencies)
			{
				if (CsharpProjectUtil.IsValidReference(name))
				{
					var guid = AssetDatabase.FindAssets($"t:AssemblyDefinitionAsset {name}");

					if (guid.Length == 0)
					{
						continue; //there is no asset of this assembly to reference
					}

					if (guid.Length > 1)
					{
						throw new Exception($"Found more than one assembly definition with the name: {name}");
					}

					var path = AssetDatabase.GUIDToAssetPath(guid[0]);

					if (string.IsNullOrEmpty(path)) continue;

					var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
					if (asset != null && asset.name.Equals(name)) assets.Add(asset);
				}
			}

			return assets;
		}

	}
}
