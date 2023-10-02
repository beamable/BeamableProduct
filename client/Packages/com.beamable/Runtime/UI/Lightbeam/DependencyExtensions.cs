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
		/// <summary>
		/// Creates a slim <see cref="LightBeam"/> UI data object.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="BeamContext"/> that will be used to populate the data in the <see cref="LightBeam"/>.
		/// For example, use <see cref="BeamContext.Default"/> to get started quickly.
		/// The 
		/// </param>
		/// <param name="root">
		/// The content <see cref="RectTransform"/> for the UI. GameObjects will be spawned and removed from
		/// this transform as the UI pages are changed.
		/// </param>
		/// <param name="loadingBlocker">
		/// The <see cref="CanvasGroup"/> that will be faded in and out as the pages change.
		/// Page changes are asynchronous, and therefor automatically show a loading screen. 
		/// </param>
		/// <param name="scopeConfiguration"></param>
		/// A <see cref="LightBeam"/> UI should use dependency injection to load <see cref="ILightComponent"/>s.
		/// Those components can be registered here.
		/// The resulting <see cref="LightBeam.Scope"/> property may be used to access the registered components.
		/// <returns>
		/// A <see cref="Promise"/> that contains the created <see cref="LightBeam"/>.
		/// The <see cref="Promise"/> will complete when the given <see cref="BeamContext"/> is ready.
		/// </returns>
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

		/// <inheritdoc cref="Start{TDefault, TModel}"/>
		public static async Promise Start<TDefault>(this IDependencyProvider provider)
			where TDefault : MonoBehaviour, ILightComponent
		{
			
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			
			ctx.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await Instantiate(provider, pageType, ctx.Root, model).ShowLoading(ctx);
			}
			else
			{
				await provider.Instantiate<TDefault>(ctx.Root).ShowLoading(ctx);
			}
		}
		
		/// <summary>
		/// Start the <see cref="LightBeam"/> UI to a page defined by <typeparamref name="TDefault"/>.
		/// <para>
		/// Clear the content of the <see cref="LightBeam.Root"/> and spawn
		/// an instance of the given <see cref="ILightComponent"/> as the main GameObject
		/// in the <see cref="LightBeam.Root"/>.
		///
		/// The <see cref="LightBeam.LoadingBlocker"/> will fade in as this function's <see cref="Promise"/>
		/// completes, and then it will fade out.
		/// </para>
		/// <para>
		/// If deep links have been specified in the app URL, they may override the starting page and model data.
		/// </para>
		/// </summary>
		/// <param name="provider">A <see cref="IDependencyProvider"/> that has the <typeparamref name="T"/> registered. </param>
		/// <param name="defaultModel">The data model required for the <see cref="ILightComponent"/></param>
		/// <typeparam name="TDefault"></typeparam>
		/// <typeparam name="TModel"></typeparam>
		public static async Promise Start<TDefault, TModel>(this IDependencyProvider provider, TModel defaultModel)
			where TDefault : MonoBehaviour, ILightComponent<TModel>
		{
				
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();

			ctx.Root.Clear();
			if (TryGetTypes(provider, LightBeamUtilExtensions.Hints, out var pageType, out var model))
			{
				await Instantiate(provider, pageType, ctx.Root, model).ShowLoading(ctx).ShowLoading(ctx);
			}
			else
			{
				await provider.Instantiate<TDefault, TModel>(ctx.Root, defaultModel).ShowLoading(ctx);
			}
		}

		/// <summary>
		/// Clear the content of the <see cref="LightBeam.Root"/> and spawn
		/// an instance of the given <see cref="ILightComponent"/> as the main GameObject
		/// in the <see cref="LightBeam.Root"/>.
		///
		/// The <see cref="LightBeam.LoadingBlocker"/> will fade in as this function's <see cref="Promise"/>
		/// completes, and then it will fade out.
		/// </summary>
		/// <param name="provider">A <see cref="IDependencyProvider"/> that has the <typeparamref name="T"/> registered. </param>
		/// <param name="model">The data model required for the <see cref="ILightComponent"/></param>
		/// <typeparam name="T">A type of <see cref="ILightComponent"/></typeparam>
		/// <typeparam name="TModel">arbitrary data that will be given to the new <see cref="ILightComponent"/></typeparam>
		/// <returns>A <see cref="Promise"/> that completes with when the new <see cref="ILightComponent.OnInstantiated"/> method completes.</returns>
		public static async Promise<T> GotoPage<T, TModel>(this IDependencyProvider provider, TModel model)
			where T : MonoBehaviour, ILightComponent<TModel>
		{
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T, TModel>(ctx.Root, model).ShowLoading(ctx);
		}
		
		/// <inheritdoc cref="GotoPage{T,TModel}"/>
		public static async Promise<T> GotoPage<T>(this IDependencyProvider provider)
			where T : MonoBehaviour, ILightComponent
		{
			var ctx = provider.GetService<LightBeam>();
			await ctx.LoadingFadeIn();
			ctx.Root.Clear();
			return await provider.Instantiate<T>(ctx.Root).ShowLoading(ctx);
		}

		/// <summary>
		/// Create a GameObject with the given <typeparamref name="T"/> component.
		/// The <see cref="ILightComponent{T}.OnInstantiated"/> function will be called.
		/// The light component must have been registered using the <see cref="AddLightComponent{T,TModel}"/> method.
		/// </summary>
		/// <param name="provider">A <see cref="IDependencyProvider"/> that has the <typeparamref name="T"/> registered. </param>
		/// <param name="container">The parent <see cref="Transform"/> where the GameObject will be spawned</param>
		/// <param name="model">The <typeparamref name="TModel"/> data that will be passed to the new <typeparamref name="T"/> instance</param>
		/// <typeparam name="T">A type of <see cref="ILightComponent"/></typeparam>
		/// <typeparam name="TModel">arbitrary data that will be given to the new <see cref="ILightComponent"/></typeparam>
		/// <returns>A <see cref="Promise"/> that completes with when the new <see cref="ILightComponent.OnInstantiated"/> method completes.</returns>
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
		
		/// <inheritdoc cref="Instantiate{T,TModel}"/>
		public static Promise<T> Instantiate<T>(
			this IDependencyProvider provider,
			Transform container)
			where T : MonoBehaviour, ILightComponent
		{
			var resolver = provider.GetService<LightBeamViewResolver<T>>();
			var instance = resolver(container);
			return instance;
		}


		/// <summary>
		/// Register a <see cref="ILightComponent"/> UI component for the <see cref="LightBeam"/>.
		/// After registering the component, it can be instantiated by using the <see cref="LightBeam.Instantiate{T}"/> method.
		/// </summary>
		/// <param name="builder">The <see cref="IDependencyBuilder"/> for the <see cref="LightBeam"/></param>
		/// <param name="template">An asset reference to a GameObject prefab that will be instantiated when the <see cref="LightBeam.Instantiate{T}"/> method is used.</param>
		/// <typeparam name="T">The type of the <see cref="ILightComponent"/></typeparam>
		/// <typeparam name="TModel">The data type for the <see cref="ILightComponent"/></typeparam>
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
		
		/// <inheritdoc cref="AddLightComponent{T,TModel}"/>
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
		
		
		private static Promise<object> Instantiate(IDependencyProvider provider,
		                                           Type componentType,
		                                           Transform container)
		{
			var resolver = provider.GetService<CurriedLightBeamViewResolver>();
			var instance = resolver(container, componentType, null);
			return instance;
		}

		private static Promise<object> Instantiate(IDependencyProvider provider,
		                                           Type componentType,
		                                           Transform container,
		                                           object model)
		{
			var resolver = provider.GetService<CurriedLightBeamViewResolver>();

			var instance = resolver(container, componentType, model);
			return instance;
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
		
		delegate Promise<T> LightBeamViewResolver<T, in TModel>(Transform container, TModel model)
			where T : ILightComponent<TModel>;
	
		delegate Promise<T> LightBeamViewResolver<T>(Transform container)
			where T : ILightComponent;

		delegate Promise<object> CurriedLightBeamViewResolver(Transform container, Type componentType, object model);
	}
}
