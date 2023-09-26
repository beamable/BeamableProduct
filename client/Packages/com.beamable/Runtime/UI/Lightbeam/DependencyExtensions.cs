using Beamable.Common;
using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Runtime.LightBeams
{

	public static class LightBeamDependencyExtensions
	{

		public static async Promise<LightBeam> CreateLightBeam(
			this BeamContext ctx,
			RectTransform root,
			CanvasGroup loadingBlocker,
			Action<IDependencyBuilder> scopeConfiguration)
		{
			var lightContext = new LightBeam
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
			
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			
			ctx.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.Instantiate(pageType, ctx.Root, model).ShowLoading(ctx);
			}
			else
			{
				await provider.Instantiate<TDefault>(ctx.Root).ShowLoading(ctx);
			}
		}
		
		public static async Promise Start<TDefault, TModel>(this IDependencyProvider provider, TModel defaultModel)
			where TDefault : MonoBehaviour, ILightComponent<TModel>
		{
				
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();

			ctx.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await provider.Instantiate(pageType, ctx.Root, model).ShowLoading(ctx).ShowLoading(ctx);
			}
			else
			{
				await provider.Instantiate<TDefault, TModel>(ctx.Root, defaultModel).ShowLoading(ctx);
			}
		}

		public static async Promise<T> GotoPage<T, TModel>(this IDependencyProvider provider, TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T, TModel>(ctx.Root, model).ShowLoading(ctx);
		}
		
		public static async Promise<T> GotoPage<T>(this IDependencyProvider provider)
			where T : MonoBehaviour, ILightComponent
		{
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T>(ctx.Root).ShowLoading(ctx);
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
					await instance.OnInstantiated(p.GetService<LightBeam>(), (TModel)model);
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T, TModel> resolver = new LightBeamViewResolver<T, TModel>(async (container, model) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<LightBeam>(), model);
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
					await instance.OnInstantiated(p.GetService<LightBeam>());
					return instance;
				};

				return curry;
			});
			
			builder.AddSingleton(p =>
			{
				LightBeamViewResolver<T> resolver = new LightBeamViewResolver<T>(async (container) =>
				{
					var instance = Object.Instantiate(template, container);
					await instance.OnInstantiated(p.GetService<LightBeam>());
					return instance;
				});

				return resolver;
			});

			builder.AddSingleton(template);
		}
	}
}
