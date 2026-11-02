using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Content;
using System;
using static Beamable.Common.Constants.Features.ContentManager;

namespace Beamable.Editor.ContentService
{
	[BeamContextSystem]
	public static class EditorLocalContentRegistration
	{
		[RegisterBeamableDependencies(-999, RegistrationOrigin.RUNTIME)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.RemoveIfExists<ILocalContentSource>();
			builder.AddSingleton<ILocalContentSource>(provider => Create(
				provider.GetService<ContentParameterProvider>(),
				ContentConfiguration.Instance,
				() => BeamEditorContext.Default.CliContentService.manifestIdOverride,
				EditorWorkspace));
		}

		/// <summary>
		/// Creates the local content source for the context being built now. Play Mode resolves the namespace
		/// selected in the Content window (an empty selection is the default namespace), from the realm the
		/// context uses. The editor is consulted only when local content is enabled.
		/// </summary>
		public static ILocalContentSource Create(ContentParameterProvider parameters, ContentConfiguration config,
		                                         Func<string> windowNamespace, Func<Promise<string>> workspace)
		{
			if (!parameters.EnableLocalContentInEditor) return new DisabledLocalContentSource();

			var selected = windowNamespace();
			return new EditorLocalContentSource(
				string.IsNullOrEmpty(selected) ? DEFAULT_MANIFEST_ID : selected,
				Beam.RuntimeConfigProvider.Pid,
				workspace(),
				config.LocalContentManifestDelaySeconds?.HasValue == true ? config.LocalContentManifestDelaySeconds.Value : 0,
				config.LocalContentReferenceDelaySeconds?.HasValue == true ? config.LocalContentReferenceDelaySeconds.Value : 0);
		}

		private static async Promise<string> EditorWorkspace()
		{
			var editor = BeamEditorContext.Default;
			// Runtime registration can run before the editor has loaded its CLI configuration.
			if (editor.BeamCli.latestConfig == null) await editor.OnReady;
			return editor.BeamCli.latestConfig?.configPath;
		}
	}
}
