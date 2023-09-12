using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Beamable.Runtime.LightBeam
{
	public class LightContext
	{
		public BeamContext BeamContext { get; set; }
		public IDependencyProviderScope Scope { get; set; }
		public RectTransform Root { get; set; }
		public CanvasGroup LoadingBlocker { get; set; }
		
	}

	public interface ILightRoot
	{
		
	}
	
	public interface ILightComponent : ILightRoot
	{
		Promise OnInstantiated(BeamContext context);
	}
	
	public interface ILightComponent<in T> : ILightRoot
	{
		Promise OnInstantiated(BeamContext context, T model);
	}

	public delegate Promise<T> LightBeamViewResolver<T, in TModel>(Transform container, TModel model)
                                          		where T : ILightComponent<TModel>;
	
	public delegate Promise<T> LightBeamViewResolver<T>(Transform container)
		where T : ILightComponent;

	public delegate Promise<object> CurriedLightBeamViewResolver(Transform container, Type componentType, object model);

	public static class LightBeamUtilExtensions
	{
		public static Dictionary<string, string> Hints = new Dictionary<string, string>();
		
		public static void Clear(this Transform transform)
		{
			for (var i = 0; i < transform.childCount; i++)
			{
				Object.Destroy(transform.GetChild(i).gameObject);
			}
		}

		public static async Promise<T> Instantiate<T, TModel>(this T template, BeamContext context, Transform container, TModel model)
			where T : Component, ILightComponent<TModel>
		{
			var instance = Object.Instantiate(template, container);
			await instance.OnInstantiated(context, model);
			return instance;
		}
		
		public static async Promise<T> Instantiate<T, TModel>(this BeamContext context, T template, Transform container, TModel model)
			where T : Component, ILightComponent<TModel>
		{
			var instance = Object.Instantiate(template, container);
			await instance.OnInstantiated(context, model);
			return instance;
		}


		public static void EnableObjects(this ILightRoot _, bool enabled, params Component[] objects)
		{
			foreach (var component in objects)
			{
				component.gameObject.SetActive(enabled);
			}
		}
		
		public static void EnableObjects(this ILightRoot root, params Component[] objects)
		{
			root.EnableObjects(true, objects);
		}
		
		public static void DisableObjects(this ILightRoot root, params Component[] objects)
		{
			root.EnableObjects(false, objects);
		}

		public static void HandleClicked(this Button button, Action onClick)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => onClick());
		}

		public static void HandleClicked(this Button button, Func<Promise> onClick) =>
			HandleClicked(button, null, onClick);
		public static void HandleClicked(this Button button, string text, Func<Promise> onClick)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(async () =>
			{
				var action = onClick();
				await button.ShowLoading(text, action);
			});
		}

		public static Promise<T> ShowLoading<T>(this Promise<T> promise, BeamContext context)
		{
			return ShowLoading(promise.ToPromise(), context)
				.Map(_ => promise.GetResult())
				.Error(ex => throw ex);
		}
		public static Promise ShowLoading<T>(this T promise, BeamContext context)
			where T : Promise<Unit>
		{
			return ShowLoading(promise.ToPromise(), context);
		}

		public static async Promise LoadingFadeIn(this BeamContext context)
		{
			var service = context.ServiceProvider.GetService<CoroutineService>();
			var lightContext = context.ServiceProvider.GetService<LightContext>();
			service.StopAll("loading");
			var promise = new Promise();
			service.StartNew("loading", Animate());
			IEnumerator Animate()
			{
				lightContext.LoadingBlocker.gameObject.SetActive(true);
				var fadeIn = Lerp(x => lightContext.LoadingBlocker.alpha = x, lightContext.LoadingBlocker.alpha, 1);
				foreach (var p in fadeIn)
				{
					yield return p;
				}
				promise.CompleteSuccess();
			}

			await promise;
		}
		
		public static async Promise ShowLoading(this Promise promise, LightContext context)
		{
			if (context.LoadingBlocker == null)
			{
				await promise;
				return;
			}
			
			
			var service = context.Scope.GetService<CoroutineService>();
			
			service.StopAll("loading");
			service.StartNew("loading", Animate());
			IEnumerator Animate()
			{
				Func<bool> isWorking = () => !promise.IsCompleted && !promise.IsFailed;
				yield return null;
				context.LoadingBlocker.gameObject.SetActive(true);
				var fadeIn = Lerp(x => context.LoadingBlocker.alpha = x, context.LoadingBlocker.alpha, 1);
				foreach (var p in fadeIn)
				{
					if (!isWorking()) break;
					yield return p;
				}
				
				while (isWorking())
				{
					yield return null;
				}
				
				var fadeOut = Lerp(x => context.LoadingBlocker.alpha = x, 1, 0);
				foreach (var p in fadeOut)
				{
					yield return p;
				}

				context.LoadingBlocker.gameObject.SetActive(false);
			}
			
			await promise;
		}
		
		static IEnumerable Lerp( Action<float> valueCallback, float start, float end, float delay=0,float duration=.1f, Action cb=null)
		{
			yield return new WaitForSecondsRealtime(delay);
			var startTime = Time.realtimeSinceStartup;
			var endTime = startTime + duration;
			valueCallback(start);
			var resolution = new WaitForSecondsRealtime(.01f);
			while ( Time.realtimeSinceStartup <= endTime)
			{
				var r = 1 - (endTime - Time.realtimeSinceStartup) / (endTime - startTime);
				var value = Mathf.Lerp(start, end, r);
				valueCallback(value);
				yield return resolution;
			}
			valueCallback(end);
			cb?.Invoke();
		}

		private static async Promise ShowLoading(this Button button, string loadingText, Promise promise)
		{
			button.interactable = false;
			var text = button.GetComponentInChildren<TMP_Text>();
			var originalText = text.text;
			text.text = loadingText ?? "loading...";
			await promise;

			if (text)
			{
				text.text = originalText;
			}

			if (button)
			{
				button.interactable = true;
			}
		}
	}

	public static class BeamContextExtensions
	{
		
		public static Promise<T> GotoPage<T, TModel>(this BeamContext ctx, TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			return ctx.ServiceProvider.GotoPage<T, TModel>(model);
		}
		
		public static Promise<T> GotoPage<T>(this BeamContext ctx)
			where T : MonoBehaviour, ILightComponent
		{
			return ctx.ServiceProvider.GotoPage<T>();
		}

		public static Promise<T> Instantiate<T, TModel>(
			this BeamContext ctx,
			Transform container,
			TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			return ctx.ServiceProvider.Instantiate<T, TModel>(container, model);
		}
		
		public static Promise<T> Instantiate<T>(
			this BeamContext ctx,
			Transform container)
			where T : MonoBehaviour, ILightComponent
		{
			return ctx.ServiceProvider.Instantiate<T>(container);
		}

		public static Promise<object> Instantiate(this BeamContext ctx,
		                                                Type componentType,
		                                                Transform container)
		{
			return ctx.ServiceProvider.Instantiate(componentType, container);
		}

		public static Promise<object> Instantiate(this BeamContext ctx,
		                                                Type componentType,
		                                                Transform container,
		                                                object model)
		{
			return ctx.ServiceProvider.Instantiate(componentType, container, model);
		}
	}
	
	public static class LightBeamDependencyExtensions
	{
		public static async Promise<BeamContext> CreateLightBeam(this BeamContext beamContext,
		                                                            RectTransform root,
		                                                            CanvasGroup loadingBlocker,
		                                                            Action<IDependencyBuilder> scopeConfigurator)
		{
			var lightContext = new LightContext
			{
				BeamContext = beamContext,
				Root = root,
				LoadingBlocker = loadingBlocker
			};
			var scope = beamContext.ServiceProvider.Fork(builder =>
			{
				builder.AddScoped(lightContext);
				scopeConfigurator?.Invoke(builder);
			});
			lightContext.Scope = scope;
			await beamContext.OnReady.ShowLoading(beamContext);
			return beamContext;
		}

		public static async Promise DestroyLightBeams(LightContext context)
		{
			await context.Scope.Dispose();
		}

		static bool TryGetTypes(IDependencyProvider provider, Dictionary<string, string> args, out Type pageType, out object model)
		{
			pageType = null;
			model = null;
			if (!LightBeamUtilExtensions.Hints.TryGetValue("pageType", out var pageTypeStr))
			{
				return false;
			}

			var scope = (IDependencyProviderScope)provider;
			var service = scope.SingletonServices.FirstOrDefault(
				x => x.Interface.Name.Equals(pageTypeStr, StringComparison.InvariantCultureIgnoreCase));

			pageType = service.Interface;

			// now we need to find a model...
			model = null;

			var interfaces = pageType.GetInterfaces();
			foreach (var interfaceType in interfaces)
			{
				if (!interfaceType.IsGenericType) continue;
				if (interfaceType.GetGenericTypeDefinition() != typeof(ILightComponent<>)) continue;

				var modelType = interfaceType.GetGenericArguments()[0];
				model = Activator.CreateInstance(modelType);
				break;
			}

			if (model != null)
			{
				foreach (var kvp in args)
				{
					if (!kvp.Key.StartsWith("d_")) continue;
					var key = kvp.Key.Substring("d_".Length);
					
					var json = $"{{\"{key}\": {kvp.Value} }}";
					JsonUtility.FromJsonOverwrite(json, model);
				}
				
			}
			

			return true;

		}

		public static async Promise Start<TDefault>(this IDependencyProvider provider)
			where TDefault : MonoBehaviour, ILightComponent
		{
			var beamContext = provider.GetService<BeamContext>();
			var lightContext = provider.GetService<LightContext>();
			await beamContext.LoadingFadeIn();
			
			lightContext.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.Instantiate(pageType, lightContext.Root, model).ShowLoading(beamContext);
			}
			else
			{
				await provider.Instantiate<TDefault>(lightContext.Root).ShowLoading(beamContext);
			}
		}
		
		public static async Promise Start<TDefault, TModel>(this IDependencyProvider provider, TModel defaultModel)
			where TDefault : MonoBehaviour, ILightComponent<TModel>
		{
			var beamContext = provider.GetService<BeamContext>();
			var ctx = provider.GetService<LightContext>();
			await beamContext.LoadingFadeIn();

			ctx.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.Instantiate(pageType, ctx.Root, model).ShowLoading(beamContext);
			}
			else
			{
				await provider.Instantiate<TDefault, TModel>(ctx.Root, defaultModel).ShowLoading(beamContext);
			}
		}

		public static async Promise<T> GotoPage<T, TModel>(this IDependencyProvider provider, TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var ctx = provider.GetService<LightContext>();
			var beamContext = provider.GetService<BeamContext>();
			await beamContext.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T, TModel>(ctx.Root, model).ShowLoading(beamContext);
		}
		
		public static async Promise<T> GotoPage<T>(this IDependencyProvider provider)
			where T : MonoBehaviour, ILightComponent
		{
			var beamContext = provider.GetService<BeamContext>();
			var ctx = provider.GetService<LightContext>();
			await beamContext.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T>(ctx.Root).ShowLoading(beamContext);
		}

		public static Promise<T> Instantiate<T, TModel>(
			this IDependencyProvider provider,
			Transform container,
			TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var resolver = provider.GetService<LightBeamViewResolver<T, TModel>>();
			var instance = resolver(container, model);
			return instance;
		}
		
		public static Promise<T> Instantiate<T>(
			this IDependencyProvider provider,
			Transform container)
			where T : MonoBehaviour, ILightComponent
		{
			var resolver = provider.GetService<LightBeamViewResolver<T>>();
			var instance = resolver(container);
			return instance;
		}

		public static Promise<object> Instantiate(this IDependencyProvider provider,
		                                                Type componentType,
		                                                Transform container)
		{
			var resolver = provider.GetService<CurriedLightBeamViewResolver>();
			var instance = resolver(container, componentType, null);
			return instance;
		}

		public static Promise<object> Instantiate(this IDependencyProvider provider,
		                                                Type componentType,
		                                                Transform container,
		                                                object model)
		{
			var resolver = provider.GetService<CurriedLightBeamViewResolver>();

			var instance = resolver(container, componentType, model);
			return instance;
		}

		public static void AddLightComponent<T, TModel>(this IDependencyBuilder builder, T template)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var rawBuilder = builder as DependencyBuilder;
			rawBuilder.TryGetSingleton(typeof(CurriedLightBeamViewResolver), out var oldCurry);
			
			builder.RemoveIfExists<CurriedLightBeamViewResolver>();
			builder.AddSingleton(p =>
			{
				CurriedLightBeamViewResolver curry = async (container, type, model) =>
				{
					if (typeof(T) != type)
					{
						if (oldCurry == null)
						{
							return null;
						}
						else
						{
							var resolver = (CurriedLightBeamViewResolver) oldCurry.Factory(p);
							return await resolver(container, type, model);
						}
					}
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<BeamContext>(), (TModel)model);
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T, TModel> resolver = new LightBeamViewResolver<T, TModel>(async (container, model) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<BeamContext>(), model);
					return instance;
				});
				

				return resolver;
			});
			
			builder.AddSingleton(template);
		}
		
		public static void AddLightComponent<T>(this IDependencyBuilder builder, T template)
			where T : MonoBehaviour, ILightComponent
		{
			var rawBuilder = builder as DependencyBuilder;
			rawBuilder.TryGetSingleton(typeof(CurriedLightBeamViewResolver), out var oldCurry);
			
			builder.RemoveIfExists<CurriedLightBeamViewResolver>();
			builder.AddSingleton(p =>
			{
				CurriedLightBeamViewResolver curry = async (container, type, model) =>
				{
					if (typeof(T) != type)
					{
						if (oldCurry == null)
						{
							return null;
						}
						else
						{
							var resolver = (CurriedLightBeamViewResolver) oldCurry.Factory(p);
							return await resolver(container, type, model);
						}
					}
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<BeamContext>());
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T> resolver = new LightBeamViewResolver<T>(async (container) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<BeamContext>());
					return instance;
				});

				return resolver;
			});

			builder.AddSingleton(template);
		}
	}
}

