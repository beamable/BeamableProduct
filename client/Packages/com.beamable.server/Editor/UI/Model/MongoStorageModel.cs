
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
        public StorageObjectDescriptor Descriptor { get; private set; }
        public MongoStorageBuilder Builder { get; private set; }
        public override IBeamableBuilder GetBuilder => Builder;
        public override IDescriptor GetDescriptor => Descriptor;
        public override bool IsRunning => Builder?.IsRunning ?? false;

        public override event Action<Task> OnStart;
        public override event Action<Task> OnStop;
        
        public static MongoStorageModel CreateNew(StorageObjectDescriptor descriptor)
        {
            return new MongoStorageModel
            {
                Descriptor = descriptor,
                Builder = Microservices.GetStorageBuilder(descriptor),
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
            throw new NotImplementedException();
        }
        private void OpenInCli()
        {
            throw new NotImplementedException();
        }
        public override void Refresh(IDescriptor descriptor)
        {
            // reset the descriptor and statemachines; because they aren't system.serializable durable.
            Descriptor = (StorageObjectDescriptor)descriptor;
            var oldBuilder = Builder;
            Builder = Microservices.GetStorageBuilder(Descriptor);
            Builder.ForwardEventsTo(oldBuilder);
        }
    }
}