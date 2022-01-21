using Beamable.Server.Editor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Model
{
    public class RemoteMicroserviceModel : MicroserviceModel
    {
        public new static RemoteMicroserviceModel CreateNew(MicroserviceDescriptor descriptor, MicroservicesDataModel dataModel)
        {
            return new RemoteMicroserviceModel
            {
                ServiceDescriptor = descriptor,
                ServiceBuilder = Microservices.GetServiceBuilder(descriptor),
                RemoteReference = dataModel.GetReference(descriptor),
                RemoteStatus = dataModel.GetStatus(descriptor),
                Config = MicroserviceConfiguration.Instance.GetEntry(descriptor.Name)
            };
        }

        public override void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
        {
            var remoteCategory = "Cloud" ;
       
            evt.menu.BeamableAppendAction($"{remoteCategory}/View Documentation", pos => { OpenOnRemote("docs/remote/"); });
            evt.menu.BeamableAppendAction($"{remoteCategory}/View Metrics", pos => { OpenOnRemote("metrics"); });
            evt.menu.BeamableAppendAction($"{remoteCategory}/View Logs", pos => { OpenOnRemote("logs"); });

            if (MicroserviceConfiguration.Instance.Microservices.Count > 1)
            {
                evt.menu.BeamableAppendAction($"Order/Move Up", pos => {
                    MicroserviceConfiguration.Instance.MoveIndex(Name, -1, ServiceType.MicroService);
                    OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetIndex(Name, ServiceType.MicroService) > 0);
                evt.menu.BeamableAppendAction($"Order/Move Down", pos => {
                    MicroserviceConfiguration.Instance.MoveIndex(Name, 1, ServiceType.MicroService);
                    OnSortChanged?.Invoke();
                }, MicroserviceConfiguration.Instance.GetIndex(Name, ServiceType.MicroService) < MicroserviceConfiguration.Instance.Microservices.Count - 1);
            }
        }
    }
}
