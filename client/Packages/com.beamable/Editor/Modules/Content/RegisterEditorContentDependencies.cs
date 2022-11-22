using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Content
{
	[BeamContextSystem()]
	public class RegisterEditorContentDependencies
	{
		[RegisterBeamableDependencies(Constants.SYSTEM_DEPENDENCY_ORDER + 1)]
		public static void Register(IDependencyBuilder builder)
		{
			var tempScope = builder.Build();
			var config = tempScope.GetService<ContentParameterProvider>();

			if (config.EnableLocalContentInEditor)
			{
				builder.AddSingleton<IContentIO, ContentIO>();
				builder.ReplaceSingleton<IManifestResolver, LocalManifestResolver>();
				builder.ReplaceSingleton<IContentCacheFactory, LocalContentCacheFactory>();
				builder.AddSingleton<DefaultContentCacheFactory>();

			}
		}
	}

	public class LocalContentCacheFactory : IContentCacheFactory
	{
		private readonly CoroutineService _coroutineService;
		private readonly ContentConfiguration _config;
		private readonly IContentCacheFactory _defaultFactory;
		public LocalContentCacheFactory(IDependencyProvider provider, CoroutineService coroutineService, ContentConfiguration config)
		{
			_coroutineService = coroutineService;
			_config = config;
			_defaultFactory = provider.GetService<DefaultContentCacheFactory>();
		}
		
		public ContentCache CreateCache(ContentService service, string manifestId, Type contentType)
		{
			if (!string.Equals(manifestId, _config.EditorManifestID))
			{
				return _defaultFactory.CreateCache(service, manifestId, contentType);
			}
			return new LocalContentCache(contentType, _coroutineService, _config);
		}
	}
	
	public class LocalManifestResolver : IManifestResolver
	{
		private readonly IContentIO _contentIO;
		private readonly ContentConfiguration _config;
		private readonly CoroutineService _coroutineService;
		private LocalContentManifest _localManifest;
		private readonly IManifestResolver _defaultResolver;

		public LocalManifestResolver(IContentIO contentIO, ContentConfiguration config, CoroutineService coroutineService)
		{
			_contentIO = contentIO;
			_config = config;
			_coroutineService = coroutineService;
			_localManifest = _contentIO.BuildLocalManifest();
			_defaultResolver = new DefaultManifestResolver();
		}

		public Promise<ClientManifest> ResolveManifest(IBeamableRequester requester, string url, ManifestSubscription subscription)
		{
			if (!string.Equals(subscription.ManifestID, _config.EditorManifestID))
			{
				// we don't have access to this manifest locally, so we'll need to go get it from the remote :( 
				return _defaultResolver.ResolveManifest(requester, url, subscription);
			}
			
			var manifest = new ClientManifest
			{
				entries = _localManifest.Content
				                        .Select(kvp => new ClientContentInfo
				                        {
					                        contentId = kvp.Value.Id,
					                        manifestID = subscription.ManifestID,
					                        tags = kvp.Value.Tags,
					                        uri = kvp.Value.AssetPath,
					                        type = kvp.Value.TypeName,
					                        version = kvp.Value.Version,
					                        visibility = ContentVisibility.Public
				                        })
				                        .ToList()
			};

			var delayPromise = new Promise();
			// simulate some delay... 
			IEnumerator Delay()
			{
				var delay = _config.LocalContentManifestDelaySeconds.GetOrElse(.2f);
				yield return new WaitForSeconds(delay);
				delayPromise.CompleteSuccess();
			}
			_coroutineService.StartNew("local-content-delay", Delay());
			return delayPromise.Map(_ => manifest);
		}
	}
	
}
