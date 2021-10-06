
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
        public StorageObjectDescriptor ServiceDescriptor { get; private set; }
        public MongoStorageBuilder ServiceBuilder { get; private set; }
        public override IBeamableBuilder Builder => ServiceBuilder;
        public override IDescriptor Descriptor => ServiceDescriptor;
        public override bool IsRunning => ServiceBuilder?.IsRunning ?? false;

        public override event Action<Task> OnStart;
        public override event Action<Task> OnStop;
        
        public static MongoStorageModel CreateNew(StorageObjectDescriptor descriptor)
        {
            return new MongoStorageModel
            {
                ServiceDescriptor = descriptor,
                ServiceBuilder = Microservices.GetStorageBuilder(descriptor),
            };
        }

        public override Task Start()
        {
            OnStart?.Invoke(null);
            throw new NotImplementedException();
        }
        public override Task Stop()
        {
            OnStop?.Invoke(null);
            throw new NotImplementedException();
        }
        public override void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
        {
            evt.menu.BeamableAppendAction($"Erase data", _ => Debug.Log("Not implemented!"), false);
            evt.menu.BeamableAppendAction($"Copy connection strings", _ => Debug.Log("Not implemented!"), false);
            evt.menu.BeamableAppendAction($"Goto data explorer", _ => Debug.Log("Not implemented!"), false);
            evt.menu.BeamableAppendAction($"Create a snapshot", _ => Debug.Log("Not implemented!"), false);
            evt.menu.BeamableAppendAction($"Download a snapshot", _ => Debug.Log("Not implemented!"), false);
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