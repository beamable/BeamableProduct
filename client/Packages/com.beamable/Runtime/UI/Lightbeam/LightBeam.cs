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
		
		public Promise<T> NewLightComponent<T, TModel>(Transform container,
		                                      TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.NewLightComponent<T, TModel>(container, model);
		
		
		public Promise<T> SetLightComponent<T, TModel>(Transform container,
		                                      TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.SetLightComponent<T, TModel>(container, model);

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
		Promise OnInstantiated(LightContext context);
	}
	
	public interface ILightComponent<in T> : ILightRoot
	{
		Promise OnInstantiated(LightContext context, T model);
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

		public static Promise<T> ShowLoading<T>(this Promise<T> promise, LightContext context)
		{
			return ShowLoading(promise.ToPromise(), context)
				.Map(_ => promise.GetResult())
				.Error(ex => throw ex);
		}
		public static Promise ShowLoading<T>(this T promise, LightContext context)
			where T : Promise<Unit>
		{
			return ShowLoading(promise.ToPromise(), context);
		}

		public static async Promise LoadingFadeIn(this LightContext context)
		{
			var service = context.Scope.GetService<CoroutineService>();
			service.StopAll("loading");
			var promise = new Promise();
			service.StartNew("loading", Animate());
			IEnumerator Animate()
			{
				context.LoadingBlocker.gameObject.SetActive(true);
				var fadeIn = Lerp(x => context.LoadingBlocker.alpha = x, context.LoadingBlocker.alpha, 1);
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
	
	public static class LightBeamDependencyExtensions
	{

		public static async Promise<LightContext> InitLightBeams(
			this BeamContext ctx,
			RectTransform root,
			CanvasGroup loadingBlocker,
			Action<IDependencyBuilder> scopeConfiguration)
		{
			var lightContext = new LightContext
			{
				BeamContext = ctx,
				Root = root,
				LoadingBlocker = loadingBlocker
			};
			var scope = ctx.ServiceProvider.Fork(builder =>
			{
				builder.AddScoped(lightContext);
				scopeConfiguration?.Invoke(builder);
			});
			lightContext.Scope = scope;

			await ctx.OnReady.ShowLoading(lightContext);
			return lightContext;
		}
		
		public static async Promise<LightContext> InitLightBeams<T>(this T lightBeam,
		                                                            RectTransform root,
		                                                            CanvasGroup loadingBlocker,
		                                                            Action<IDependencyBuilder> scopeConfigurator)
			where T : Component
		{
			var ctx = BeamContext.InParent(lightBeam);
			var lightContext = new LightContext
			{
				BeamContext = ctx,
				Root = root,
				LoadingBlocker = loadingBlocker
			};
			var scope = ctx.ServiceProvider.Fork(builder =>
			{
				builder.AddScoped(lightContext);
				scopeConfigurator?.Invoke(builder);
			});
			lightContext.Scope = scope;

			await ctx.OnReady.ShowLoading(lightContext);
			return lightContext;
		}

		public static async Promise DestroyLightBeams(LightContext context)
		{
			await context.Scope.Dispose();
		}

		public static Promise<T> SetLightComponent<T, TModel>(this IDependencyProvider provider,
		                                             Transform container,
		                                             TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			container.Clear();
			return provider.NewLightComponent<T, TModel>(container, model);
		}
		
		public static Promise<T> SetLightComponent<T>(this IDependencyProvider provider,
		                                             Transform container)
			where T : MonoBehaviour, ILightComponent
		{
			container.Clear();
			return provider.NewLightComponent<T>(container);
		}

		public static Promise<object> SetLightComponent(this IDependencyProvider provider,
		                                                Type componentType,
		                                                Transform container)
		{
			container.Clear();
			return provider.NewLightComponent(componentType, container);
		}

		public static Promise<object> SetLightComponent(this IDependencyProvider provider,
		                                                Type componentType,
		                                                Transform container,
		                                                object model)
		{
			container.Clear();
			return provider.NewLightComponent(componentType, container, model);
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
			
			var ctx = provider.GetService<LightContext>();
			await ctx.LoadingFadeIn();
			
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.SetLightComponent(pageType, ctx.Root, model).ShowLoading(ctx);
			}
			else
			{
				await provider.SetLightComponent<TDefault>(ctx.Root).ShowLoading(ctx);
			}
		}
		
		public static async Promise Start<TDefault, TModel>(this IDependencyProvider provider, TModel defaultModel)
			where TDefault : MonoBehaviour, ILightComponent<TModel>
		{
				
			var ctx = provider.GetService<LightContext>();
			await ctx.LoadingFadeIn();

			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.SetLightComponent(pageType, ctx.Root, model).ShowLoading(ctx).ShowLoading(ctx);
			}
			else
			{
				await provider.SetLightComponent<TDefault, TModel>(ctx.Root, defaultModel).ShowLoading(ctx);
			}
		}

		public static async Promise<T> GotoPage<T, TModel>(this IDependencyProvider provider, TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var ctx = provider.GetService<LightContext>();
			await ctx.LoadingFadeIn();
			return await provider.SetLightComponent<T, TModel>(ctx.Root, model).ShowLoading(ctx);
		}
		
		public static async Promise<T> GotoPage<T>(this IDependencyProvider provider)
			where T : MonoBehaviour, ILightComponent
		{
			var ctx = provider.GetService<LightContext>();
			await ctx.LoadingFadeIn();
			return await provider.SetLightComponent<T>(ctx.Root).ShowLoading(ctx);
		}

		public static Promise<T> NewLightComponent<T, TModel>(
			this IDependencyProvider provider,
			Transform container,
			TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var resolver = provider.GetService<LightBeamViewResolver<T, TModel>>();
			var instance = resolver(container, model);
			return instance;
		}
		
		public static Promise<T> NewLightComponent<T>(
			this IDependencyProvider provider,
			Transform container)
			where T : MonoBehaviour, ILightComponent
		{
			var resolver = provider.GetService<LightBeamViewResolver<T>>();
			var instance = resolver(container);
			return instance;
		}

		public static Promise<object> NewLightComponent(this IDependencyProvider provider,
		                                                Type componentType,
		                                                Transform container)
		{
			var resolver = provider.GetService<CurriedLightBeamViewResolver>();
			var instance = resolver(container, componentType, null);
			return instance;
		}

		public static Promise<object> NewLightComponent(this IDependencyProvider provider,
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
					await instance.OnInstantiated(p.GetService<LightContext>(), (TModel)model);
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T, TModel> resolver = new LightBeamViewResolver<T, TModel>(async (container, model) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<LightContext>(), model);
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
					await instance.OnInstantiated(p.GetService<LightContext>());
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T> resolver = new LightBeamViewResolver<T>(async (container) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<LightContext>());
					return instance;
				});

				return resolver;
			});

			builder.AddSingleton(template);
		}
	}
}

