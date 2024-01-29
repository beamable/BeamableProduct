using Beamable.Common;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Server.Editor;
using System;
using System.Threading.Tasks;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Model
{
	public interface IBeamableService : IMicroserviceVisualsModel
	{
		bool IsRunning { get; }
		ServiceType ServiceType { get; }
		IDescriptor Descriptor { get; }
		bool IsArchived { get; }
		Action<float, long, long> OnDeployProgress { get; set; }

		event Action<Promise> OnStart;
		event Action<Promise> OnStop;

		void Refresh(IDescriptor descriptor);
		Promise Start();
		Promise Stop();
	}
}
