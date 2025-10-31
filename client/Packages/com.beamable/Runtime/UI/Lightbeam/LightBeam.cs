using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Beamable.Runtime.LightBeams
{
	/// <summary>
	/// A <see cref="LightBeam"/> is a data structure that contains basic information
	/// for a slim Unity UI.
	/// To create a <see cref="LightBeam"/>, use the <see cref="LightBeamDependencyExtensions.CreateLightBeam"/> function.
	/// </summary>
	public class LightBeam
	{
		/// <summary>
		/// The <see cref="BeamContext"/> used to populate the data for the given <see cref="LightBeam"/> UI.
		/// </summary>
		public BeamContext BeamContext { get; set; }

		/// <summary>
		/// A child scope from the <see cref="BeamContext"/>'s dependency scope.
		/// This scope contains all the registered <see cref="ILightComponent"/> services.
		/// </summary>
		public IDependencyProviderScope Scope { get; set; }

		/// <summary>
		/// The transform that is used as the top level of the UI.
		/// GameObjects will be created and removed from this transform as
		/// the UI changes pages. 
		/// </summary>
		public RectTransform Root { get; set; }

		/// <summary>
		/// When page changes happen, the <see cref="CanvasGroup"/> will be faded in and out.
		/// </summary>
		public CanvasGroup LoadingBlocker { get; set; }


		/// <inheritdoc cref="LightBeamDependencyExtensions.Instantiate{T, TModel}"/>
		public Promise<T> Instantiate<T, TModel>(Transform container,
													   TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.Instantiate<T, TModel>(container, model);

		/// <inheritdoc cref="LightBeamDependencyExtensions.Instantiate{T}"/>
		public Promise<T> Instantiate<T>(Transform container)
			where T : MonoBehaviour, ILightComponent =>
			Scope.Instantiate<T>(container);

		public Promise<T> GotoPage<T, TModel>(TModel model)
			where T : MonoBehaviour, ILightComponent<TModel> =>
			Scope.GotoPage<T, TModel>(model);

		public Promise<T> GotoPage<T>() where T : MonoBehaviour, ILightComponent
			=> Scope.GotoPage<T>();

		public void OpenPortalRealm(string relativePath,
		                       Dictionary<string, string> queryArgs = null,
		                       bool includeAuthIfAvailable = true)
		{
			OpenPortalBase($"/{BeamContext.Cid}/games/{BeamContext.Pid}/realms/{BeamContext.Pid}{(relativePath.StartsWith("/") ? relativePath : $"/{relativePath}")}", queryArgs, includeAuthIfAvailable);
		}
		public void OpenPortalBase(string relativePath, Dictionary<string, string> queryArgs=null, bool includeAuthIfAvailable = true)
		{
			OpenPortalBase(BeamContext, relativePath, queryArgs, includeAuthIfAvailable);
		}

		public static void OpenPortalRealm(BeamContext ctx,
		                                   string relativePath,
		                                   Dictionary<string, string> queryArgs = null,
		                                   bool includeAuthIfAvailable = true)
		{
			OpenPortalBase(ctx, $"/{ctx.Cid}/games/{ctx.Pid}/realms/{ctx.Pid}{(relativePath.StartsWith("/") ? relativePath : $"/{relativePath}")}", queryArgs, includeAuthIfAvailable);
		}

		public static void OpenPortalBase(BeamContext ctx, string relativePath, Dictionary<string, string> queryArgs=null, bool includeAuthIfAvailable = true)
		{
			var config = ctx.ServiceProvider.GetService<IDefaultRuntimeConfigProvider>();
			var escaper = ctx.ServiceProvider.GetService<IUrlEscaper>();
			var builder = new QueryBuilder(escaper);

			if (queryArgs != null)
			{
				foreach (var kvp in queryArgs)
				{
					builder.Add(kvp.Key, kvp.Value);
				}
			}
			if (includeAuthIfAvailable)
			{
				if (ctx.ServiceProvider.CanBuildService<IBeamDeveloperAuthProvider>())
				{
					var provider = ctx.ServiceProvider.GetService<IBeamDeveloperAuthProvider>();
					if (!string.IsNullOrEmpty(provider.RefreshToken))
					{
						builder.Add("refresh_token", provider.RefreshToken);
					}
				}
			}
			if (!relativePath.StartsWith("/"))
			{
				relativePath = "/" + relativePath;
			}
			
			var url = config.PortalUrl + relativePath + builder;
			Application.OpenURL(url);
		}
		
	}

	public interface ILightRoot
	{

	}

	/// <summary>
	/// A component that can be registered in a <see cref="LightBeam"/> UI.
	/// Critically, when the <see cref="LightBeam.Instantiate{T}"/> creates a
	/// GameObject with this component, the <see cref="OnInstantiated"/> method is
	/// executed.
	/// </summary>
	public interface ILightComponent : ILightRoot
	{
		/// <summary>
		/// Initializes the instance.
		/// </summary>
		/// <param name="beam">
		/// The <see cref="LightBeam"/> that created the instance. 
		/// </param>
		/// <returns>
		/// A <see cref="Promise"/> that should complete when the component is ready to
		/// be shown.
		/// </returns>
		Promise OnInstantiated(LightBeam beam);
	}

	/// <inheritdoc cref="ILightComponent"/>
	/// <typeparam name="T">The type of the data model that is required to create the instance</typeparam>
	public interface ILightComponent<in T> : ILightRoot
	{
		/// <inheritdoc cref="ILightComponent.OnInstantiated"/>
		/// <param name="model">Data that should be used to configure the instance</param>
		Promise OnInstantiated(LightBeam beam, T model);
	}
}

