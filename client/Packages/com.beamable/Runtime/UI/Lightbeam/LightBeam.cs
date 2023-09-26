using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Beamable.Runtime.LightBeams
{
	public class LightBeam
	{
		public BeamContext BeamContext { get; set; }
		public IDependencyProviderScope Scope { get; set; }
		public RectTransform Root { get; set; }
		public CanvasGroup LoadingBlocker { get; set; }
		
		public Promise<T> Instantiate<T, TModel>(Transform container,
		                                               TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.Instantiate<T, TModel>(container, model);
		
		public Promise<T> GotoPage<T, TModel>(TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.GotoPage<T, TModel>(model);

		public Promise<T> GotoPage<T>() where T : MonoBehaviour, ILightComponent
			=> Scope.GotoPage<T>();
	}

	public interface ILightRoot
	{
		
	}
	
	public interface ILightComponent : ILightRoot
	{
		Promise OnInstantiated(LightBeam beam);
	}
	
	public interface ILightComponent<in T> : ILightRoot
	{
		Promise OnInstantiated(LightBeam beam, T model);
	}

	public delegate Promise<T> LightBeamViewResolver<T, in TModel>(Transform container, TModel model)
                                          		where T : ILightComponent<TModel>;
	
	public delegate Promise<T> LightBeamViewResolver<T>(Transform container)
		where T : ILightComponent;

	public delegate Promise<object> CurriedLightBeamViewResolver(Transform container, Type componentType, object model);

	
}

