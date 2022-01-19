
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
    public class MongoStorageModel : ServiceModelBase, IBeamableStorageObject
    {
        public ServiceStorageReference RemoteReference { get; protected set; }
        public StorageObjectDescriptor ServiceDescriptor { get; protected set; }
        public MongoStorageBuilder ServiceBuilder { get; protected set; }
        public override IBeamableBuilder Builder => ServiceBuilder;
        public override IDescriptor Descriptor => ServiceDescriptor;
        public override bool IsRunning => ServiceBuilder?.IsRunning ?? false;
        public StorageConfigurationEntry Config { get; protected set; }

        public Action<ServiceStorageReference> OnRemoteReferenceEnriched;

        public override event Action<Task> OnStart;
        public override event Action<Task> OnStop;
        
        public static MongoStorageModel CreateNew(StorageObjectDescriptor descriptor, MicroservicesDataModel dataModel)
        {
            return new MongoStorageModel
            {
                RemoteReference = dataModel.GetStorageReference(descriptor),
                ServiceDescriptor = descriptor,
                ServiceBuilder = Microservices.GetStorageBuilder(descriptor),
                Config = MicroserviceConfiguration.Instance.GetStorageEntry(descriptor.Name)
            };
        }

        public override Task Start()
        {
            OnLogsAttached?.Invoke();
            var task = ServiceBuilder.TryToStart();
            OnStart?.Invoke(task);
            return task;
        }
        public override Task Stop()
        {
            var task = ServiceBuilder.TryToStop();
            OnStop?.Invoke(task);
            return task;
        }

        public void EnrichWithRemoteReference(ServiceStorageReference remoteReference)
        {
            RemoteReference = remoteReference;
            OnRemoteReferenceEnriched?.Invoke(remoteReference);
        }

        public override void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
        {
            // TODO - Implement copy connection strings
            
            evt.menu.BeamableAppendAction($"Erase data", _ => AssemblyDefinitionHelper.ClearMongo(ServiceDescriptor));
            //evt.menu.BeamableAppendAction($"Copy connection strings", _ => Debug.Log("Not implemented!"));
            evt.menu.BeamableAppendAction($"Goto data explorer", _ => AssemblyDefinitionHelper.OpenMongoExplorer(ServiceDescriptor));
            evt.menu.BeamableAppendAction($"Create a snapshot", _ => AssemblyDefinitionHelper.SnapshotMongo(ServiceDescriptor));
            evt.menu.BeamableAppendAction($"Download a snapshot", _ => AssemblyDefinitionHelper.RestoreMongo(ServiceDescriptor));
            evt.menu.BeamableAppendAction($"Open C# Code", _ => OpenCode());

            if (MicroserviceConfiguration.Instance.Microservices.Count > 1)
            {
                evt.menu.BeamableAppendAction($"Order/Move Up", pos => {
					MicroserviceConfiguration.Instance.MoveIndex(Name, -1, ServiceType.StorageObject);
					OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetIndex(Name, ServiceType.StorageObject) > 0);
                evt.menu.BeamableAppendAction($"Order/Move Down", pos => {
					MicroserviceConfiguration.Instance.MoveIndex(Name, 1, ServiceType.StorageObject);
					OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetIndex(Name, ServiceType.StorageObject) < MicroserviceConfiguration.Instance.StorageObjects.Count - 1);
            }
        }

		public override void Refresh(IDescriptor descriptor)
        {
            // reset the descriptor and statemachines; because they aren't system.serializable durable.
            ServiceDescriptor = (StorageObjectDescriptor)descriptor;
            var oldBuilder = ServiceBuilder;
            ServiceBuilder = Microservices.GetStorageBuilder(ServiceDescriptor);
            ServiceBuilder.ForwardEventsTo(oldBuilder);
        }
    }
}
